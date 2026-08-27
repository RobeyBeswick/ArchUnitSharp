namespace ArchUnitSharp.Extraction;

using ArchUnitSharp.Common.Extraction;

/// <summary>
/// Produces the canonical edge set of a project's dependency graph from its enumerated source files
/// and the raw, per-directive edges <see cref="ImportResolver"/> computes: a self-edge for every file,
/// so a file with no dependencies still appears as a node, and parallel edges merged with their import
/// kinds unioned, so downstream code can assume <c>(source, target)</c> is unique. The pure half of
/// edge normalisation: files and edges in, the canonical edge set out, no filesystem.
/// </summary>
/// <remarks>
/// <para>
/// Every file contributes a self-edge whose import kind is <see cref="ImportKind.None"/> — the edge
/// exists to make the file a node, not to record an import. A file that imports its own namespace
/// contributes a genuine self-edge carrying a real import kind; the two merge, and the union of a real
/// kind with <see cref="ImportKind.None"/> is the real kind, so nothing is lost.
/// </para>
/// <para>
/// Parallel edges — the same <c>(source, target)</c> pair produced by several directives — merge into
/// one edge whose import kinds are the bitwise union of the parallel edges' kinds. The merged edge is
/// external only when every parallel edge was external, so a target that any directive resolved to a
/// project file remains a node. A file that failed to parse contributes no directive edges but still
/// gets its self-edge, so it remains a node.
/// </para>
/// <para>
/// The resulting edges are sorted by source then target, so the output is stable and reports built
/// from it are reproducible.
/// </para>
/// <para>
/// This type is stateless and safe for concurrent use. The list it returns is a fresh copy on every
/// call, and the <see cref="Edge"/> values in it are immutable.
/// </para>
/// </remarks>
public static class ImportEdgeNormaliser
{
    /// <summary>
    /// Returns the canonical edge set for <paramref name="sourceFiles"/> given the raw edges
    /// <paramref name="edges"/> that their directives resolved to.
    /// </summary>
    /// <param name="sourceFiles">The project's enumerated source files. Must not be <see langword="null"/>.</param>
    /// <param name="edges">The raw, per-directive edges for those files. Must not be <see langword="null"/>.</param>
    /// <returns>The canonical edge set, sorted by source then target.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="sourceFiles"/> or <paramref name="edges"/> is <see langword="null"/>.</exception>
    public static IReadOnlyList<Edge> Normalise(
        IReadOnlyList<SourceFile> sourceFiles,
        IReadOnlyList<Edge> edges)
    {
        ArgumentNullException.ThrowIfNull(sourceFiles);
        ArgumentNullException.ThrowIfNull(edges);

        var merged = new Dictionary<(string Source, string Target), (bool External, ImportKind Kinds)>(
            edges.Count + sourceFiles.Count);

        foreach (SourceFile file in sourceFiles)
        {
            Add(file.Identifier, file.Identifier, external: false, ImportKind.None);
        }

        foreach (Edge edge in edges)
        {
            Add(edge.Source, edge.Target, edge.External, edge.ImportKinds);
        }

        return merged
            .Select(static pair => new Edge(pair.Key.Source, pair.Key.Target, pair.Value.External, pair.Value.Kinds))
            .OrderBy(static edge => edge.Source, StringComparer.Ordinal)
            .ThenBy(static edge => edge.Target, StringComparer.Ordinal)
            .ToArray();

        void Add(string source, string target, bool external, ImportKind kinds)
        {
            var key = (source, target);
            if (merged.TryGetValue(key, out (bool External, ImportKind Kinds) existing))
            {
                merged[key] = (existing.External && external, existing.Kinds | kinds);
            }
            else
            {
                merged[key] = (external, kinds);
            }
        }
    }
}
