namespace ArchUnitSharp.Projection;

using ArchUnitSharp.Common.Extraction;

/// <summary>
/// The shared reshaping layer of the library: the three projections a module applies to the one
/// shared <see cref="Graph"/> to get its own view of it. Each operation takes a <see cref="MapFunction"/>
/// — the single hook for filtering and relabelling — so a module that maps files to layers, slices or
/// any other label uses the same mechanism as a module that keeps the files themselves.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Edges"/> projects the graph to its dependencies, <see cref="ToNodes"/> to its nodes and
/// <see cref="Cycles"/> to its dependency cycles. The <see cref="MapFunction"/> returns
/// <see langword="null"/> to drop an edge and a <see cref="ProjectedEdge"/> to keep it under new
/// labels; the projected edge must carry the raw edge it was given so the projected view can always
/// name the concrete files behind it.
/// </para>
/// <para>
/// Every projection follows the same conventions. Parallel projected edges — distinct raw edges that
/// map to the same relabelled <c>(source, target)</c> — merge into one edge whose import kinds are
/// the union and whose external flag is true only when every merged edge was external, so a label any
/// raw edge reached from inside the project remains a node. Self-edges — a projected edge whose
/// source and target are the same label — are filtered out, so the projected edge set and the cycle
/// set describe dependencies <em>between</em> distinct labels; node projection is the exception, and
/// depends on the raw self-edges every file carries. Output is sorted, so reports are reproducible.
/// </para>
/// <para>
/// This type is stateless and safe for concurrent use. The lists it returns are fresh copies, and the
/// <see cref="ProjectedEdge"/>, <see cref="ProjectedNode"/> and <see cref="ProjectedCycle"/> values in
/// them are immutable.
/// </para>
/// </remarks>
public static class Projection
{
    /// <summary>
    /// Projects the graph to its dependencies: every raw edge is mapped, dropped edges and self-edges
    /// are removed, parallel projected edges are merged and the result is sorted by source then
    /// target.
    /// </summary>
    /// <param name="graph">The shared graph to project. Must not be <see langword="null"/>.</param>
    /// <param name="map">The per-edge relabelling hook. Must not be <see langword="null"/>.</param>
    /// <returns>The projected edges, sorted by source then target.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="graph"/> or <paramref name="map"/> is <see langword="null"/>.</exception>
    public static IReadOnlyList<ProjectedEdge> Edges(Graph graph, MapFunction map)
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(map);

        var merged = new Dictionary<(string Source, string Target), (bool External, ImportKind Kinds, List<Edge> Edges)>();

        foreach (Edge edge in graph.Edges)
        {
            ProjectedEdge? projected = map(edge);
            if (projected is null || projected.Source == projected.Target)
            {
                continue;
            }

            var key = (projected.Source, projected.Target);
            if (merged.TryGetValue(key, out (bool External, ImportKind Kinds, List<Edge> Edges) existing))
            {
                existing.Edges.AddRange(projected.Edges);
                merged[key] = (existing.External && projected.External, existing.Kinds | projected.ImportKinds, existing.Edges);
            }
            else
            {
                merged[key] = (projected.External, projected.ImportKinds, projected.Edges.ToList());
            }
        }

        return merged
            .Select(static pair => new ProjectedEdge(
                pair.Key.Source,
                pair.Key.Target,
                pair.Value.External,
                pair.Value.Kinds,
                pair.Value.Edges.OrderBy(static edge => edge.Source, StringComparer.Ordinal)
                    .ThenBy(static edge => edge.Target, StringComparer.Ordinal)))
            .OrderBy(static edge => edge.Source, StringComparer.Ordinal)
            .ThenBy(static edge => edge.Target, StringComparer.Ordinal)
            .ToArray();
    }

    /// <summary>
    /// Projects the graph to its nodes: every file's raw self-edge is mapped, dropped files are
    /// removed and the files whose mapped self-edge carries the same source label are grouped into one
    /// node that carries all of them. The result is sorted by label.
    /// </summary>
    /// <param name="graph">The shared graph to project. Must not be <see langword="null"/>.</param>
    /// <param name="map">The per-edge relabelling hook. Must not be <see langword="null"/>.</param>
    /// <returns>The projected nodes, sorted by label.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="graph"/> or <paramref name="map"/> is <see langword="null"/>.</exception>
    public static IReadOnlyList<ProjectedNode> ToNodes(Graph graph, MapFunction map)
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(map);

        var grouped = new Dictionary<string, List<Edge>>(StringComparer.Ordinal);

        foreach (Edge edge in graph.Edges)
        {
            if (edge.Source != edge.Target)
            {
                continue;
            }

            ProjectedEdge? projected = map(edge);
            if (projected is null)
            {
                continue;
            }

            if (!grouped.TryGetValue(projected.Source, out List<Edge>? files))
            {
                files = new List<Edge>();
                grouped[projected.Source] = files;
            }

            files.AddRange(projected.Edges);
        }

        return grouped
            .Select(static pair => new ProjectedNode(
                pair.Key,
                pair.Value.OrderBy(static edge => edge.Source, StringComparer.Ordinal)
                    .ThenBy(static edge => edge.Target, StringComparer.Ordinal)))
            .OrderBy(static node => node.Label, StringComparer.Ordinal)
            .ToArray();
    }

    /// <summary>
    /// Projects the graph to its dependency cycles: the projected edges form a graph whose elementary
    /// cycles — each a closed dependency loop in which no label appears twice — are reported in order
    /// as <see cref="ProjectedCycle"/> values whose hops carry the raw edges behind them.
    /// </summary>
    /// <param name="graph">The shared graph to project. Must not be <see langword="null"/>.</param>
    /// <param name="map">The per-edge relabelling hook. Must not be <see langword="null"/>.</param>
    /// <returns>The elementary cycles of the projected graph, sorted by length then by contents.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="graph"/> or <paramref name="map"/> is <see langword="null"/>.</exception>
    public static IReadOnlyList<ProjectedCycle> Cycles(Graph graph, MapFunction map)
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(map);

        IReadOnlyList<ProjectedEdge> edges = Edges(graph, map);
        IReadOnlyList<IReadOnlyList<string>> nodeCycles = Johnson.FindElementaryCycles(edges);

        var byEndpoint = new Dictionary<(string Source, string Target), ProjectedEdge>(edges.Count);
        foreach (ProjectedEdge edge in edges)
        {
            byEndpoint[(edge.Source, edge.Target)] = edge;
        }

        var cycles = new List<ProjectedCycle>(nodeCycles.Count);
        foreach (IReadOnlyList<string> nodes in nodeCycles)
        {
            var hops = new ProjectedEdge[nodes.Count];
            for (int i = 0; i < nodes.Count; i++)
            {
                hops[i] = byEndpoint[(nodes[i], nodes[(i + 1) % nodes.Count])];
            }

            cycles.Add(new ProjectedCycle(hops));
        }

        return cycles;
    }
}
