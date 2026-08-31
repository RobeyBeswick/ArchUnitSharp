namespace ArchUnitSharp.Slices;

using ArchUnitSharp.Common.Extraction;

/// <summary>
/// A violation produced by the negated mood of the slices <c>contain dependency(from, to)</c>
/// predicate — <c>should not contain dependency</c>: a dependency the rule forbids, together with the
/// slice that contains it. Carries the slice's name and the two concrete files that form the
/// dependency, and nothing else.
/// </summary>
/// <remarks>
/// <para>
/// The negated predicate reports one of these per offending dependency edge — a slice that contains
/// several forbidden dependencies yields several violations — with <see cref="Slice"/> the name of the
/// slice the importing file belongs to, <see cref="Source"/> the importing file (one of the rule's
/// <c>from</c> files) and <see cref="Target"/> the imported file (one of the rule's <c>to</c> files).
/// It carries <see cref="ViolationKind.Rule"/>, the same kind the slices module's other predicate
/// violation carries.
/// </para>
/// <para>
/// This type is immutable and value-semantic; two violations with the same three values are equal.
/// </para>
/// </remarks>
public sealed record ForbiddenDependencyViolation : Violation
{
    private readonly string _slice;
    private readonly string _source;
    private readonly string _target;

    /// <summary>
    /// The slice that contains the forbidden dependency. Must not be <see langword="null"/> or empty;
    /// both the constructor and a <see langword="with"/> expression route through the same validation,
    /// so neither can introduce a bad value.
    /// </summary>
    public string Slice
    {
        get => _slice;
        init => _slice = Require(value, nameof(Slice));
    }

    /// <summary>
    /// The file that imports. Must not be <see langword="null"/> or empty; both the constructor and a
    /// <see langword="with"/> expression route through the same validation, so neither can introduce a
    /// bad value.
    /// </summary>
    public string Source
    {
        get => _source;
        init => _source = Require(value, nameof(Source));
    }

    /// <summary>
    /// The file that is imported. Must not be <see langword="null"/> or empty; both the constructor
    /// and a <see langword="with"/> expression route through the same validation, so neither can
    /// introduce a bad value.
    /// </summary>
    public string Target
    {
        get => _target;
        init => _target = Require(value, nameof(Target));
    }

    /// <summary>
    /// Creates a violation for a forbidden dependency inside a slice.
    /// </summary>
    /// <param name="slice">The slice that contains the dependency; must not be <see langword="null"/> or empty.</param>
    /// <param name="source">The importing file; must not be <see langword="null"/> or empty.</param>
    /// <param name="target">The imported file; must not be <see langword="null"/> or empty.</param>
    /// <exception cref="ArgumentNullException"><paramref name="slice"/>, <paramref name="source"/> or <paramref name="target"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="slice"/>, <paramref name="source"/> or <paramref name="target"/> is empty.</exception>
    public ForbiddenDependencyViolation(string slice, string source, string target)
        : base(ViolationKind.Rule)
    {
        _slice = Require(slice, nameof(slice));
        _source = Require(source, nameof(source));
        _target = Require(target, nameof(target));
    }

    private static string Require(string value, string parameterName) =>
        value is null
            ? throw new ArgumentNullException(parameterName)
            : value.Length == 0
                ? throw new ArgumentException($"{parameterName} must not be empty.", parameterName)
                : value;
}
