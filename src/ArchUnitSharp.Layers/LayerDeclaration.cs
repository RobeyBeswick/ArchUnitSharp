namespace ArchUnitSharp.Layers;

using ArchUnitSharp.Common.Extraction;

/// <summary>
/// The internal data model of one layer definition: a layer name bound to a single file filter. A
/// named layer is the union of every declaration that shares its name, so a layer defined in two
/// places — <c>layer("Models").DefinedByFolder("src/Models")</c> and
/// <c>layer("Models").DefinedByFolder("src/Shared/Models")</c> — contains the files that match either
/// filter.
/// </summary>
/// <remarks>
/// <para>
/// The filter is a <see cref="Filter"/> built from the declaration's glob. <c>defined by folder</c>
/// binds a folder glob (<see cref="MatchTarget.PathWithoutFilename"/>); <c>defined by</c> binds a
/// whole-path glob (<see cref="MatchTarget.Path"/>). A file belongs to the layer when the filter
/// matches its identifier.
/// </para>
/// <para>
/// This type is internal to the layers module — the public surface speaks in globs and layer names,
/// never in <see cref="Filter"/> instances. It is immutable and safe for concurrent use.
/// </para>
/// </remarks>
internal sealed class LayerDeclaration
{
    private readonly string _name;
    private readonly Filter _filter;

    /// <summary>
    /// The layer's name.
    /// </summary>
    internal string Name => _name;

    /// <summary>
    /// The file filter that defines the layer's files.
    /// </summary>
    internal Filter Filter => _filter;

    /// <summary>
    /// Creates a layer declaration.
    /// </summary>
    /// <param name="name">The layer's name. Must not be <see langword="null"/> or empty.</param>
    /// <param name="filter">The file filter defining the layer. Must not be <see langword="null"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> or <paramref name="filter"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is empty.</exception>
    internal LayerDeclaration(string name, Filter filter)
    {
        _name = RequireName(name);
        ArgumentNullException.ThrowIfNull(filter);
        _filter = filter;
    }

    internal static string RequireName(string name) =>
        name is null
            ? throw new ArgumentNullException(nameof(name))
            : name.Length == 0
                ? throw new ArgumentException("Layer name must not be empty.", nameof(name))
                : name;
}
