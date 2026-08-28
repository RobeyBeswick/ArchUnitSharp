namespace ArchUnitSharp.Layers;

using ArchUnitSharp.Common.Extraction;

/// <summary>
/// The SCOPE step of a layer rule chain begun by <see cref="Layers.Layer"/>: the declaration of one
/// named layer, completed by <c>defined by</c> or <c>defined by folder</c>.
/// </summary>
/// <remarks>
/// <para>
/// <c>layer('Model') defined by 'src/Models/**'</c> declares the layer <c>Model</c> over the files
/// whose whole path matches the glob; <c>layer('Model') defined by folder 'src/Models'</c> declares it
/// over the files whose folder matches. Each completion returns the <see cref="Layers"/> policy with
/// the layer added, so further declarations and subject selections can be chained.
/// </para>
/// <para>
/// This type is immutable and safe for concurrent use. Completing a declaration never mutates the
/// policy it was built from.
/// </para>
/// </remarks>
public sealed class LayerDefinition
{
    private readonly Layers _builder;
    private readonly string _name;

    /// <summary>
    /// Creates the definition of the layer <paramref name="name"/> on <paramref name="builder"/>.
    /// Callers obtain a <see cref="LayerDefinition"/> from <see cref="Layers.Layer"/> rather than
    /// constructing one.
    /// </summary>
    /// <param name="builder">The policy the completed layer is added to.</param>
    /// <param name="name">The layer's declared name.</param>
    internal LayerDefinition(Layers builder, string name)
    {
        _builder = builder;
        _name = name;
    }

    /// <summary>
    /// <c>defined by</c>: the layer contains the files whose whole path matches <paramref name="glob"/>.
    /// Returns the policy with the layer declared; the policy this definition was built from is
    /// unchanged.
    /// </summary>
    /// <param name="glob">The glob to match each file's whole path against. Must not be <see langword="null"/> or empty.</param>
    /// <returns>The policy with the layer declared.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="glob"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="glob"/> is empty.</exception>
    /// <exception cref="UserError">The layer's name is already declared on the policy.</exception>
    public Layers DefinedBy(string glob) =>
        _builder.Add(new Layer(_name, MatcherFactory.Path(glob)));

    /// <summary>
    /// <c>defined by folder</c>: the layer contains the files whose folder matches
    /// <paramref name="glob"/>. The folder is a file's identifier with its name removed, so a file
    /// identified by <c>src/Models/Car.cs</c> sits in the folder <c>src/Models</c>. Returns the policy
    /// with the layer declared; the policy this definition was built from is unchanged.
    /// </summary>
    /// <param name="glob">The glob to match each file's folder against. Must not be <see langword="null"/> or empty.</param>
    /// <returns>The policy with the layer declared.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="glob"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="glob"/> is empty.</exception>
    /// <exception cref="UserError">The layer's name is already declared on the policy.</exception>
    public Layers DefinedByFolder(string glob) =>
        _builder.Add(new Layer(_name, MatcherFactory.Folder(glob)));
}
