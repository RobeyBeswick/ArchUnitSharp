namespace ArchUnitSharp.Graph.Projection;

using ArchUnitSharp.Projection;

/// <summary>
/// One immutable capture of a project's dependency graph in the shape every graph report renders
/// from: the <see cref="ProjectedNode"/>s (one per file of the project) and the
/// <see cref="ProjectedEdge"/>s (one per dependency between distinct files, external dependencies
/// included). A report captures the snapshot once and hands the same instance to all six output
/// formats, which is why the six render from the same view of the graph.
/// </summary>
/// <remarks>
/// <para>
/// The nodes are the files of the graph — every file carries a self-edge, which node projection turns
/// into a node — and the edges are the projected dependencies between distinct files, self-edges
/// filtered out. An external edge keeps its target as the written module name and its
/// <see cref="ProjectedEdge.External"/> flag, so a renderer can tell a file from an external module
/// and a dependency that leaves the project from one that stays inside it.
/// </para>
/// <para>
/// The node and edge lists are copied on construction and copied again on every read, so a caller can
/// never corrupt the snapshot through a reference it obtained from this type. This type is immutable
/// and safe for concurrent use: once constructed, it never changes.
/// </para>
/// </remarks>
internal sealed class GraphSnapshot
{
    private readonly ProjectedNode[] _nodes;
    private readonly ProjectedEdge[] _edges;

    /// <summary>
    /// Creates a snapshot from the given projected nodes and edges. The supplied sequences are
    /// copied; later mutation of them does not affect this snapshot.
    /// </summary>
    /// <param name="nodes">The nodes of the graph's projected view. Must not be <see langword="null"/>.</param>
    /// <param name="edges">The edges of the graph's projected view. Must not be <see langword="null"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="nodes"/> or <paramref name="edges"/> is <see langword="null"/>.</exception>
    public GraphSnapshot(IEnumerable<ProjectedNode> nodes, IEnumerable<ProjectedEdge> edges)
    {
        ArgumentNullException.ThrowIfNull(nodes);
        ArgumentNullException.ThrowIfNull(edges);
        _nodes = nodes.ToArray();
        _edges = edges.ToArray();
    }

    /// <summary>
    /// The graph's projected nodes, one per file, sorted by label. Each access returns a fresh copy,
    /// so the returned list is always safe to hold or mutate.
    /// </summary>
    public IReadOnlyList<ProjectedNode> Nodes => _nodes.ToArray();

    /// <summary>
    /// The graph's projected edges, one per dependency between distinct files, sorted by source then
    /// target. Each access returns a fresh copy, so the returned list is always safe to hold or
    /// mutate.
    /// </summary>
    public IReadOnlyList<ProjectedEdge> Edges => _edges.ToArray();
}
