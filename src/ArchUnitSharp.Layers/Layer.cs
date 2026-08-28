namespace ArchUnitSharp.Layers;

using ArchUnitSharp.Common.Extraction;

/// <summary>
/// One declared layer of a named-layer policy: a name bound to the <see cref="Filter"/> that decides
/// which files belong to it. A file belongs to the layer when the filter matches its identifier, the
/// same matching the files module's scope selectors perform.
/// </summary>
/// <remarks>
/// <para>
/// The filter is bound at declaration time — <c>defined by</c> binds a whole-path filter and
/// <c>defined by folder</c> a folder filter — so a layer is a file selection the layers projection can
/// resolve to its files. A layer carries no glob of its own; the compiled <see cref="Pattern"/> lives
/// inside the <see cref="Filter"/>, which is where every glob in the library is compiled.
/// </para>
/// <para>
/// This type is immutable and value-semantic; two layers with the same name and filter are equal.
/// </para>
/// </remarks>
internal sealed record Layer
{
    private readonly string _name;

    /// <summary>
    /// The layer's name, as declared with <c>layer(name)</c> and referenced by <c>where layer(name)</c>
    /// and <c>may (only / not) depend on layers(...)</c>. Must not be <see langword="null"/> or empty;
    /// the constructor validates it.
    /// </summary>
    public string Name
    {
        get => _name;
        init => _name = RequireName(value);
    }

    /// <summary>
    /// The filter that decides which files belong to the layer. Must not be <see langword="null"/>.
    /// </summary>
    public Filter Filter { get; }

    /// <summary>
    /// Creates a layer from a name and its defining filter.
    /// </summary>
    /// <param name="name">The layer's name; must not be <see langword="null"/> or empty.</param>
    /// <param name="filter">The filter that selects the layer's files; must not be <see langword="null"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> or <paramref name="filter"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is empty.</exception>
    public Layer(string name, Filter filter)
    {
        _name = RequireName(name);
        ArgumentNullException.ThrowIfNull(filter);
        Filter = filter;
    }

    private static string RequireName(string name) =>
        name is null
            ? throw new ArgumentNullException(nameof(Name))
            : name.Length == 0
                ? throw new ArgumentException("A layer name must not be empty.", nameof(Name))
                : name;
}
