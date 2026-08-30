namespace ArchUnitSharp.Layers;

using ArchUnitSharp.Common.Extraction;

/// <summary>
/// A violation produced by a layers rule predicate — <c>may only depend on layers(...)</c> or
/// <c>may not depend on layers(...)</c>: a cross-layer dependency the rule forbids. Carries the two
/// layers the dependency runs between and the two concrete files that form it, and nothing else.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="SourceLayer"/> is the layer of the importing file and <see cref="TargetLayer"/> the
/// layer of the imported file; <see cref="Source"/> and <see cref="Target"/> are the two files
/// themselves, so a report can point at the concrete dependency behind the layer-to-layer one. The
/// meaning — whether the dependency was blocked outright or fell outside an allowlist — is supplied
/// by the rule that produced it. It carries <see cref="ViolationKind.Rule"/>.
/// </para>
/// <para>
/// This type is immutable and value-semantic; two violations with the same four values are equal.
/// </para>
/// </remarks>
public sealed record LayerViolation : Violation
{
    private readonly string _sourceLayer;
    private readonly string _targetLayer;
    private readonly string _source;
    private readonly string _target;

    /// <summary>
    /// The layer of the importing file. Must not be <see langword="null"/> or empty; both the
    /// constructor and a <see langword="with"/> expression route through the same validation, so
    /// neither can introduce a bad value.
    /// </summary>
    public string SourceLayer
    {
        get => _sourceLayer;
        init => _sourceLayer = Require(value, nameof(SourceLayer));
    }

    /// <summary>
    /// The layer of the imported file. Must not be <see langword="null"/> or empty; both the
    /// constructor and a <see langword="with"/> expression route through the same validation, so
    /// neither can introduce a bad value.
    /// </summary>
    public string TargetLayer
    {
        get => _targetLayer;
        init => _targetLayer = Require(value, nameof(TargetLayer));
    }

    /// <summary>
    /// The importing file. Must not be <see langword="null"/> or empty; both the constructor and a
    /// <see langword="with"/> expression route through the same validation, so neither can introduce a
    /// bad value.
    /// </summary>
    public string Source
    {
        get => _source;
        init => _source = Require(value, nameof(Source));
    }

    /// <summary>
    /// The imported file. Must not be <see langword="null"/> or empty; both the constructor and a
    /// <see langword="with"/> expression route through the same validation, so neither can introduce a
    /// bad value.
    /// </summary>
    public string Target
    {
        get => _target;
        init => _target = Require(value, nameof(Target));
    }

    /// <summary>
    /// Creates a violation for a forbidden cross-layer dependency.
    /// </summary>
    /// <param name="sourceLayer">The layer of the importing file; must not be <see langword="null"/> or empty.</param>
    /// <param name="targetLayer">The layer of the imported file; must not be <see langword="null"/> or empty.</param>
    /// <param name="source">The importing file; must not be <see langword="null"/> or empty.</param>
    /// <param name="target">The imported file; must not be <see langword="null"/> or empty.</param>
    /// <exception cref="ArgumentNullException"><paramref name="sourceLayer"/>, <paramref name="targetLayer"/>, <paramref name="source"/> or <paramref name="target"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="sourceLayer"/>, <paramref name="targetLayer"/>, <paramref name="source"/> or <paramref name="target"/> is empty.</exception>
    public LayerViolation(string sourceLayer, string targetLayer, string source, string target)
        : base(ViolationKind.Rule)
    {
        _sourceLayer = Require(sourceLayer, nameof(sourceLayer));
        _targetLayer = Require(targetLayer, nameof(targetLayer));
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
