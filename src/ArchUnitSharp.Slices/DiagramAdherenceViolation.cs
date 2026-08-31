namespace ArchUnitSharp.Slices;

using ArchUnitSharp.Common.Extraction;

/// <summary>
/// A violation produced by the positive mood of the slices <c>adhere to diagram</c> predicate —
/// <c>should adhere to diagram</c>: a dependency the actual graph carries that the diagram does not
/// allow. Carries the two slice labels that form the dependency, and nothing else.
/// </summary>
/// <remarks>
/// <para>
/// The predicate reports one of these per disallowed dependency between two slices, with
/// <see cref="SourceSlice"/> the slice that depends and <see cref="TargetSlice"/> the slice or external
/// module it depends on — the pair the diagram has no arrow for. Dependencies between the same two
/// slices collapse into one violation, because the rule is about the slice-level architecture the
/// diagram describes. It carries <see cref="ViolationKind.Rule"/>, the same kind the slices module's
/// other predicate violations carry.
/// </para>
/// <para>
/// This type is immutable and value-semantic; two violations with the same two slices are equal.
/// </para>
/// </remarks>
public sealed record DiagramAdherenceViolation : Violation
{
    private readonly string _sourceSlice;
    private readonly string _targetSlice;

    /// <summary>
    /// The slice that depends. Must not be <see langword="null"/> or empty; both the constructor and a
    /// <see langword="with"/> expression route through the same validation, so neither can introduce a
    /// bad value.
    /// </summary>
    public string SourceSlice
    {
        get => _sourceSlice;
        init => _sourceSlice = Require(value, nameof(SourceSlice));
    }

    /// <summary>
    /// The slice or external module it depends on. Must not be <see langword="null"/> or empty; both
    /// the constructor and a <see langword="with"/> expression route through the same validation, so
    /// neither can introduce a bad value.
    /// </summary>
    public string TargetSlice
    {
        get => _targetSlice;
        init => _targetSlice = Require(value, nameof(TargetSlice));
    }

    /// <summary>
    /// Creates a violation for a dependency the diagram does not allow.
    /// </summary>
    /// <param name="sourceSlice">The depending slice; must not be <see langword="null"/> or empty.</param>
    /// <param name="targetSlice">The depended-on slice or external module; must not be <see langword="null"/> or empty.</param>
    /// <exception cref="ArgumentNullException"><paramref name="sourceSlice"/> or <paramref name="targetSlice"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="sourceSlice"/> or <paramref name="targetSlice"/> is empty.</exception>
    public DiagramAdherenceViolation(string sourceSlice, string targetSlice)
        : base(ViolationKind.Rule)
    {
        _sourceSlice = Require(sourceSlice, nameof(SourceSlice));
        _targetSlice = Require(targetSlice, nameof(TargetSlice));
    }

    private static string Require(string value, string parameterName) =>
        value is null
            ? throw new ArgumentNullException(parameterName)
            : value.Length == 0
                ? throw new ArgumentException($"{parameterName} must not be empty.", parameterName)
                : value;
}
