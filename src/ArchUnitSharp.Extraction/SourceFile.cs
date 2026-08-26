namespace ArchUnitSharp.Extraction;

/// <summary>
/// A single C# source file found by <see cref="SourceEnumerator"/>: the identifier the graph will
/// know the file by, and the absolute path used to read it.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Identifier"/> is the file's path relative to the project root, with forward-slash
/// separators on every operating system — for example <c>src/Models/Car.cs</c>. This is the
/// normalised, stable identifier that becomes a graph node and that the match targets in the kernel
/// (<c>Filename</c>, <c>Path</c>, <c>Classname</c>) are designed to consume. <see cref="AbsolutePath"/>
/// is the file's absolute path, also forward-slash normalised, which extraction reads to parse the
/// file's imports.
/// </para>
/// <para>
/// This type is immutable and value-semantic; two source files with the same identifier and absolute
/// path are equal.
/// </para>
/// </remarks>
public sealed record SourceFile
{
    private readonly string _identifier;
    private readonly string _absolutePath;

    /// <summary>
    /// The file's identifier: its path relative to the project root, normalised to forward-slash
    /// separators. Must not be <see langword="null"/> or empty; both the constructor and a
    /// <see langword="with"/> expression route through the same validation, so neither can introduce
    /// a bad value.
    /// </summary>
    public string Identifier
    {
        get => _identifier;
        init => _identifier = Require(value, nameof(Identifier));
    }

    /// <summary>
    /// The file's absolute path on disk, normalised to forward-slash separators. Must not be
    /// <see langword="null"/> or empty; both the constructor and a <see langword="with"/> expression
    /// route through the same validation, so neither can introduce a bad value.
    /// </summary>
    public string AbsolutePath
    {
        get => _absolutePath;
        init => _absolutePath = Require(value, nameof(AbsolutePath));
    }

    /// <summary>
    /// Creates a source file.
    /// </summary>
    /// <param name="identifier">The file's project-relative identifier; must not be <see langword="null"/> or empty.</param>
    /// <param name="absolutePath">The file's absolute path; must not be <see langword="null"/> or empty.</param>
    /// <exception cref="ArgumentNullException"><paramref name="identifier"/> or <paramref name="absolutePath"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="identifier"/> or <paramref name="absolutePath"/> is empty.</exception>
    public SourceFile(string identifier, string absolutePath)
    {
        _identifier = Require(identifier, nameof(Identifier));
        _absolutePath = Require(absolutePath, nameof(AbsolutePath));
    }

    private static string Require(string value, string propertyName) =>
        value is null
            ? throw new ArgumentNullException(propertyName)
            : value.Length == 0
                ? throw new ArgumentException($"{propertyName} must not be empty.", propertyName)
                : PathNormaliser.Normalise(value);
}
