namespace ArchUnitSharp.Metrics.Projection;

using ArchUnitSharp.Common.Extraction;
using ArchUnitSharp.Metrics;

/// <summary>
/// The metrics module's pure distance projection: turns a rule's extracted file subjects and the
/// project's <see cref="Graph"/> into the <see cref="DistanceInfo"/> values the distance metrics
/// measure. This is the one place a file's internal dependency couplings are computed from the
/// graph's edges.
/// </summary>
/// <remarks>
/// <para>
/// A file's couplings are the distinct internal project files it has edges with: its efferent
/// coupling is the set of targets of its outgoing edges and its afferent coupling the set of sources
/// of its incoming edges, each target or source counted once. An edge is a coupling only when both
/// its endpoints are project files, so self-edges — the marker every file carries — and edges to
/// external targets are never couplings. The coupling facts are computed over the whole project's
/// edges, not just the subjects' files, because a file's instability and coupling factor are
/// properties of its place in the whole graph.
/// </para>
/// <para>
/// <see cref="DistanceInfo.ProjectFileCount"/> is the number of distinct project files, the graph's
/// node set as the set of distinct edge sources. Results keep the order of the supplied files, which
/// the assertion has already sorted, so reports are stable and reproducible.
/// </para>
/// <para>
/// This type is stateless and safe for concurrent use. The lists it returns are fresh copies on every
/// call.
/// </para>
/// </remarks>
internal static class DistanceProjection
{
    /// <summary>
    /// Returns the distance info of each of <paramref name="files"/>, its coupling facts read from
    /// <paramref name="graph"/>'s edges.
    /// </summary>
    /// <param name="files">The files to project, in report order. Must not be <see langword="null"/>.</param>
    /// <param name="graph">The whole project's dependency graph. Must not be <see langword="null"/>.</param>
    /// <returns>One <see cref="DistanceInfo"/> per supplied file, in the same order.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="files"/> or <paramref name="graph"/> is <see langword="null"/>.</exception>
    public static IReadOnlyList<DistanceInfo> Build(IReadOnlyList<FileInfo> files, Graph graph)
    {
        ArgumentNullException.ThrowIfNull(files);
        ArgumentNullException.ThrowIfNull(graph);

        string[] projectFiles = graph.Edges
            .Select(static edge => edge.Source)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        var projectFileSet = new HashSet<string>(projectFiles, StringComparer.Ordinal);
        var outgoing = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        var incoming = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

        foreach (Edge edge in graph.Edges)
        {
            if (edge.External || edge.Source == edge.Target)
            {
                continue;
            }

            if (!projectFileSet.Contains(edge.Source) || !projectFileSet.Contains(edge.Target))
            {
                continue;
            }

            Add(outgoing, edge.Source, edge.Target);
            Add(incoming, edge.Target, edge.Source);
        }

        return files
            .Select(file => new DistanceInfo(
                file.Path,
                file.TypeCount,
                file.AbstractTypeCount,
                file.LinesOfCode,
                Coupling(incoming, file.Path),
                Coupling(outgoing, file.Path),
                projectFiles.Length))
            .ToArray();
    }

    private static void Add(Dictionary<string, HashSet<string>> index, string source, string target)
    {
        if (!index.TryGetValue(source, out HashSet<string>? targets))
        {
            targets = new HashSet<string>(StringComparer.Ordinal);
            index.Add(source, targets);
        }

        targets.Add(target);
    }

    private static int Coupling(Dictionary<string, HashSet<string>> index, string file) =>
        index.TryGetValue(file, out HashSet<string>? partners) ? partners.Count : 0;
}
