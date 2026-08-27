namespace ArchUnitSharp.Projection;

using ArchUnitSharp.Common.Extraction;

/// <summary>
/// The per-edge relabelling hook of the projection layer: a function from a raw <see cref="Edge"/> of
/// the shared graph to the <see cref="ProjectedEdge"/> that represents it in one module's view of the
/// graph. Returning <see langword="null"/> drops the edge; returning a projected edge keeps it under
/// whatever labels the function chose. Filtering and relabelling are the same hook, which is how every
/// module gets its own view of one shared graph.
/// </summary>
/// <remarks>
/// <para>
/// A module implements one of these and passes it to <see cref="Projection.Edges"/>,
/// <see cref="Projection.ToNodes"/> or <see cref="Projection.Cycles"/>. The function receives an edge
/// and decides: drop it (return <see langword="null"/>, e.g. because neither endpoint belongs to the
/// module's world) or keep it (return a <see cref="ProjectedEdge"/> whose source and target are the
/// module's labels for the edge's endpoints). The projected edge must carry the raw edge it was given
/// in its <see cref="ProjectedEdge.Edges"/> list, so violation messages can point at concrete files.
/// </para>
/// <para>
/// A function that is a pure relabelling of every edge — the identity of the projection layer — maps
/// an edge to itself: <c>edge =&gt; new ProjectedEdge(edge.Source, edge.Target, edge.External,
/// edge.ImportKinds, new[] { edge })</c>.
/// </para>
/// </remarks>
/// <param name="edge">The raw edge of the shared graph to relabel. Never <see langword="null"/>.</param>
/// <returns>
/// The edge's projection, or <see langword="null"/> to drop the edge from the projected view.
/// </returns>
public delegate ProjectedEdge? MapFunction(Edge edge);
