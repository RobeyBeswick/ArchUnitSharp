namespace ArchUnitSharp.Layers;

using ArchUnitSharp.Common.Extraction;

/// <summary>
/// The second word of a layer declaration: <c>layer(name)</c> is followed by
/// <see cref="DefinedBy"/> or <see cref="DefinedByFolder"/> to say which files belong to the layer.
/// Built from <see cref="Layers.Layer"/>. Completing a declaration returns a new
/// <see cref="Layers"/> with the layer added; this builder holds nothing but the layer's name and the
/// policy it extends.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="DefinedBy"/> binds a whole-path glob — a file belongs to the layer when its full
/// project-relative path matches. <see cref="DefinedByFolder"/> binds a folder glob — a file belongs
/// to the layer when the folder it sits in matches. Both return a new <see cref="Layers"/> and never
/// mutate the policy they were built from, so one builder can be completed several ways without one
/// completion seeing another's. This type is immutable and safe for concurrent use.
/// </para>
/// </remarks>
public sealed class LayerBuilder
{
    private readonly Layers _layers;
    private readonly string _name;

    /// <summary>
    /// Creates the second word of a layer declaration. Callers obtain a <see cref="LayerBuilder"/>
    /// from <see cref="Layers.Layer"/> rather than constructing one.
    /// </summary>
    /// <param name="layers">The policy the declaration extends.</param>
    /// <param name="name">The layer's name.</param>
    internal LayerBuilder(Layers layers, string name)
    {
        _layers = layers;
        _name = LayerDeclaration.RequireName(name);
    }

    /// <summary>
    /// <c>defined by</c>: the files whose whole path matches <paramref name="glob"/> belong to this
    /// layer. Returns a new <see cref="Layers"/> with the layer declared; this builder is unchanged.
    /// </summary>
    /// <param name="glob">The glob to match each file's whole path against. Must not be <see langword="null"/> or empty.</param>
    /// <returns>A new policy with the layer declared by path.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="glob"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="glob"/> is empty.</exception>
    public Layers DefinedBy(string glob) =>
        Add(new Filter(new Pattern(glob), MatchTarget.Path));

    /// <summary>
    /// <c>defined by folder</c>: the files whose folder matches <paramref name="glob"/> belong to this
    /// layer. Returns a new <see cref="Layers"/> with the layer declared; this builder is unchanged.
    /// </summary>
    /// <param name="glob">The glob to match each file's folder against. Must not be <see langword="null"/> or empty.</param>
    /// <returns>A new policy with the layer declared by folder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="glob"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="glob"/> is empty.</exception>
    public Layers DefinedByFolder(string glob) =>
        Add(new Filter(new Pattern(glob), MatchTarget.PathWithoutFilename));

    private Layers Add(Filter filter) =>
        _layers.AddDeclaration(new LayerDeclaration(_name, filter));
}
