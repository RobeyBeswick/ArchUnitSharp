namespace ArchUnitSharp.Layers;

/// <summary>
/// One dependency a layers rule considers: an edge from a file of the subject layer to a file that
/// belongs to at least one <em>other</em> declared layer. Internal: the layers projection produces
/// these and the shared assertion turns each into a violation when a constraint forbids it.
/// </summary>
/// <remarks>
/// <para>
/// A self-edge, an external edge, an edge from a file outside the subject layer and an edge to a file
/// in no declared layer are all filtered out before this type is produced, as is an intra-layer
/// target — <see cref="TargetLayers"/> never contains the subject layer's name. The target layers are
/// the declared layers that contain the target, in declaration order.
/// </para>
/// <para>
/// This type is immutable and safe for concurrent use.
/// </para>
/// </remarks>
internal sealed record CrossLayerDependency
{
    /// <summary>
    /// The file of the subject layer that imports.
    /// </summary>
    public string Source { get; }

    /// <summary>
    /// The imported file, a member of at least one other declared layer.
    /// </summary>
    public string Target { get; }

    /// <summary>
    /// The declared layers that contain <see cref="Target"/>, excluding the subject layer, in
    /// declaration order.
    /// </summary>
    public IReadOnlyList<string> TargetLayers { get; }

    /// <summary>
    /// Creates a cross-layer dependency.
    /// </summary>
    /// <param name="source">The importing file of the subject layer.</param>
    /// <param name="target">The imported file.</param>
    /// <param name="targetLayers">The declared layers, other than the subject, that contain the target.</param>
    /// <exception cref="ArgumentNullException"><paramref name="source"/>, <paramref name="target"/> or <paramref name="targetLayers"/> is <see langword="null"/>.</exception>
    public CrossLayerDependency(string source, string target, IReadOnlyList<string> targetLayers)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(targetLayers);
        Source = source;
        Target = target;
        TargetLayers = targetLayers.ToArray();
    }
}
