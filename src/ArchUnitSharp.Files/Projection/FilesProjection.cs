namespace ArchUnitSharp.Files.Projection;

using ArchUnitSharp.Common.Extraction;

/// <summary>
/// The files module's pure selection logic: which files of a <see cref="Graph"/> a scope's list of
/// <see cref="Filter"/> instances selects. Filters combine with AND — a file is selected when every
/// filter matches it — and the empty filter list selects every file.
/// </summary>
/// <remarks>
/// <para>
/// The files of a graph are its nodes, which the self-edge every file carries makes visible: a file
/// appears as the <see cref="Edge.Source"/> of its own self-edge, so the node set is exactly the set
/// of distinct edge sources. An external target is never a source, so it never appears as a file.
/// </para>
/// <para>
/// Each filter matches one part of a file's identifier — its name, folder, whole path or class-style
/// name — and a file without filters matches everything. The result is sorted ordinally so reports
/// are stable and reproducible.
/// </para>
/// <para>
/// This type is stateless and safe for concurrent use. The list it returns is a fresh copy on every
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
}
