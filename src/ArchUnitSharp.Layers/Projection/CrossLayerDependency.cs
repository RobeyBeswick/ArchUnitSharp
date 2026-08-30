namespace ArchUnitSharp.Layers.Projection;

/// <summary>
/// One cross-layer dependency a <see cref="LayersProjection"/> finds: a dependency edge whose
/// importing file and imported file belong to two different layers. The raw dependency is carried as
/// the two concrete file identifiers so a violation can name them; the two layer names are the
/// dependency the layers module reasons about.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="SourceLayer"/> and <see cref="TargetLayer"/> are the layer names of the importing and
/// imported file. They are always different — an intra-layer dependency is not a cross-layer
/// dependency — and neither end is an undeclared file, because an edge whose endpoint belongs to no
/// layer is dropped by the projection. <see cref="Source"/> and <see cref="Target"/> are the
/// project-relative identifiers of the two files.
/// </para>
/// <para>
/// This type is immutable and value-semantic: two dependencies with the same four values are equal.
/// </para>
/// </remarks>
internal sealed record CrossLayerDependency
{
    private readonly string _sourceLayer;
    private readonly string _targetLayer;
    private readonly string _source;
    private readonly string _target;

    /// <summary>
    /// The layer of the importing file.
    /// </summary>
    internal string SourceLayer
    {
        get => _sourceLayer;
        init => _sourceLayer = Require(value, nameof(SourceLayer));
    }

    /// <summary>
    /// The layer of the imported file.
    /// </summary>
    internal string TargetLayer
    {
        get => _targetLayer;
        init => _targetLayer = Require(value, nameof(TargetLayer));
    }

    /// <summary>
    /// The importing file's project-relative identifier.
    /// </summary>
    internal string Source
    {
        get => _source;
        init => _source = Require(value, nameof(Source));
    }

    /// <summary>
    /// The imported file's project-relative identifier.
    /// </summary>
    internal string Target
    {
        get => _target;
        init => _target = Require(value, nameof(Target));
    }

    /// <summary>
    /// Creates a cross-layer dependency.
    /// </summary>
    /// <param name="sourceLayer">The layer of the importing file. Must not be <see langword="null"/> or empty.</param>
    /// <param name="targetLayer">The layer of the imported file. Must not be <see langword="null"/> or empty.</param>
    /// <param name="source">The importing file's identifier. Must not be <see langword="null"/> or empty.</param>
    /// <param name="target">The imported file's identifier. Must not be <see langword="null"/> or empty.</param>
    /// <exception cref="ArgumentNullException"><paramref name="sourceLayer"/>, <paramref name="targetLayer"/>, <paramref name="source"/> or <paramref name="target"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="sourceLayer"/>, <paramref name="targetLayer"/>, <paramref name="source"/> or <paramref name="target"/> is empty.</exception>
    internal CrossLayerDependency(string sourceLayer, string targetLayer, string source, string target)
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
