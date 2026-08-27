namespace ArchUnitSharp.Projection;

using ArchUnitSharp.Common.Extraction;

/// <summary>
/// The ready-made per-edge map functions: <see cref="PerEdge"/>, <see cref="PerInternalEdge"/>,
/// <see cref="PerExternalEdge"/> and <see cref="Identity"/>. A module that keeps the graph's own
/// labels instead of relabelling passes one of these to <see cref="Projection.Edges"/>,
/// <see cref="Projection.ToNodes"/> or <see cref="Projection.Cycles"/>; the four differ only in which
/// edges they drop, never in the labels they choose.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="PerEdge"/> is the dependency view: every edge except self-edges, because a self-edge is
/// a marker that keeps a file visible as a node, not a dependency. <see cref="PerInternalEdge"/>
/// narrows it to the edges whose target lies inside the project, <see cref="PerExternalEdge"/> to the
/// edges whose target lies outside it. Every non-self edge is exactly one of the two, so the internal
/// and external views are disjoint and together they are exactly the per-edge view.
/// <see cref="Identity"/> is the widest: it keeps every edge, self-edges included, and is the map for
/// node projection when the module's nodes are the files themselves.
/// </para>
/// <para>
/// Each member returns the same stateless delegate instance on every call, so the functions are safe
/// for concurrent use and can be cached by a caller.
/// </para>
/// </remarks>
public static class MapFunctions
{
    /// <summary>
    /// Keeps every edge except self-edges, each under its own labels. The map for the dependency
    /// views — edges and cycles — which describe dependencies between distinct files or labels.
    /// </summary>
    public static MapFunction PerEdge { get; } = static edge =>
        edge.Source == edge.Target ? null : Relabel(edge);

    /// <summary>
    /// Keeps every internal edge except self-edges, each under its own labels: an edge whose target
    /// lies inside the project being analysed.
    /// </summary>
    public static MapFunction PerInternalEdge { get; } = static edge =>
        edge.Source == edge.Target || edge.External ? null : Relabel(edge);

    /// <summary>
    /// Keeps every external edge, each under its own labels: an edge whose target lies outside the
    /// project being analysed. External edges are never self-edges, so no self-edge check is needed.
    /// </summary>
    public static MapFunction PerExternalEdge { get; } = static edge =>
        edge.External ? Relabel(edge) : null;

    /// <summary>
    /// Keeps every edge, self-edges included, each under its own labels. The map for node projection
    /// when the module's nodes are the files themselves; node projection depends on the self-edge
    /// every file carries.
    /// </summary>
    public static MapFunction Identity { get; } = static edge =>
        new ProjectedEdge(edge.Source, edge.Target, edge.External, edge.ImportKinds, new[] { edge });

    private static ProjectedEdge Relabel(Edge edge) =>
        new(edge.Source, edge.Target, edge.External, edge.ImportKinds, new[] { edge });
}
