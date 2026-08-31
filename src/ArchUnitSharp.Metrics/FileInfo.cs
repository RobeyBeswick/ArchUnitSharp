namespace ArchUnitSharp.Metrics;

/// <summary>
/// Static information about one source file: its path and the count facts extracted from its text —
/// lines of code, statements, imports, classes, interfaces and types — plus the <see cref="ClassInfo"/>
/// of every class it declares. The file-level count metrics measure these; the class-level metrics
/// measure the individual <see cref="ClassInfo"/> values, and the <c>for classes matching</c>
/// selector narrows a rule's file subjects to the files that declare at least one matching class.
/// The distance metrics' abstractness reads <see cref="TypeCount"/> and <see cref="AbstractTypeCount"/>,
/// and their normalised distance reads <see cref="LinesOfCode"/>.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="ClassCount"/> is the number of classes the file declares and is exactly the number of
/// entries in <see cref="ClassInfos"/>: the extraction produces both from the same walk, so they
/// cannot disagree. A record, struct, interface or enum declaration is not a class and contributes to
/// neither count.
/// </para>
/// <para>
/// <see cref="TypeCount"/> is the number of types the file declares — a <c>class</c> or
/// <c>interface</c> declaration each counts one, so it is exactly <see cref="ClassCount"/> plus
/// <see cref="InterfaceCount"/> — and <see cref="AbstractTypeCount"/> is the subset that are abstract:
/// an <c>interface</c> is abstract by definition, and so is a <c>class</c> declared with the
/// <c>abstract</c> modifier. The two are always consistent: <see cref="AbstractTypeCount"/> never
/// exceeds <see cref="TypeCount"/>.
/// </para>
/// <para>
/// This type is immutable and value-semantic; two file infos with the same path and facts are equal.
/// </para>
/// </remarks>
public sealed record FileInfo
{
    private readonly string _path;
    private readonly IReadOnlyList<ClassInfo> _classInfos;

    /// <summary>
    /// The file's graph identifier, project-relative. Must not be <see langword="null"/> or empty;
    /// both the constructor and a <see langword="with"/> expression route through the same validation,
    /// so neither can introduce a bad value.
    /// </summary>
    public string Path
    {
        get => _path;
        init => _path = Require(value, nameof(Path));
    }

    /// <summary>
    /// The number of lines of the file that are not blank or whitespace only.
    /// </summary>
    public int LinesOfCode { get; init; }

    /// <summary>
    /// The number of statements of the file: every statement in its syntax tree that is not itself a
    /// block. An <c>if</c>, a <c>return</c>, a declaration and a local function each count one; the
    /// blocks that group them do not.
    /// </summary>
    public int StatementCount { get; init; }

    /// <summary>
    /// The number of import directives of the file: every <c>using</c> directive in its syntax tree.
    /// </summary>
    public int ImportCount { get; init; }

    /// <summary>
    /// The number of classes the file declares, nested declarations included. Exactly the number of
    /// entries in <see cref="ClassInfos"/>.
    /// </summary>
    public int ClassCount { get; init; }

    /// <summary>
    /// The number of interfaces the file declares.
    /// </summary>
    public int InterfaceCount { get; init; }

    /// <summary>
    /// The number of types the file declares: a <c>class</c> or <c>interface</c> declaration each
    /// counts one, so this is exactly <see cref="ClassCount"/> plus <see cref="InterfaceCount"/>. The
    /// distance metrics' abstractness divides <see cref="AbstractTypeCount"/> by it.
    /// </summary>
    public int TypeCount { get; init; }

    /// <summary>
    /// The number of the file's types that are abstract: its <see cref="InterfaceCount"/> (an
    /// interface is abstract by definition) plus its abstract <c>class</c> declarations. Never
    /// exceeds <see cref="TypeCount"/>.
    /// </summary>
    public int AbstractTypeCount { get; init; }

    /// <summary>
    /// The file's classes, one info per <c>class</c> declaration, sorted by identifier. Each access
    /// returns a fresh copy, so the returned list is always safe to hold or mutate.
    /// </summary>
    public IReadOnlyList<ClassInfo> ClassInfos
    {
        get => _classInfos.ToArray();
        init => _classInfos = Copy(value, nameof(ClassInfos));
    }

    /// <summary>
    /// Creates a file info for a source file.
    /// </summary>
    /// <param name="path">The file's graph identifier; must not be <see langword="null"/> or empty.</param>
    /// <param name="linesOfCode">The file's non-blank line count.</param>
    /// <param name="statementCount">The file's statement count.</param>
    /// <param name="importCount">The file's import directive count.</param>
    /// <param name="classCount">The number of classes the file declares.</param>
    /// <param name="interfaceCount">The number of interfaces the file declares.</param>
    /// <param name="typeCount">The number of types the file declares — <see cref="ClassCount"/> plus <see cref="InterfaceCount"/>.</param>
    /// <param name="abstractTypeCount">The number of the file's types that are abstract; must not exceed <paramref name="typeCount"/>.</param>
    /// <param name="classInfos">The file's classes; must not be <see langword="null"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> or <paramref name="classInfos"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="path"/> is empty, or <paramref name="abstractTypeCount"/> exceeds <paramref name="typeCount"/>.</exception>
    public FileInfo(
        string path,
        int linesOfCode,
        int statementCount,
        int importCount,
        int classCount,
        int interfaceCount,
        int typeCount,
        int abstractTypeCount,
        IReadOnlyList<ClassInfo> classInfos)
    {
        _path = Require(path, nameof(Path));
        LinesOfCode = linesOfCode;
        StatementCount = statementCount;
        ImportCount = importCount;
        ClassCount = classCount;
        InterfaceCount = interfaceCount;
        TypeCount = RequireNonNegative(typeCount, nameof(TypeCount));
        AbstractTypeCount = RequireAbstractTypes(abstractTypeCount, TypeCount);
        _classInfos = Copy(classInfos, nameof(ClassInfos));
    }

    /// <summary>
    /// Two file infos are equal when their paths, counts and classes are equal, compared by value.
    /// </summary>
    /// <param name="other">The other file info.</param>
    /// <returns><see langword="true"/> when all nine are equal.</returns>
    public bool Equals(FileInfo? other) =>
        other is not null
        && string.Equals(Path, other.Path, StringComparison.Ordinal)
        && LinesOfCode == other.LinesOfCode
        && StatementCount == other.StatementCount
        && ImportCount == other.ImportCount
        && ClassCount == other.ClassCount
        && InterfaceCount == other.InterfaceCount
        && TypeCount == other.TypeCount
        && AbstractTypeCount == other.AbstractTypeCount
        && ClassInfos.SequenceEqual(other.ClassInfos);

    /// <summary>
    /// A hash code consistent with <see cref="Equals(FileInfo?)"/>, computed over the path, counts and
    /// every class.
    /// </summary>
    /// <returns>A hash code over the file info's facts.</returns>
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Path);
        hash.Add(LinesOfCode);
        hash.Add(StatementCount);
        hash.Add(ImportCount);
        hash.Add(ClassCount);
        hash.Add(InterfaceCount);
        hash.Add(TypeCount);
        hash.Add(AbstractTypeCount);
        foreach (ClassInfo classInfo in ClassInfos)
        {
            hash.Add(classInfo);
        }

        return hash.ToHashCode();
    }

    private static IReadOnlyList<ClassInfo> Copy(IReadOnlyList<ClassInfo> values, string propertyName)
    {
        ArgumentNullException.ThrowIfNull(values);
        return values.ToArray();
    }

    private static int RequireNonNegative(int value, string propertyName) =>
        value >= 0
            ? value
            : throw new ArgumentException($"{propertyName} must not be negative.", propertyName);

    private static int RequireAbstractTypes(int value, int typeCount) =>
        RequireNonNegative(value, nameof(AbstractTypeCount)) <= typeCount
            ? value
            : throw new ArgumentException(
                $"{nameof(AbstractTypeCount)} must not exceed {nameof(TypeCount)}.",
                nameof(AbstractTypeCount));

    private static string Require(string value, string propertyName) =>
        value is null
            ? throw new ArgumentNullException(propertyName)
            : value.Length == 0
                ? throw new ArgumentException($"{propertyName} must not be empty.", propertyName)
                : value;
}
