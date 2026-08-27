namespace ArchUnitSharp.Files;

using ArchUnitSharp.Common.Extraction;

/// <summary>
/// A violation produced by a negated depend-on predicate — <c>should not depend on files</c> or
/// <c>should not depend on external modules</c>: a dependency the rule forbids. Carries the dependency
/// as the importing file and the file or external module it imports, and nothing else.
/// </summary>
/// <remarks>
/// <para>
/// The negated depend-on predicates report one of these per offending dependency edge — a selected
/// file that depends on several of the object's files or modules yields several violations — with
/// <see cref="Source"/> the importing file (one of the rule's selected files) and
/// <see cref="Target"/> the imported file or external module (one of the object's files or modules).
/// The meaning is supplied by the rule that produced it. It carries
/// <see cref="ViolationKind.Rule"/>, the same kind the files module's other predicate violations carry.
/// </para>
/// <para>
/// This type is immutable and value-semantic; two violations with the same dependency are equal.
/// </para>
/// </remarks>
public sealed record DependencyViolation : Violation
{
    private readonly string _source;
    private readonly string _target;

    /// <summary>
    /// The file that imports. Must not be <see langword="null"/> or empty; both the constructor and a
    /// <see langword="with"/> expression route through the same validation, so neither can introduce a
    /// bad value.
    /// </summary>
    public string Source
    {
        get => _source;
        init => _source = RequireSource(value);
    }

    /// <summary>
    /// The file or external module that is imported. Must not be <see langword="null"/> or empty; both
    /// the constructor and a <see langword="with"/> expression route through the same validation, so
    /// neither can introduce a bad value.
    /// </summary>
    public string Target
    {
        get => _target;
        init => _target = RequireTarget(value);
    }

    /// <summary>
    /// Creates a violation for a forbidden dependency.
    /// </summary>
    /// <param name="source">The importing file; must not be <see langword="null"/> or empty.</param>
    /// <param name="target">The imported file or external module; must not be <see langword="null"/> or empty.</param>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> or <paramref name="target"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="source"/> or <paramref name="target"/> is empty.</exception>
    public DependencyViolation(string source, string target)
        : base(ViolationKind.Rule)
    {
        _source = RequireSource(source);
        _target = RequireTarget(target);
    }

    private static string RequireSource(string source) =>
        source is null
            ? throw new ArgumentNullException(nameof(Source))
            : source.Length == 0
                ? throw new ArgumentException("Source must not be empty.", nameof(Source))
                : source;

    private static string RequireTarget(string target) =>
        target is null
            ? throw new ArgumentNullException(nameof(Target))
            : target.Length == 0
                ? throw new ArgumentException("Target must not be empty.", nameof(Target))
                : target;
}
