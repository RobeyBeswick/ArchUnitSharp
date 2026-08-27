namespace ArchUnitSharp.Files.Projection;

using ArchUnitSharp.Common.Extraction;
using ArchUnitSharp.Projection;

/// <summary>
/// The files module's pure projection logic: which files of a <see cref="Graph"/> a scope's list of
/// <see cref="Filter"/> instances selects, and which cycles the selected files' dependencies form.
/// Filters combine with AND — a file is selected when every filter matches it — and the empty filter
/// list selects every file.
/// </summary>
/// <remarks>
/// <para>
/// The files of a graph are its nodes, which the self-edge every file carries makes visible: a file
/// appears as the <see cref="Edge.Source"/> of its own self-edge, so the node set is exactly the set
/// of distinct edge sources. An external target is never a source, so it never appears as a file.
/// </para>
/// <para>
/// Each filter matches one part of a file's identifier — its name, folder, whole path or class-style
/// name — and a file without filters matches everything. Selection results are sorted ordinally so
/// reports are stable and reproducible.
/// </para>
/// <para>
/// Cycle detection runs on the subgraph the selected files induce — every raw edge whose source and
/// target are both selected — projected under the files' own identifiers, so a cycle is reported only
/// when every file it passes through is in the selection. Each reported cycle is the closed file path
/// that renders its loop: <c>src/A.cs, src/B.cs, src/A.cs</c> for a two-file cycle.
/// </para>
/// <para>
/// This type is stateless and safe for concurrent use. The lists it returns are fresh copies on every
/// call.
/// </para>
/// </remarks>
internal static class FilesProjection
{
    /// <summary>
    /// Returns the identifiers of the files every filter selects, sorted ordinally.
    /// </summary>
    /// <param name="graph">The project's dependency graph. Must not be <see langword="null"/>.</param>
    /// <param name="filters">The scope's selectors. Must not be <see langword="null"/>.</param>
    /// <returns>The selected files' identifiers, sorted.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="graph"/> or <paramref name="filters"/> is <see langword="null"/>.</exception>
    public static IReadOnlyList<string> Select(Graph graph, IReadOnlyList<Filter> filters)
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(filters);

        return graph.Edges
            .Select(static edge => edge.Source)
            .Distinct(StringComparer.Ordinal)
            .Where(identifier => filters.All(filter => filter.Matches(identifier)))
            .OrderBy(static identifier => identifier, StringComparer.Ordinal)
            .ToArray();
    }

    /// <summary>
    /// Returns the cycles of the selected files' dependency subgraph, each as the closed file path
    /// that renders its loop — first and last entry the same file — in the order the cycle projection
    /// reports them. A cycle is reported only when every file it passes through is selected.
    /// </summary>
    /// <param name="graph">The project's dependency graph. Must not be <see langword="null"/>.</param>
    /// <param name="filters">The scope's selectors. Must not be <see langword="null"/>.</param>
    /// <returns>The selected files' cycles as closed paths, in the cycle projection's order.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="graph"/> or <paramref name="filters"/> is <see langword="null"/>.</exception>
    public static IReadOnlyList<IReadOnlyList<string>> Cycles(Graph graph, IReadOnlyList<Filter> filters)
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(filters);

        IReadOnlyList<string> selected = Select(graph, filters);
        var selectedSet = new HashSet<string>(selected, StringComparer.Ordinal);

        var subgraph = new Graph(graph.Edges.Where(edge =>
            selectedSet.Contains(edge.Source) && selectedSet.Contains(edge.Target)));

        return ArchUnitSharp.Projection.Projection
            .Cycles(subgraph, MapFunctions.Identity)
            .Select(ClosedPath)
            .ToArray();
    }

    private static string[] ClosedPath(ProjectedCycle cycle)
    {
        IReadOnlyList<ProjectedEdge> hops = cycle.Edges;
        string[] path = new string[hops.Count + 1];
        path[0] = hops[0].Source;
        for (int index = 0; index < hops.Count; index++)
        {
            path[index + 1] = hops[index].Target;
        }

        return path;
    }
}
