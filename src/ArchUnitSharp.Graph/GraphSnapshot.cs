namespace ArchUnitSharp.Graph;

/// <summary>
/// The immutable data contract of a graph report: the snapshot every renderer consumes. Building a
/// report is two steps — filter, collapse, aggregate and count into a <see cref="GraphSnapshot"/>,
/// then render it — and every renderer consumes the identical snapshot, so a new format is one
/// function and a new query option benefits every format at once.
/// </summary>
/// <remarks>
/// <para>
/// The snapshot carries the report's <see cref="Title"/>, its <see cref="Nodes"/>, its
/// <see cref="Edges"/> — each an aggregate of the raw dependencies it replaced, carrying the
/// aggregation count, the external flag and the union of import kinds — and the summary counts
/// <see cref="NodeCount"/>, <see cref="EdgeCount"/> and <see cref="FileCount"/>. It is produced by
/// <see cref="GraphReport.Build"/> from a <see cref="GraphReport"/> builder's query options.
/// </para>
/// <para>
/// The node and edge lists are copied on construction and copied again on every read, so a caller can
/// never corrupt the snapshot through a reference it obtained from it. This type is immutable and
/// value-semantic: two snapshots with the same title and equal node and edge lists are equal. It is
/// safe for concurrent use.
/// </para>
/// </remarks>
public sealed record GraphSnapshot
{
    private readonly string _title;
    private readonly SnapshotNode[] _nodes;
    private readonly SnapshotEdge[] _edges;

    /// <summary>
    /// The report's title. May be empty when the query did not set one; must not be
    /// <see langword="null"/>. Both the constructor and a <see langword="with"/> expression route
    /// through the same validation, so neither can introduce a bad value.
    /// </summary>
    public string Title
    {
        get => _title;
        init => _title = RequireTitle(value);
    }

    /// <summary>
    /// The snapshot's nodes, one per distinct label the scope's files and, when external dependencies
    /// are included, external targets collapse to. Each access returns a fresh copy, so the returned
    /// list is always safe to hold or mutate.
    /// </summary>
    public IReadOnlyList<SnapshotNode> Nodes
    {
        get => _nodes.ToArray();
        init => _nodes = RequireNodes(value);
    }

    /// <summary>
    /// The snapshot's edges, one per distinct (source label, target label) pair of the raw
    /// dependencies the query included, aggregated with their counts and import-kind unions. Each
    /// access returns a fresh copy, so the returned list is always safe to hold or mutate.
    /// </summary>
    public IReadOnlyList<SnapshotEdge> Edges
    {
        get => _edges.ToArray();
        init => _edges = RequireEdges(value);
    }

    /// <summary>
    /// The number of nodes in the snapshot.
    /// </summary>
    public int NodeCount => _nodes.Length;

    /// <summary>
    /// The number of edges in the snapshot.
    /// </summary>
    public int EdgeCount => _edges.Length;

    /// <summary>
    /// The number of project files the snapshot's scope covers: the sum of the files of every
    /// non-external node. External module nodes carry no project files, so they contribute nothing.
    /// </summary>
    public int FileCount => _nodes.Sum(static node => node.External ? 0 : node.Files.Count);

    /// <summary>
    /// Creates a snapshot.
    /// </summary>
    /// <param name="title">The report's title; must not be <see langword="null"/>. May be empty.</param>
    /// <param name="nodes">The snapshot's nodes. Must not be <see langword="null"/>.</param>
    /// <param name="edges">The snapshot's edges. Must not be <see langword="null"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="title"/>, <paramref name="nodes"/> or <paramref name="edges"/> is <see langword="null"/>.</exception>
    public GraphSnapshot(string title, IEnumerable<SnapshotNode> nodes, IEnumerable<SnapshotEdge> edges)
    {
        _title = RequireTitle(title);
        _nodes = RequireNodes(nodes);
        _edges = RequireEdges(edges);
    }

    /// <summary>
    /// Two snapshots are equal when their titles and their node and edge lists are equal.
    /// </summary>
    /// <param name="other">The other snapshot.</param>
    /// <returns><see langword="true"/> when the snapshots are equal.</returns>
    public bool Equals(GraphSnapshot? other) =>
        other is not null
        && string.Equals(Title, other.Title, StringComparison.Ordinal)
        && Nodes.SequenceEqual(other.Nodes)
        && Edges.SequenceEqual(other.Edges);

    /// <summary>
    /// A hash code consistent with <see cref="Equals(GraphSnapshot?)"/>.
    /// </summary>
    /// <returns>A hash code over the title and the node and edge lists.</returns>
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Title);
        foreach (SnapshotNode node in Nodes)
        {
            hash.Add(node);
        }

        foreach (SnapshotEdge edge in Edges)
        {
            hash.Add(edge);
        }

        return hash.ToHashCode();
    }

    private static string RequireTitle(string title) =>
        title is null
            ? throw new ArgumentNullException(nameof(Title))
            : title;

    private static SnapshotNode[] RequireNodes(IEnumerable<SnapshotNode> nodes)
    {
        ArgumentNullException.ThrowIfNull(nodes);
        return nodes.ToArray();
    }

    private static SnapshotEdge[] RequireEdges(IEnumerable<SnapshotEdge> edges)
    {
        ArgumentNullException.ThrowIfNull(edges);
        return edges.ToArray();
    }
}
