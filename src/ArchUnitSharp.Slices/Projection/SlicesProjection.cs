namespace ArchUnitSharp.Slices.Projection;

using ArchUnitSharp.Common.Extraction;
using ArchUnitSharp.Projection;

/// <summary>
/// The slices module's pure projection logic: which files a list of <see cref="SliceDefinition"/>
/// instances assigns to which slices, which slices a graph's files form, which dependency edges a
/// <c>contain dependency(from, to)</c> rule counts, and the ready-made <see cref="MapFunction"/>
/// hooks a consumer passes to the projection layer for direct use.
/// </summary>
/// <remarks>
/// <para>
/// A file belongs to exactly one slice — the name the first definition that matches it captures — or
/// to none, so the projection is a partition of a subset of the graph's files. The files of a graph
/// are its nodes, visible through the self-edge every file carries. A file no definition captures a
/// name for is unsliced and appears in no slice; an external target is never a file, so it is never
/// sliced.
/// </para>
/// <para>
/// A <c>contain dependency(from, to)</c> rule counts an internal, non-self edge whose importing file
/// is sliced and matches <c>from</c> and whose imported file matches <c>to</c>; the dependency is
/// contained in the importing file's slice, and the imported file need not be sliced, because a
/// dependency can leave the slicing. Results are sorted — slices by name, files ordinally,
/// dependencies by slice then source then target — so reports are stable and reproducible.
/// </para>
/// <para>
/// The <see cref="Map"/> hook is what the public <c>slice by pattern</c>, <c>slice by regex</c> and
/// <c>slice by file suffix</c> projections are built from: it relabels each edge's endpoints to their
/// slice labels and drops any edge whose endpoint is not sliced, external edges included.
/// <see cref="DiagramMap"/> is the diagram-adherence counterpart: it keeps external edges, relabelling
/// an external edge's target to the module name as written, which is how an <c>adhere to diagram</c>
/// rule and the generated PlantUML report see dependencies that leave the project.
/// </para>
/// <para>
/// This type is stateless and safe for concurrent use. The lists it returns are fresh copies on every
/// call.
/// </para>
/// </remarks>
internal static class SlicesProjection
{
    /// <summary>
    /// Returns the identifiers of the sliced files — the files some definition assigns to a slice —
    /// sorted ordinally.
    /// </summary>
    /// <param name="graph">The project's dependency graph. Must not be <see langword="null"/>.</param>
    /// <param name="definitions">The slice definitions. Must not be <see langword="null"/>.</param>
    /// <returns>The sliced files' identifiers, sorted.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="graph"/> or <paramref name="definitions"/> is <see langword="null"/>.</exception>
    public static IReadOnlyList<string> SlicedFiles(Graph graph, IReadOnlyList<SliceDefinition> definitions)
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(definitions);

        return SliceByFile(graph, definitions)
            .Keys
            .OrderBy(static file => file, StringComparer.Ordinal)
            .ToArray();
    }

    /// <summary>
    /// Returns the names of the distinct slices the graph's files form, sorted ordinally. A slice
    /// exists exactly when at least one file is assigned to it.
    /// </summary>
    /// <param name="graph">The project's dependency graph. Must not be <see langword="null"/>.</param>
    /// <param name="definitions">The slice definitions. Must not be <see langword="null"/>.</param>
    /// <returns>The slice names, sorted.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="graph"/> or <paramref name="definitions"/> is <see langword="null"/>.</exception>
    public static IReadOnlyList<string> Slices(Graph graph, IReadOnlyList<SliceDefinition> definitions)
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(definitions);

        return SliceByFile(graph, definitions)
            .Values
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static slice => slice, StringComparer.Ordinal)
            .ToArray();
    }

    /// <summary>
    /// Returns the identifiers of the sliced files whose whole path matches <paramref name="filter"/>,
    /// sorted ordinally.
    /// </summary>
    /// <param name="graph">The project's dependency graph. Must not be <see langword="null"/>.</param>
    /// <param name="definitions">The slice definitions. Must not be <see langword="null"/>.</param>
    /// <param name="filter">The whole-path filter. Must not be <see langword="null"/>.</param>
    /// <returns>The matching sliced files' identifiers, sorted.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="graph"/>, <paramref name="definitions"/> or <paramref name="filter"/> is <see langword="null"/>.</exception>
    public static IReadOnlyList<string> FilesOf(
        Graph graph,
        IReadOnlyList<SliceDefinition> definitions,
        Filter filter)
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(definitions);
        ArgumentNullException.ThrowIfNull(filter);

        return SliceByFile(graph, definitions)
            .Keys
            .Where(file => filter.Matches(file))
            .OrderBy(static file => file, StringComparer.Ordinal)
            .ToArray();
    }

    /// <summary>
    /// Returns the identifiers of the graph's files whose whole path matches <paramref name="filter"/>,
    /// sorted ordinally. Sliced or not, every file of the graph is considered, because a rule's
    /// <c>to</c> filter can name files outside the slices.
    /// </summary>
    /// <param name="graph">The project's dependency graph. Must not be <see langword="null"/>.</param>
    /// <param name="filter">The whole-path filter. Must not be <see langword="null"/>.</param>
    /// <returns>The matching files' identifiers, sorted.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="graph"/> or <paramref name="filter"/> is <see langword="null"/>.</exception>
    public static IReadOnlyList<string> MatchingFiles(Graph graph, Filter filter)
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(filter);

        return graph.Edges
            .Select(static edge => edge.Source)
            .Distinct(StringComparer.Ordinal)
            .Where(file => filter.Matches(file))
            .OrderBy(static file => file, StringComparer.Ordinal)
            .ToArray();
    }

    /// <summary>
    /// Returns the dependencies a <c>contain dependency(from, to)</c> rule counts: every internal,
    /// non-self edge from a sliced file matching <paramref name="from"/> to a file matching
    /// <paramref name="to"/>, each carried with the slice that contains it. The importing file's slice
    /// is the slice the dependency is contained in; the imported file need not be sliced, because a
    /// dependency can leave the slicing. The result is sorted by slice, source, then target.
    /// </summary>
    /// <param name="graph">The project's dependency graph. Must not be <see langword="null"/>.</param>
    /// <param name="definitions">The slice definitions. Must not be <see langword="null"/>.</param>
    /// <param name="from">The whole-path filter the importing file must match. Must not be <see langword="null"/>.</param>
    /// <param name="to">The whole-path filter the imported file must match. Must not be <see langword="null"/>.</param>
    /// <returns>The counted dependencies, sorted by slice, source, then target.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="graph"/>, <paramref name="definitions"/>, <paramref name="from"/> or <paramref name="to"/> is <see langword="null"/>.</exception>
    public static IReadOnlyList<SliceDependency> Dependencies(
        Graph graph,
        IReadOnlyList<SliceDefinition> definitions,
        Filter from,
        Filter to)
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(definitions);
        ArgumentNullException.ThrowIfNull(from);
        ArgumentNullException.ThrowIfNull(to);

        Dictionary<string, string> sliceByFile = SliceByFile(graph, definitions);

        var result = new List<SliceDependency>();
        foreach (Edge edge in graph.Edges)
        {
            if (edge.External || edge.Source == edge.Target)
            {
                continue;
            }

            if (!sliceByFile.TryGetValue(edge.Source, out string? sourceSlice))
            {
                continue;
            }

            if (!from.Matches(edge.Source) || !to.Matches(edge.Target))
            {
                continue;
            }

            result.Add(new SliceDependency(sourceSlice, edge.Source, edge.Target));
        }

        return result
            .OrderBy(static dependency => dependency.Slice, StringComparer.Ordinal)
            .ThenBy(static dependency => dependency.Source, StringComparer.Ordinal)
            .ThenBy(static dependency => dependency.Target, StringComparer.Ordinal)
            .ToArray();
    }

    /// <summary>
    /// Returns the name of the slice <paramref name="identifier"/> is assigned to by the first
    /// definition that captures one, or <see langword="null"/> when no definition slices the file.
    /// </summary>
    /// <param name="definitions">The slice definitions. Must not be <see langword="null"/>.</param>
    /// <param name="identifier">The file's graph identifier. Must not be <see langword="null"/>.</param>
    /// <returns>The file's slice name, or <see langword="null"/> when the file is unsliced.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="definitions"/> or <paramref name="identifier"/> is <see langword="null"/>.</exception>
    public static string? SliceOf(IReadOnlyList<SliceDefinition> definitions, string identifier)
    {
        ArgumentNullException.ThrowIfNull(definitions);
        ArgumentNullException.ThrowIfNull(identifier);

        foreach (SliceDefinition definition in definitions)
        {
            string? slice = definition.SliceOf(identifier);
            if (slice is not null)
            {
                return slice;
            }
        }

        return null;
    }

    /// <summary>
    /// The relabelling hook the public slicing projections are built from: given a file-to-slice
    /// assignment, it maps an edge to the projected edge between its endpoints' slice labels, and
    /// returns <see langword="null"/> to drop the edge when either endpoint is not sliced or the edge
    /// is external. Self-edges map to a self-loop on the file's slice, which node projection consumes
    /// and the edge and cycle projections filter out.
    /// </summary>
    /// <param name="labelOf">The file-to-slice assignment. Must not be <see langword="null"/>.</param>
    /// <returns>A <see cref="MapFunction"/> that relabels the graph by slice.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="labelOf"/> is <see langword="null"/>.</exception>
    public static MapFunction Map(Func<string, string?> labelOf)
    {
        ArgumentNullException.ThrowIfNull(labelOf);

        return edge =>
        {
            if (edge.External)
            {
                return null;
            }

            string? source = labelOf(edge.Source);
            string? target = labelOf(edge.Target);
            if (source is null || target is null)
            {
                return null;
            }

            return new ProjectedEdge(source, target, external: false, edge.ImportKinds, new[] { edge });
        };
    }

    /// <summary>
    /// The file-to-slice assignment the <c>slice by file suffix</c> projection uses: a file's slice is
    /// its extension — the final dot and what follows it — so <c>src/Models/Car.cs</c> slices to
    /// <c>.cs</c>. A file with no extension has no suffix and slices to nothing.
    /// </summary>
    /// <param name="identifier">The file's graph identifier. Must not be <see langword="null"/>.</param>
    /// <returns>The file's extension including the dot, or <see langword="null"/> when the file has none.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="identifier"/> is <see langword="null"/>.</exception>
    public static string? FileSuffix(string identifier)
    {
        ArgumentNullException.ThrowIfNull(identifier);

        int separator = identifier.LastIndexOf('/');
        int dot = identifier.LastIndexOf('.');
        return dot < 0 || dot < separator ? null : identifier.Substring(dot);
    }

    /// <summary>
    /// The relabelling hook the <c>adhere to diagram</c> rule and the generated PlantUML report project
    /// the graph with: like <see cref="Map"/>, it relabels each edge's endpoints to their slice labels,
    /// but an external edge is kept with its target relabelled to the module name as written, so a
    /// dependency that leaves the project appears in the projected view. A self-edge maps to a
    /// self-loop on the file's slice, which the projection layer filters out of the edge set.
    /// </summary>
    /// <param name="labelOf">The file-to-slice assignment. Must not be <see langword="null"/>.</param>
    /// <returns>A <see cref="MapFunction"/> that relabels the graph by slice, keeping external edges.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="labelOf"/> is <see langword="null"/>.</exception>
    public static MapFunction DiagramMap(Func<string, string?> labelOf)
    {
        ArgumentNullException.ThrowIfNull(labelOf);

        return edge =>
        {
            string? source = labelOf(edge.Source);
            if (source is null)
            {
                return null;
            }

            string? target = edge.External ? edge.Target : labelOf(edge.Target);
            if (target is null)
            {
                return null;
            }

            return new ProjectedEdge(source, target, edge.External, edge.ImportKinds, new[] { edge });
        };
    }

    /// <summary>
    /// The sliced files of the graph with their slice names, as a file-to-slice map. A file appears
    /// exactly once, with the name the first definition that matches it captures.
    /// </summary>
    internal static Dictionary<string, string> SliceByFile(
        Graph graph,
        IReadOnlyList<SliceDefinition> definitions)
    {
        var map = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (string file in graph.Edges
            .Select(static edge => edge.Source)
            .Distinct(StringComparer.Ordinal))
        {
            string? slice = SliceOf(definitions, file);
            if (slice is not null)
            {
                map[file] = slice;
            }
        }

        return map;
    }
}
