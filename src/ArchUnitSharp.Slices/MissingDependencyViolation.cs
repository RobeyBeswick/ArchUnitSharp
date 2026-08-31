namespace ArchUnitSharp.Slices;

using ArchUnitSharp.Common.Extraction;

/// <summary>
/// A violation produced by the positive mood of the slices <c>contain dependency(from, to)</c>
/// predicate — <c>should contain dependency</c>: a slice that contains none of the required
/// dependencies. Carries the slice's name and the two patterns the missing dependency would have run
/// between, and nothing else.
/// </summary>
/// <remarks>
/// <para>
/// The positive predicate reports one of these per slice that contains no dependency from a sliced
/// file matching <see cref="From"/> to a file matching <see cref="To"/> (the target may be
/// unsliced), with
/// <see cref="Slice"/> the slice's name and <see cref="From"/> / <see cref="To"/> the two globs as
/// written, so a report can name the dependency the slice lacks. It carries
/// <see cref="ViolationKind.Rule"/>, the same kind the slices module's other predicate violation
/// carries.
/// </para>
/// <para>
/// This type is immutable and value-semantic; two violations with the same three values are equal.
/// </para>
/// </remarks>
public sealed record MissingDependencyViolation : Violation
{
    private readonly string _slice;
    private readonly string _from;
    private readonly string _to;

    /// <summary>
    /// The slice that lacks the required dependency. Must not be <see langword="null"/> or empty; both
    /// the constructor and a <see langword="with"/> expression route through the same validation, so
    /// neither can introduce a bad value.
    /// </summary>
    public string Slice
    {
        get => _slice;
        init => _slice = Require(value, nameof(Slice));
    }

    /// <summary>
    /// The glob the importing file of the missing dependency would have matched, as written. Must not
    /// be <see langword="null"/> or empty; both the constructor and a <see langword="with"/> expression
    /// route through the same validation, so neither can introduce a bad value.
    /// </summary>
    public string From
    {
        get => _from;
        init => _from = Require(value, nameof(From));
    }

    /// <summary>
    /// The glob the imported file of the missing dependency would have matched, as written. Must not
    /// be <see langword="null"/> or empty; both the constructor and a <see langword="with"/> expression
    /// route through the same validation, so neither can introduce a bad value.
    /// </summary>
    public string To
    {
        get => _to;
        init => _to = Require(value, nameof(To));
    }

    /// <summary>
    /// Creates a violation for a slice that lacks a required dependency.
    /// </summary>
    /// <param name="slice">The slice that lacks the dependency; must not be <see langword="null"/> or empty.</param>
    /// <param name="from">The <c>from</c> glob as written; must not be <see langword="null"/> or empty.</param>
    /// <param name="to">The <c>to</c> glob as written; must not be <see langword="null"/> or empty.</param>
    /// <exception cref="ArgumentNullException"><paramref name="slice"/>, <paramref name="from"/> or <paramref name="to"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="slice"/>, <paramref name="from"/> or <paramref name="to"/> is empty.</exception>
    public MissingDependencyViolation(string slice, string from, string to)
        : base(ViolationKind.Rule)
    {
        _slice = Require(slice, nameof(slice));
        _from = Require(from, nameof(from));
        _to = Require(to, nameof(to));
    }

    private static string Require(string value, string parameterName) =>
        value is null
            ? throw new ArgumentNullException(parameterName)
            : value.Length == 0
                ? throw new ArgumentException($"{parameterName} must not be empty.", parameterName)
                : value;
}
