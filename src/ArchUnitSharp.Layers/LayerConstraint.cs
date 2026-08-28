namespace ArchUnitSharp.Layers;

/// <summary>
/// One constraint a <see cref="LayerRule"/> accumulates over its subject layer: an allowlist
/// (<c>may only depend on layers(...)</c>) or a blocklist (<c>may not depend on layers(...)</c>). The
/// rule carries the names of the layers the constraint names; the assertion resolves them to files
/// through the rule's declared layers.
/// </summary>
/// <remarks>
/// <para>
/// An allowlist with no names is the sealed-layer idiom: the subject layer may depend on nothing
/// outside itself (intra-layer dependencies are always allowed). A blocklist with no names forbids
/// nothing and is vacuous.
/// </para>
/// <para>
/// This type is immutable and safe for concurrent use. The layer-name list is copied on construction.
/// </para>
/// </remarks>
internal sealed record LayerConstraint
{
    /// <summary>
    /// The constraint's kind, deciding whether <see cref="LayerNames"/> is an allowlist or a blocklist.
    /// </summary>
    public LayerConstraintKind Kind { get; }

    /// <summary>
    /// The names of the layers the constraint names, in the order they were supplied.
    /// </summary>
    public IReadOnlyList<string> LayerNames { get; }

    /// <summary>
    /// Creates a constraint of the given kind over the given layer names.
    /// </summary>
    /// <param name="kind">The constraint's kind.</param>
    /// <param name="layerNames">The names of the layers the constraint names. Must not be <see langword="null"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="layerNames"/> is <see langword="null"/>.</exception>
    public LayerConstraint(LayerConstraintKind kind, IReadOnlyList<string> layerNames)
    {
        ArgumentNullException.ThrowIfNull(layerNames);
        Kind = kind;
        LayerNames = layerNames.ToArray();
    }
}

/// <summary>
/// The kind of a <see cref="LayerConstraint"/>: an allowlist the subject layer may depend on, or a
/// blocklist the subject layer may not depend on.
/// </summary>
internal enum LayerConstraintKind
{
    /// <summary><c>may only depend on layers(...)</c>: the named layers are the whole permitted target set.</summary>
    AllowOnly,

    /// <summary><c>may not depend on layers(...)</c>: the named layers are forbidden targets.</summary>
    Forbid,
}
