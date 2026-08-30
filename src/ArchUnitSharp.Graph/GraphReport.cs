namespace ArchUnitSharp.Graph;

using System.Text;
using ArchUnitSharp.Common.Extraction;
using KernelGraph = ArchUnitSharp.Common.Extraction.Graph;

/// <summary>
/// The graph-reports domain module's fluent surface: the immutable query options that shape the
/// snapshot of one project's <see cref="ArchUnitSharp.Common.Extraction.Graph"/>. It is the ENTRY of
/// a report chain — built from the entry points <c>Project.ProjectGraph()</c> / <c>Project.Graph()</c>
/// — and the accumulator of the query options that filter, collapse, aggregate and count the graph
/// into a <see cref="GraphSnapshot"/>.
/// </summary>
/// <remarks>
/// <para>
/// A report is built in two steps: build a snapshot, then render it. <see cref="Build"/> produces the
/// snapshot every renderer consumes; <see cref="Check"/> is the rule terminal — it reports one
/// <see cref="EmptyTestViolation"/> when the snapshot's scope matched no files, unless the query's
/// check options allow empty tests, and nothing otherwise. The options are the modifiers of the chain:
/// <see cref="IncludingExternalDependencies"/>, <see cref="IncludingSelfDependencies"/>,
/// <see cref="FocusingOn"/>, <see cref="ReachableFrom"/>, <see cref="DependentsOf"/>,
/// <see cref="CollapsedToFolderDepth"/>, <see cref="CollapsedByPattern"/>, <see cref="Titled"/> and
/// <see cref="WithCheckOptions"/>. They combine freely and are order-independent, except that
/// collapse rules apply in the order they were added and the first rule that relabels a file wins.
/// </para>
/// <para>
/// Every method returns a new <see cref="GraphReport"/> instance and never mutates the one it was
/// called on, so a half-built query can be stored in a variable and branched from without one branch
/// seeing another's options. This type is immutable and safe for concurrent use.
/// </para>
/// </remarks>
public sealed class GraphReport : ICheckable
{
    private readonly KernelGraph _graph;
    private readonly GraphQueryOptions _options;

    /// <summary>
    /// Creates a report query over every file of <paramref name="graph"/> with the default options: no
    /// restrictions, external and self dependencies excluded, no collapse and no title.
    /// </summary>
    /// <param name="graph">The project's dependency graph. Must not be <see langword="null"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="graph"/> is <see langword="null"/>.</exception>
    public GraphReport(KernelGraph graph)
    {
        ArgumentNullException.ThrowIfNull(graph);
        _graph = graph;
        _options = GraphQueryOptions.Default;
    }

    private GraphReport(KernelGraph graph, GraphQueryOptions options)
    {
        _graph = graph;
        _options = options;
    }

    /// <summary>
    /// <c>including external dependencies</c>: the snapshot includes edges whose target lies outside
    /// the project, and each such external target appears as a node. Returns a new
    /// <see cref="GraphReport"/>; the current query is unchanged.
    /// </summary>
    /// <returns>A new query that includes external dependencies.</returns>
    public GraphReport IncludingExternalDependencies() =>
        With(static options => options with { IncludeExternalDependencies = true });

    /// <summary>
    /// <c>including self dependencies</c>: the snapshot includes the per-file self-edge every file
    /// carries, aggregated as a self-loop on each file's node. Returns a new <see cref="GraphReport"/>;
    /// the current query is unchanged.
    /// </summary>
    /// <returns>A new query that includes self dependencies.</returns>
    public GraphReport IncludingSelfDependencies() =>
        With(static options => options with { IncludeSelfDependencies = true });

    /// <summary>
    /// <c>focusing on(pattern, depth)</c>: the scope narrows to the files whose whole path matches
    /// <paramref name="glob"/> plus every file within <paramref name="depth"/> hops of them, in either
    /// direction. A depth of zero selects exactly the matching files. Returns a new
    /// <see cref="GraphReport"/>; the current query is unchanged.
    /// </summary>
    /// <param name="glob">The glob to match each file's whole path against. Must not be <see langword="null"/> or empty.</param>
    /// <param name="depth">The hop radius around the matching files; must not be negative.</param>
    /// <returns>A new query focused on the matching files and their neighbourhood.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="glob"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="glob"/> is empty.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="depth"/> is negative.</exception>
    public GraphReport FocusingOn(string glob, int depth)
    {
        if (depth < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(depth), depth, "Focus depth must not be negative.");
        }

        return With(options => options with
        {
            Focus = new Filter(new Pattern(glob), MatchTarget.Path),
            FocusDepth = depth,
        });
    }

    /// <summary>
    /// <c>reachable from(pattern)</c>: the scope narrows to the files whose whole path matches
    /// <paramref name="glob"/> plus every file reachable from them by following dependency edges.
    /// Returns a new <see cref="GraphReport"/>; the current query is unchanged.
    /// </summary>
    /// <param name="glob">The glob to match each file's whole path against. Must not be <see langword="null"/> or empty.</param>
    /// <returns>A new query restricted to the matching files and everything they can reach.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="glob"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="glob"/> is empty.</exception>
    public GraphReport ReachableFrom(string glob) =>
        With(options => options with { ReachableFrom = new Filter(new Pattern(glob), MatchTarget.Path) });

    /// <summary>
    /// <c>dependents of(pattern)</c>: the scope narrows to the files whose whole path matches
    /// <paramref name="glob"/> plus every file that can reach them by following dependency edges.
    /// Returns a new <see cref="GraphReport"/>; the current query is unchanged.
    /// </summary>
    /// <param name="glob">The glob to match each file's whole path against. Must not be <see langword="null"/> or empty.</param>
    /// <returns>A new query restricted to the matching files and everything that can reach them.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="glob"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="glob"/> is empty.</exception>
    public GraphReport DependentsOf(string glob) =>
        With(options => options with { DependentsOf = new Filter(new Pattern(glob), MatchTarget.Path) });

    /// <summary>
    /// <c>collapsed to folder depth(n)</c>: each file relabels to its folder truncated to the first
    /// <paramref name="depth"/> path segments, the whole folder when it has fewer, and the root bucket
    /// for a root-level file or a depth of zero. Dependencies between files of the same label
    /// aggregate to a self-loop. Returns a new <see cref="GraphReport"/>; the current query is
    /// unchanged.
    /// </summary>
    /// <param name="depth">The folder depth to collapse to; must not be negative.</param>
    /// <returns>A new query that collapses to the folder at the given depth.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="depth"/> is negative.</exception>
    public GraphReport CollapsedToFolderDepth(int depth)
    {
        if (depth < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(depth), depth, "Folder depth must not be negative.");
        }

        return With(options => options with
        {
            Collapse = Append(options.Collapse, new CollapseRule.FolderDepth(depth)),
        });
    }

    /// <summary>
    /// <c>collapsed by pattern</c>: the files whose whole path matches <paramref name="glob"/> relabel
    /// to one bucket labeled with the glob itself. A collapse rule is applied to a file only when no
    /// earlier rule relabelled it, so later pattern rules never see the files a folder-depth rule
    /// already relabelled. Returns a new <see cref="GraphReport"/>; the current query is unchanged.
    /// </summary>
    /// <param name="glob">The glob to match each file's whole path against. Must not be <see langword="null"/> or empty.</param>
    /// <returns>A new query that collapses the matching files to a single bucket.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="glob"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="glob"/> is empty.</exception>
    public GraphReport CollapsedByPattern(string glob) =>
        With(options => options with
        {
            Collapse = Append(options.Collapse, new CollapseRule.Pattern(new Filter(new Pattern(glob), MatchTarget.Path))),
        });

    /// <summary>
    /// <c>titled</c>: the snapshot's title. Returns a new <see cref="GraphReport"/>; the current query
    /// is unchanged.
    /// </summary>
    /// <param name="title">The title the snapshot carries. Must not be <see langword="null"/>; may be empty.</param>
    /// <returns>A new query with the given title.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="title"/> is <see langword="null"/>.</exception>
    public GraphReport Titled(string title)
    {
        ArgumentNullException.ThrowIfNull(title);
        return With(options => options with { Title = title });
    }

    /// <summary>
    /// <c>with check options</c>: the options bag the rule terminal honours when the snapshot's scope
    /// matched nothing. Returns a new <see cref="GraphReport"/>; the current query is unchanged.
    /// </summary>
    /// <param name="options">The check options. Must not be <see langword="null"/>.</param>
    /// <returns>A new query carrying the given check options.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is <see langword="null"/>.</exception>
    public GraphReport WithCheckOptions(CheckOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return With(current => current with { CheckOptions = options });
    }

    /// <summary>
    /// Builds the snapshot the query options describe: the scope's files collapsed to labels as nodes,
    /// the scoped dependencies aggregated to one edge per label pair, and the summary counts. The
    /// snapshot is the immutable data contract every renderer consumes.
    /// </summary>
    /// <returns>The snapshot the query describes.</returns>
    public GraphSnapshot Build() => Projection.GraphProjection.Build(_graph, _options);

    /// <summary>
    /// Checks that the query's scope matched some files. A scope that matched nothing is an
    /// <see cref="EmptyTestViolation"/> naming the query, unless the query's check options allow empty
    /// tests; otherwise the check reports nothing. The <paramref name="options"/> argument, when given,
    /// overrides the query's own check options for this call.
    /// </summary>
    /// <param name="options">The options to check with; <see langword="null"/> means the query's own check options, which default to <see cref="CheckOptions"/>.</param>
    /// <returns>The violations found; empty when the scope matched some files or empty tests are allowed.</returns>
    public IReadOnlyList<Violation> Check(CheckOptions? options = null)
    {
        IReadOnlyList<string> scope = Projection.GraphProjection.Select(_graph, _options);
        if (scope.Count == 0)
        {
            return EmptyTestGuard.Guard(DescribeScope(), options ?? _options.CheckOptions);
        }

        return Array.Empty<Violation>();
    }

    /// <inheritdoc/>
    void ICheckable.ProhibitExternalImplementation()
    {
    }

    /// <summary>
    /// The project's dependency graph the query draws its report from.
    /// </summary>
    internal KernelGraph SourceGraph => _graph;

    /// <summary>
    /// The accumulated query options.
    /// </summary>
    internal GraphQueryOptions QueryOptions => _options;

    /// <summary>
    /// Describes this query as a report, for the empty-test guard: the entry phrase <c>project graph</c>
    /// followed by one clause per set restriction, in the option's own words.
    /// </summary>
    internal string DescribeScope()
    {
        var builder = new StringBuilder("project graph");
        if (_options.Focus is not null)
        {
            builder.Append(" focusing on '");
            builder.Append(_options.Focus.Pattern.Glob);
            builder.Append("' with depth ");
            builder.Append(_options.FocusDepth);
        }

        if (_options.ReachableFrom is not null)
        {
            builder.Append(" reachable from '");
            builder.Append(_options.ReachableFrom.Pattern.Glob);
            builder.Append('\'');
        }

        if (_options.DependentsOf is not null)
        {
            builder.Append(" dependents of '");
            builder.Append(_options.DependentsOf.Pattern.Glob);
            builder.Append('\'');
        }

        return builder.ToString();
    }

    private GraphReport With(Func<GraphQueryOptions, GraphQueryOptions> transform) =>
        new(_graph, transform(_options));

    private static CollapseRule[] Append(CollapseRule[] rules, CollapseRule rule)
    {
        var copy = new CollapseRule[rules.Length + 1];
        Array.Copy(rules, copy, rules.Length);
        copy[rules.Length] = rule;
        return copy;
    }
}
