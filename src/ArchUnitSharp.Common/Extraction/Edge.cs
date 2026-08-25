namespace ArchUnitSharp.Common.Extraction;

/// <summary>
/// A directed dependency from <see cref="Source"/> to <see cref="Target"/> within the project being
/// analysed. The atom of the whole library: a <see cref="Graph"/> is a list of edges.
/// </summary>
/// <remarks>
/// <para>
/// Identifiers (<see cref="Source"/> and <see cref="Target"/>) are expected to be normalised before
/// they reach this type: separators normalised, and project-relative or absolute throughout, never
/// mixed. <see cref="Graph"/> makes no attempt to mix them.
/// </para>
/// <para>
/// A self-edge (<see cref="Source"/> equal to <see cref="Target"/>) is how a file with no
/// dependencies still appears as a node. <see cref="External"/> marks an edge whose target lies
/// outside the project.
/// </para>
/// <para>
/// This type is immutable and value-semantic; two edges with the same four values are equal, which
/// is what lets parallel edges be detected and merged.
/// </para>
/// </remarks>
public sealed record Edge
{
    private readonly string _source;
    private readonly string _target;

    /// <summary>
    /// The normalised project-relative or absolute identifier of the file that imports. Must not be
    /// <see langword="null"/> or empty; both the constructor and a <see langword="with"/> expression
    /// route through the same validation, so neither can introduce a bad value.
    /// </summary>
    public string Source
    {
        get => _source;
        init => _source = RequireSource(value);
    }

    /// <summary>
    /// The normalised project-relative or absolute identifier of the file or external target that is
    /// imported. Must not be <see langword="null"/> or empty; both the constructor and a
    /// <see langword="with"/> expression route through the same validation, so neither can introduce
    /// a bad value.
    /// </summary>
    public string Target
    {
        get => _target;
        init => _target = RequireTarget(value);
    }

    /// <summary>
    /// <see langword="true"/> when <see cref="Target"/> lies outside the project being analysed and is
    /// therefore not itself a node in the graph.
    /// </summary>
    public bool External { get; init; }

    /// <summary>
    /// The import kinds carried by this edge. A single import contributes exactly one kind; a merged
    /// edge carries the union of the kinds of the parallel edges it replaced.
    /// </summary>
    public ImportKind ImportKinds { get; init; }

    /// <summary>
    /// Creates an edge.
    /// </summary>
    /// <param name="source">The importing file's identifier; must not be <see langword="null"/> or empty.</param>
    /// <param name="target">The imported file's or external target's identifier; must not be <see langword="null"/> or empty.</param>
    /// <param name="external">
    /// <see langword="true"/> when <paramref name="target"/> lies outside the project being analysed.
    /// </param>
    /// <param name="importKinds">The import kind or union of import kinds carried by the edge.</param>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> or <paramref name="target"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="source"/> or <paramref name="target"/> is empty.</exception>
    public Edge(string source, string target, bool external, ImportKind importKinds)
    {
        _source = RequireSource(source);
        _target = RequireTarget(target);
        External = external;
        ImportKinds = importKinds;
    }

    private static string RequireSource(string source) =>
        source is null
            ? throw new ArgumentNullException(nameof(Source))
            : source.Length == 0
                ? throw new ArgumentException("Source must not be empty.", nameof(Source))
                : source;

    private static string RequireTarget(string target) =>
        target is null
            ? throw new ArgumentNullException(nameof(Target))
            : target.Length == 0
                ? throw new ArgumentException("Target must not be empty.", nameof(Target))
                : target;
}
