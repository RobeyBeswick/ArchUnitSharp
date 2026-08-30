namespace ArchUnitSharp.Layers;

/// <summary>
/// The second word of a layer rule: <c>where layer(name)</c> is followed by
/// <see cref="MayOnlyDependOnLayers"/> or <see cref="MayNotDependOnLayers"/> to assert what the layer
/// may or may not depend on. Built from <see cref="Layers.WhereLayer"/>. Completing a rule returns a
/// new <see cref="Layers"/> with the rule added; this builder holds nothing but the subject layer's
/// name and the policy it extends.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="MayOnlyDependOnLayers"/> is the allowlist mood — the subject may depend only on the
/// given layers, and with no arguments it is sealed, meaning it may depend on no other layer.
/// <see cref="MayNotDependOnLayers"/> is the blocklist mood — the subject may not depend on the given
/// layers. Both return a new <see cref="Layers"/> and never mutate the policy they were built from, so
/// one builder can be completed several ways without one completion seeing another's. This type is
/// immutable and safe for concurrent use.
/// </para>
/// </remarks>
public sealed class LayerRule
{
    private readonly Layers _layers;
    private readonly string _name;

    /// <summary>
    /// Creates the second word of a layer rule. Callers obtain a <see cref="LayerRule"/> from
    /// <see cref="Layers.WhereLayer"/> rather than constructing one.
    /// </summary>
    /// <param name="layers">The policy the rule extends.</param>
    /// <param name="name">The subject layer's name.</param>
    internal LayerRule(Layers layers, string name)
    {
        _layers = layers;
        _name = LayerDeclaration.RequireName(name);
    }

    /// <summary>
    /// <c>may only depend on layers(...)</c>: the subject layer may depend only on the given target
    /// layers — intra-layer dependencies are always allowed — and on no other layer. With no
    /// arguments the layer is sealed: it may depend on no other layer at all. Returns a new
    /// <see cref="Layers"/> with the rule asserted; this builder is unchanged.
    /// </summary>
    /// <param name="layerNames">The target layers the subject may depend on. Must not be <see langword="null"/>.</param>
    /// <returns>A new policy with the allowlist rule asserted.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="layerNames"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="layerNames"/> contains a <see langword="null"/> or empty name.</exception>
    public Layers MayOnlyDependOnLayers(params string[] layerNames) =>
        _layers.AddConstraint(new LayerConstraint(_name, layerNames, negate: false));

    /// <summary>
    /// <c>may not depend on layers(...)</c>: the subject layer may not depend on any of the given
    /// target layers. Returns a new <see cref="Layers"/> with the rule asserted; this builder is
    /// unchanged.
    /// </summary>
    /// <param name="layerNames">The target layers the subject may not depend on. Must not be <see langword="null"/>.</param>
    /// <returns>A new policy with the blocklist rule asserted.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="layerNames"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="layerNames"/> contains a <see langword="null"/> or empty name.</exception>
    public Layers MayNotDependOnLayers(params string[] layerNames) =>
        _layers.AddConstraint(new LayerConstraint(_name, layerNames, negate: true));
}
