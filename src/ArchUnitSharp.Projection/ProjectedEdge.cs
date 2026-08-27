namespace ArchUnitSharp.Projection;

using ArchUnitSharp.Common.Extraction;

/// <summary>
/// An edge of the projected graph: a dependency from <see cref="Source"/> to <see cref="Target"/>
/// between two relabelled nodes, together with the raw <see cref="Edge"/>s of the shared graph it was
/// produced from. The <see cref="MapFunction"/> hook returns one of these (or <see langword="null"/>
/// to drop the edge).
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Source"/> and <see cref="Target"/> are the module's labels for the edge's endpoints —
/// a layer, a slice, a class name — not file identifiers. The underlying raw edges are kept in
/// <see cref="Edges"/> so a violation message can point at the concrete files behind a projected
/// dependency. A projected edge produced from parallel raw edges that the projection merges carries
/// every raw edge of the merged set, so nothing a message might want to name is dropped.
/// </para>
/// <para>
/// <see cref="External"/> is <see langword="true"/> when the projected dependency leaves the project:
/// a projected edge merged from several raw edges is external only when every raw edge was external,
/// so a label that any raw edge reached from inside the project remains a node. <see cref="ImportKinds"/>
/// is the union of the import kinds of the raw edges behind the projected edge.
/// </para>
/// <para>
/// This type is immutable and value-semantic: two projected edges with the same labels, external flag,
/// import kinds and raw edges are equal. The raw-edge list is copied on construction and copied again
/// on every read, so a caller can never corrupt an instance through a reference it obtained from it.
/// </para>
/// </remarks>
public sealed record ProjectedEdge
{
    private readonly string _source;
    private readonly string _target;
    private readonly Edge[] _edges;

    /// <summary>
    /// The module's label for the importing endpoint of the projected edge. Must not be
    /// <see langword="null"/> or empty; both the constructor and a <see langword="with"/> expression
    /// route through the same validation, so neither can introduce a bad value.
    /// </summary>
    public string Source
    {
        get => _source;
        init => _source = RequireSource(value);
    }

    /// <summary>
    /// The module's label for the imported endpoint of the projected edge. Must not be
    /// <see langword="null"/> or empty; both the constructor and a <see langword="with"/> expression
    /// route through the same validation, so neither can introduce a bad value.
    /// </summary>
    public string Target
    {
        get => _target;
        init => _target = RequireTarget(value);
    }

    /// <summary>
    /// <see langword="true"/> when the projected dependency leaves the project being analysed. A
    /// projected edge merged from several raw edges is external only when every raw edge was external.
    /// </summary>
    public bool External { get; init; }

    /// <summary>
    /// The union of the import kinds of the raw edges behind this projected edge. A single raw edge
    /// contributes exactly its own kind.
    /// </summary>
    public ImportKind ImportKinds { get; init; }

    /// <summary>
    /// The raw edges of the shared graph behind this projected edge. Never empty. Each access returns
    /// a fresh copy, so the returned list is always safe to hold or mutate.
    /// </summary>
    public IReadOnlyList<Edge> Edges
    {
        get => _edges.ToArray();
        init => _edges = RequireEdges(value);
    }

    /// <summary>
    /// Creates a projected edge.
    /// </summary>
    /// <param name="source">The module's label for the importing endpoint; must not be <see langword="null"/> or empty.</param>
    /// <param name="target">The module's label for the imported endpoint; must not be <see langword="null"/> or empty.</param>
    /// <param name="external">
    /// <see langword="true"/> when the projected dependency leaves the project being analysed.
    /// </param>
    /// <param name="importKinds">The import kind or union of import kinds behind the projected edge.</param>
    /// <param name="edges">The raw edges behind the projected edge; must not be <see langword="null"/> or empty.</param>
    /// <exception cref="ArgumentNullException"><paramref name="source"/>, <paramref name="target"/> or <paramref name="edges"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="source"/> or <paramref name="target"/> is empty, or <paramref name="edges"/> is empty.</exception>
    public ProjectedEdge(
        string source,
        string target,
        bool external,
        ImportKind importKinds,
        IEnumerable<Edge> edges)
    {
        _source = RequireSource(source);
        _target = RequireTarget(target);
        External = external;
        ImportKinds = importKinds;
        _edges = RequireEdges(edges);
    }

    /// <summary>
    /// Two projected edges are equal when their sources, targets, external flags, import kinds and raw
    /// edge lists are equal.
    /// </summary>
    /// <param name="other">The other projected edge.</param>
    /// <returns><see langword="true"/> when the projected edges are equal.</returns>
    public bool Equals(ProjectedEdge? other) =>
        other is not null
        && string.Equals(Source, other.Source, StringComparison.Ordinal)
        && string.Equals(Target, other.Target, StringComparison.Ordinal)
        && External == other.External
        && ImportKinds == other.ImportKinds
        && Edges.SequenceEqual(other.Edges);

    /// <summary>
    /// A hash code consistent with <see cref="Equals(ProjectedEdge?)"/>.
    /// </summary>
    /// <returns>A hash code over the source, target, external flag, import kinds and raw edges.</returns>
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Source);
        hash.Add(Target);
        hash.Add(External);
        hash.Add(ImportKinds);
        foreach (Edge edge in Edges)
        {
            hash.Add(edge);
        }

        return hash.ToHashCode();
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

    private static Edge[] RequireEdges(IEnumerable<Edge> edges)
    {
        ArgumentNullException.ThrowIfNull(edges);
        Edge[] copy = edges.ToArray();
        if (copy.Length == 0)
        {
            throw new ArgumentException("A projected edge must carry at least one raw edge.", nameof(Edges));
        }

        return copy;
    }
}
