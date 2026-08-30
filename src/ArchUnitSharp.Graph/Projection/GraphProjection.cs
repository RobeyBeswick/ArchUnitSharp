namespace ArchUnitSharp.Graph.Projection;

using ArchUnitSharp.Common.Extraction;

/// <summary>
/// The graph module's pure projection logic: capturing the one <see cref="GraphSnapshot"/> every
/// output format renders from. The snapshot is the graph under its own labels — the files as nodes
/// and the dependencies between distinct files as edges — so the six reports show one consistent view
/// of the project.
/// </summary>
/// <remarks>
/// <para>
/// Node projection keeps the identity map so every file of the graph appears as a node; edge
/// projection keeps the per-edge map so every dependency between distinct files appears as an edge,
/// external dependencies included. Both projections sort their output, so the snapshot and everything
/// rendered from it are stable and reproducible.
/// </para>
/// <para>
/// This type is stateless and safe for concurrent use. The snapshot it returns is immutable.
/// </para>
/// </remarks>
internal static class GraphProjection
{
    /// <summary>
    /// Captures the graph's projected view: its files as nodes and its dependencies between distinct
    /// files as edges, external dependencies included.
    /// </summary>
    /// <param name="graph">The project's dependency graph. Must not be <see langword="null"/>.</param>
    /// <returns>The graph's snapshot.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="graph"/> is <see langword="null"/>.</exception>
    public static GraphSnapshot Snapshot(Graph graph)
    {
        ArgumentNullException.ThrowIfNull(graph);

        return new GraphSnapshot(
            ArchUnitSharp.Projection.Projection.ToNodes(graph, ArchUnitSharp.Projection.MapFunctions.Identity),
            ArchUnitSharp.Projection.Projection.Edges(graph, ArchUnitSharp.Projection.MapFunctions.PerEdge));
    }
}
