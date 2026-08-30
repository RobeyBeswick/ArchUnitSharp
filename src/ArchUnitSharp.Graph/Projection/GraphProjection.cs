namespace ArchUnitSharp.Graph.Projection;

using ArchUnitSharp.Common.Extraction;

/// <summary>
/// The graph module's pure projection logic: the query options become a <see cref="GraphSnapshot"/> by
/// filtering, collapsing, aggregating and counting a project's dependency
/// <see cref="ArchUnitSharp.Common.Extraction.Graph"/>. A snapshot is built in four steps: the scope
/// (which files the focus, reachability and dependents restrictions select), the collapse (which
/// label each scoped file maps to), the aggregation (raw dependencies become one edge per label pair,
/// with a count, an external flag and the union of import kinds), and the count (the summary counts
/// the snapshot carries).
/// </summary>
/// <remarks>
/// <para>
/// The scope is every file of the graph when no restriction is set, otherwise the intersection of the
/// files each set restriction selects: <c>focus on</c> is the seed files plus everything within the
/// given hop radius in either direction, <c>reachable from</c> is the transitive closure of outgoing
/// dependencies, and <c>dependents of</c> is the transitive closure of incoming dependencies. Edges
/// into and out of external targets are never traversal edges, because an external target is not a
/// file. A file the scope excludes contributes no node and no edge, so an edge whose target file lies
/// outside the scope is dropped.
/// </para>
/// <para>
/// The collapse rules are applied to each scoped file's identifier in the order they were added; the
/// first rule that relabels a file wins and a file no rule relabels keeps its own identifier. A
/// folder-depth rule relabels every file, so put pattern rules first when combining the two. External
/// targets are never relabelled — they keep the module name as written, and the snapshot's nodes are
/// the scope's file labels only.
/// </para>
/// <para>
/// The edge set is every raw dependency whose source is in scope, excluding self-edges unless self
/// dependencies are included and excluding external edges unless external dependencies are included,
/// and whose target is in scope (for internal edges) or is an external target (for external edges).
/// Each distinct (source label, target label) pair becomes one <see cref="SnapshotEdge"/> whose count
/// is the number of raw edges it replaced, whose external flag is set only when every raw edge was
/// external, and whose import kinds are the union of the raw edges'. A dependency between two files of
/// the same collapsed label — and a file's own self-edge when included — aggregates to a self-loop.
/// Output is sorted — nodes by label, edges by source then target — so reports are reproducible.
/// </para>
/// <para>
/// This type is stateless and safe for concurrent use. The lists it returns are fresh copies on every
/// call.
/// </para>
/// </remarks>
internal static class GraphProjection
{
    /// <summary>
    /// The root bucket: the label a folder-depth collapse gives to a root-level file and to every file
    /// at a depth of zero.
    /// </summary>
    internal const string RootBucket = ".";

    /// <summary>
    /// Returns the identifiers of the files a snapshot's scope covers, sorted ordinally: every file of
    /// the graph when no restriction is set, otherwise the intersection of the files each set
    /// restriction selects.
    /// </summary>
    /// <param name="graph">The project's dependency graph. Must not be <see langword="null"/>.</param>
    /// <param name="options">The query options. Must not be <see langword="null"/>.</param>
    /// <returns>The scope's files, sorted.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="graph"/> or <paramref name="options"/> is <see langword="null"/>.</exception>
    public static IReadOnlyList<string> Select(ArchUnitSharp.Common.Extraction.Graph graph, GraphQueryOptions options)
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(options);

        HashSet<string>? restriction = Restriction(graph, options);
        return graph.Edges
            .Select(static edge => edge.Source)
            .Distinct(StringComparer.Ordinal)
            .Where(file => restriction is null || restriction.Contains(file))
            .OrderBy(static file => file, StringComparer.Ordinal)
            .ToArray();
    }

    /// <summary>
    /// Builds the snapshot the query options describe: the scope's files collapsed to labels as nodes,
    /// the scoped dependencies aggregated to one edge per label pair, and the summary counts. The
    /// title is the options' title.
    /// </summary>
    /// <param name="graph">The project's dependency graph. Must not be <see langword="null"/>.</param>
    /// <param name="options">The query options. Must not be <see langword="null"/>.</param>
    /// <returns>The snapshot the query describes.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="graph"/> or <paramref name="options"/> is <see langword="null"/>.</exception>
    public static GraphSnapshot Build(ArchUnitSharp.Common.Extraction.Graph graph, GraphQueryOptions options)
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(options);

        IReadOnlyList<string> scope = Select(graph, options);
        var scopeSet = new HashSet<string>(scope, StringComparer.Ordinal);

        var filesByLabel = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        foreach (string file in scope)
        {
            string label = LabelOf(options, file);
            if (!filesByLabel.TryGetValue(label, out List<string>? files))
            {
                files = new List<string>();
                filesByLabel[label] = files;
            }

            files.Add(file);
        }

        var nodes = new List<SnapshotNode>(filesByLabel.Count);
        foreach (KeyValuePair<string, List<string>> pair in filesByLabel)
        {
            nodes.Add(new SnapshotNode(
                pair.Key,
                pair.Value.OrderBy(static file => file, StringComparer.Ordinal)));
        }

        var aggregated = new Dictionary<(string Source, string Target), (int Count, bool External, ImportKind Kinds)>();

        foreach (Edge edge in graph.Edges)
        {
            if (!scopeSet.Contains(edge.Source))
            {
                continue;
            }

            if (edge.External)
            {
                if (!options.IncludeExternalDependencies)
                {
                    continue;
                }
            }
            else if (!scopeSet.Contains(edge.Target))
            {
                continue;
            }

            if (edge.Source == edge.Target && !options.IncludeSelfDependencies)
            {
                continue;
            }

            string sourceLabel = LabelOf(options, edge.Source);
            string targetLabel = edge.External ? edge.Target : LabelOf(options, edge.Target);

            var key = (sourceLabel, targetLabel);
            if (aggregated.TryGetValue(key, out (int Count, bool External, ImportKind Kinds) existing))
            {
                aggregated[key] = (
                    existing.Count + 1,
                    existing.External && edge.External,
                    existing.Kinds | edge.ImportKinds);
            }
            else
            {
                aggregated[key] = (1, edge.External, edge.ImportKinds);
            }
        }

        var edges = aggregated
            .Select(static pair => new SnapshotEdge(
                pair.Key.Source,
                pair.Key.Target,
                pair.Value.Count,
                pair.Value.External,
                pair.Value.Kinds))
            .OrderBy(static edge => edge.Source, StringComparer.Ordinal)
            .ThenBy(static edge => edge.Target, StringComparer.Ordinal)
            .ToArray();

        return new GraphSnapshot(
            options.Title,
            nodes.OrderBy(static node => node.Label, StringComparer.Ordinal),
            edges);
    }

    /// <summary>
    /// The restriction set of the query: the intersection of the files each set restriction selects,
    /// or <see langword="null"/> when no restriction is set. The files of each restriction are built
    /// as an ordinal set, so the intersection is order-independent.
    /// </summary>
    private static HashSet<string>? Restriction(
        ArchUnitSharp.Common.Extraction.Graph graph,
        GraphQueryOptions options)
    {
        HashSet<string>? scope = null;
        if (options.Focus is not null)
        {
            scope = Focused(graph, options.Focus, options.FocusDepth);
        }

        if (options.ReachableFrom is not null)
        {
            HashSet<string> reachable = ReachableFrom(graph, options.ReachableFrom);
            scope = scope is null ? reachable : Intersect(scope, reachable);
        }

        if (options.DependentsOf is not null)
        {
            HashSet<string> dependents = DependentsOf(graph, options.DependentsOf);
            scope = scope is null ? dependents : Intersect(scope, dependents);
        }

        return scope;
    }

    /// <summary>
    /// The files the <c>reachable from</c> restriction selects: the files matching
    /// <paramref name="filter"/> plus everything reachable from them by following internal dependency
    /// edges.
    /// </summary>
    private static HashSet<string> ReachableFrom(
        ArchUnitSharp.Common.Extraction.Graph graph,
        Filter filter)
    {
        Dictionary<string, List<string>> outgoing = Outgoing(graph);
        return Closure(MatchingFiles(graph, filter), current =>
            outgoing.TryGetValue(current, out List<string>? targets) ? targets : Enumerable.Empty<string>());
    }

    /// <summary>
    /// The files the <c>dependents of</c> restriction selects: the files matching
    /// <paramref name="filter"/> plus everything that can reach them by following internal dependency
    /// edges backwards.
    /// </summary>
    private static HashSet<string> DependentsOf(
        ArchUnitSharp.Common.Extraction.Graph graph,
        Filter filter)
    {
        Dictionary<string, List<string>> incoming = Incoming(graph);
        return Closure(MatchingFiles(graph, filter), current =>
            incoming.TryGetValue(current, out List<string>? sources) ? sources : Enumerable.Empty<string>());
    }

    /// <summary>
    /// The files the <c>focus on</c> restriction selects: the files matching <paramref name="filter"/>
    /// plus everything within <paramref name="depth"/> hops of them in either direction.
    /// </summary>
    private static HashSet<string> Focused(
        ArchUnitSharp.Common.Extraction.Graph graph,
        Filter filter,
        int depth)
    {
        Dictionary<string, List<string>> outgoing = Outgoing(graph);
        Dictionary<string, List<string>> incoming = Incoming(graph);

        HashSet<string> seed = MatchingFiles(graph, filter);
        var seen = new HashSet<string>(seed, StringComparer.Ordinal);
        var frontier = new Queue<string>(seed);
        int level = 0;
        while (frontier.Count > 0 && level < depth)
        {
            int count = frontier.Count;
            for (int i = 0; i < count; i++)
            {
                string current = frontier.Dequeue();
                foreach (string neighbor in Neighbors(outgoing, incoming, current))
                {
                    if (seen.Add(neighbor))
                    {
                        frontier.Enqueue(neighbor);
                    }
                }
            }

            level++;
        }

        return seen;
    }

    /// <summary>
    /// The files of the graph whose identifier matches <paramref name="filter"/>.
    /// </summary>
    private static HashSet<string> MatchingFiles(ArchUnitSharp.Common.Extraction.Graph graph, Filter filter)
    {
        ArgumentNullException.ThrowIfNull(filter);
        return graph.Edges
            .Select(static edge => edge.Source)
            .Distinct(StringComparer.Ordinal)
            .Where(file => filter.Matches(file))
            .ToHashSet(StringComparer.Ordinal);
    }

    /// <summary>
    /// The transitive closure of <paramref name="seed"/> under <paramref name="next"/>. External
    /// targets and self-edges are not traversal edges, which the adjacency builders already exclude.
    /// </summary>
    private static HashSet<string> Closure(
        HashSet<string> seed,
        Func<string, IEnumerable<string>> next)
    {
        var seen = new HashSet<string>(seed, StringComparer.Ordinal);
        var frontier = new Queue<string>(seed);
        while (frontier.Count > 0)
        {
            string current = frontier.Dequeue();
            foreach (string target in next(current))
            {
                if (seen.Add(target))
                {
                    frontier.Enqueue(target);
                }
            }
        }

        return seen;
    }

    /// <summary>
    /// The internal outgoing adjacency of the graph: each file's internal non-self targets, so
    /// external targets — which are not files — and marker self-edges never become traversal edges.
    /// </summary>
    private static Dictionary<string, List<string>> Outgoing(ArchUnitSharp.Common.Extraction.Graph graph)
    {
        var map = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        foreach (Edge edge in graph.Edges)
        {
            if (edge.External || edge.Source == edge.Target)
            {
                continue;
            }

            if (!map.TryGetValue(edge.Source, out List<string>? targets))
            {
                targets = new List<string>();
                map[edge.Source] = targets;
            }

            targets.Add(edge.Target);
        }

        return map;
    }

    /// <summary>
    /// The internal incoming adjacency of the graph: for each file, the internal non-self sources that
    /// reach it.
    /// </summary>
    private static Dictionary<string, List<string>> Incoming(ArchUnitSharp.Common.Extraction.Graph graph)
    {
        var map = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        foreach (Edge edge in graph.Edges)
        {
            if (edge.External || edge.Source == edge.Target)
            {
                continue;
            }

            if (!map.TryGetValue(edge.Target, out List<string>? sources))
            {
                sources = new List<string>();
                map[edge.Target] = sources;
            }

            sources.Add(edge.Source);
        }

        return map;
    }

    /// <summary>
    /// The internal neighbors of a file in either direction: its outgoing targets and its incoming
    /// sources.
    /// </summary>
    private static IEnumerable<string> Neighbors(
        Dictionary<string, List<string>> outgoing,
        Dictionary<string, List<string>> incoming,
        string current)
    {
        if (outgoing.TryGetValue(current, out List<string>? targets))
        {
            foreach (string target in targets)
            {
                yield return target;
            }
        }

        if (incoming.TryGetValue(current, out List<string>? sources))
        {
            foreach (string source in sources)
            {
                yield return source;
            }
        }
    }

    /// <summary>
    /// The intersection of two ordinal sets.
    /// </summary>
    private static HashSet<string> Intersect(HashSet<string> left, HashSet<string> right)
    {
        var result = new HashSet<string>(StringComparer.Ordinal);
        foreach (string file in left)
        {
            if (right.Contains(file))
            {
                result.Add(file);
            }
        }

        return result;
    }

    /// <summary>
    /// The label a scoped file collapses to: the first collapse rule that relabels it, or its own
    /// identifier when no rule relabels it.
    /// </summary>
    private static string LabelOf(GraphQueryOptions options, string identifier)
    {
        foreach (CollapseRule rule in options.Collapse)
        {
            if (rule is CollapseRule.FolderDepth { Depth: int depth })
            {
                return FolderLabel(identifier, depth);
            }

            if (rule is CollapseRule.Pattern { Filter: Filter filter } && filter.Matches(identifier))
            {
                return filter.Pattern.Glob;
            }
        }

        return identifier;
    }

    /// <summary>
    /// The folder label of a file at a given depth: its folder truncated to the first
    /// <paramref name="depth"/> path segments, the whole folder when it has fewer segments, and the
    /// root bucket for a root-level file, a depth of zero, or a folder whose truncated label would
    /// be empty. Empty segments — an absolute identifier's leading separator — are skipped, so a
    /// depth of one on an absolute identifier names its first folder rather than the root.
    /// </summary>
    private static string FolderLabel(string identifier, int depth)
    {
        if (depth == 0)
        {
            return RootBucket;
        }

        int separator = identifier.LastIndexOf('/');
        if (separator < 0)
        {
            return RootBucket;
        }

        string folder = identifier.Substring(0, separator);
        string[] segments = folder
            .Split('/')
            .Where(static segment => segment.Length > 0)
            .Take(depth)
            .ToArray();
        return segments.Length == 0 ? RootBucket : string.Join('/', segments);
    }
}
