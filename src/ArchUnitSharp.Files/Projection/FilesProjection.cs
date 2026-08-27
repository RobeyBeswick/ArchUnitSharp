namespace ArchUnitSharp.Files.Projection;

using ArchUnitSharp.Common.Extraction;
using ArchUnitSharp.Projection;

/// <summary>
/// The files module's pure projection logic: which files of a <see cref="Graph"/> a scope's list of
/// <see cref="Filter"/> instances selects, which cycles the selected files' dependencies form, which
/// of their edges are the dependencies of a depend-on-files rule, which external modules a
/// depend-on-external-modules rule's object names, and the per-file detail an <c>adhere to</c> rule
/// hands its custom predicate. File filters combine with AND — a file is selected when every filter
/// matches it — while external-module filters combine with OR, and the empty filter list selects
/// everything.
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
/// An external module is the target of an external edge: a name no file in the project declares, kept
/// as written. External-module filters match that name as a whole against a glob — a
/// <see cref="MatchTarget.Path"/> filter — and combine with OR, so an object narrowed by
/// <c>Matching("System.*")</c> and <c>Matching("Newtonsoft.*")</c> names the modules in either family.
/// The empty filter list names every external module.
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
    /// Returns the per-file detail an <c>adhere to</c> rule hands its custom predicate for one file:
    /// the identifier, the name without its extension, the extension, the directory and the full
    /// source text, with the non-blank line count of that text. The name, extension and directory are
    /// derived from the identifier the same way the kernel derives its match targets — a file at
    /// <c>src/Models/Car.cs</c> has the name <c>Car</c>, the extension <c>.cs</c> and the directory
    /// <c>src/Models</c>; a root-level file has an empty directory and a file with no extension has an
    /// empty extension. A line of the source whose content is only whitespace is blank.
    /// </summary>
    /// <param name="identifier">The file's graph identifier. Must not be <see langword="null"/>.</param>
    /// <param name="sourceText">The file's full source text. Must not be <see langword="null"/>.</param>
    /// <returns>The file's detail.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="identifier"/> or <paramref name="sourceText"/> is <see langword="null"/>.</exception>
    public static FileDetail Detail(string identifier, string sourceText)
    {
        ArgumentNullException.ThrowIfNull(identifier);
        ArgumentNullException.ThrowIfNull(sourceText);

        string name = FilenameOf(identifier);
        int dot = name.LastIndexOf('.');
        string nameWithoutExtension = dot < 0 ? name : name.Substring(0, dot);
        string extension = dot < 0 ? string.Empty : name.Substring(dot);

        return new FileDetail(
            identifier,
            nameWithoutExtension,
            extension,
            PathWithoutFilenameOf(identifier),
            sourceText,
            NonBlankLineCountOf(sourceText));
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

    /// <summary>
    /// Returns the dependency edges of a <c>should (not) depend on files</c> rule: every edge from a
    /// file the <paramref name="subjectFilters"/> select to a file the
    /// <paramref name="objectFilters"/> select. A self-edge is not a dependency — a file never depends
    /// on itself — and an external edge's target is not a file, so neither is ever returned. The
    /// result is sorted by source then target, so reports are reproducible.
    /// </summary>
    /// <param name="graph">The project's dependency graph. Must not be <see langword="null"/>.</param>
    /// <param name="subjectFilters">The rule's subject selectors. Must not be <see langword="null"/>.</param>
    /// <param name="objectFilters">The rule's object selectors. Must not be <see langword="null"/>.</param>
    /// <returns>The subject-to-object dependency edges, sorted by source then target.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="graph"/>, <paramref name="subjectFilters"/> or <paramref name="objectFilters"/> is <see langword="null"/>.</exception>
    public static IReadOnlyList<Edge> Dependencies(
        Graph graph,
        IReadOnlyList<Filter> subjectFilters,
        IReadOnlyList<Filter> objectFilters)
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(subjectFilters);
        ArgumentNullException.ThrowIfNull(objectFilters);

        var subject = new HashSet<string>(Select(graph, subjectFilters), StringComparer.Ordinal);
        var objects = new HashSet<string>(Select(graph, objectFilters), StringComparer.Ordinal);

        return graph.Edges
            .Where(edge => edge.Source != edge.Target
                && !edge.External
                && subject.Contains(edge.Source)
                && objects.Contains(edge.Target))
            .OrderBy(static edge => edge.Source, StringComparer.Ordinal)
            .ThenBy(static edge => edge.Target, StringComparer.Ordinal)
            .ToArray();
    }

    /// <summary>
    /// Returns the names of the external modules of a <c>should (not) depend on external modules</c>
    /// rule's object: every distinct target of an external edge whose name matches at least one
    /// <paramref name="filters"/>, or every distinct external target when there are no filters. The
    /// result is sorted ordinally, so reports are reproducible.
    /// </summary>
    /// <param name="graph">The project's dependency graph. Must not be <see langword="null"/>.</param>
    /// <param name="filters">The object's selectors. Must not be <see langword="null"/>.</param>
    /// <returns>The matching external module names, sorted.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="graph"/> or <paramref name="filters"/> is <see langword="null"/>.</exception>
    public static IReadOnlyList<string> ExternalModules(Graph graph, IReadOnlyList<Filter> filters)
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(filters);

        return graph.Edges
            .Where(static edge => edge.External)
            .Select(static edge => edge.Target)
            .Distinct(StringComparer.Ordinal)
            .Where(name => filters.Count == 0 || filters.Any(filter => filter.Matches(name)))
            .OrderBy(static name => name, StringComparer.Ordinal)
            .ToArray();
    }

    /// <summary>
    /// Returns the dependency edges of a <c>should (not) depend on external modules</c> rule: every
    /// external edge from a file the <paramref name="subjectFilters"/> select to an external module
    /// whose name matches at least one <paramref name="objectFilters"/>, or every such edge when there
    /// are no object filters. An internal edge's target is a file, not a module, so it is never
    /// returned. The result is sorted by source then target, so reports are reproducible.
    /// </summary>
    /// <param name="graph">The project's dependency graph. Must not be <see langword="null"/>.</param>
    /// <param name="subjectFilters">The rule's subject selectors. Must not be <see langword="null"/>.</param>
    /// <param name="objectFilters">The rule's object selectors. Must not be <see langword="null"/>.</param>
    /// <returns>The subject-to-external-module dependency edges, sorted by source then target.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="graph"/>, <paramref name="subjectFilters"/> or <paramref name="objectFilters"/> is <see langword="null"/>.</exception>
    public static IReadOnlyList<Edge> ExternalDependencies(
        Graph graph,
        IReadOnlyList<Filter> subjectFilters,
        IReadOnlyList<Filter> objectFilters)
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(subjectFilters);
        ArgumentNullException.ThrowIfNull(objectFilters);

        var subject = new HashSet<string>(Select(graph, subjectFilters), StringComparer.Ordinal);

        return graph.Edges
            .Where(edge => edge.External
                && subject.Contains(edge.Source)
                && (objectFilters.Count == 0 || objectFilters.Any(filter => filter.Matches(edge.Target))))
            .OrderBy(static edge => edge.Source, StringComparer.Ordinal)
            .ThenBy(static edge => edge.Target, StringComparer.Ordinal)
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

    private static string FilenameOf(string path)
    {
        int separator = path.LastIndexOf('/');
        return separator < 0 ? path : path.Substring(separator + 1);
    }

    private static string PathWithoutFilenameOf(string path)
    {
        int separator = path.LastIndexOf('/');
        return separator < 0 ? string.Empty : path.Substring(0, separator);
    }

    private static int NonBlankLineCountOf(string sourceText)
    {
        int count = 0;
        foreach (string line in sourceText.Split('\n'))
        {
            if (!string.IsNullOrWhiteSpace(line))
            {
                count++;
            }
        }

        return count;
    }
}
