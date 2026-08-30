namespace ArchUnitSharp.Graph;

using System.Text;
using ArchUnitSharp.Common.Extraction;
using ArchUnitSharp.Graph.Projection;
using ArchUnitSharp.Graph.Rendering;

/// <summary>
/// The graph domain module's fluent surface: an immutable query over one project's
/// <see cref="ArchUnitSharp.Common.Extraction.Graph"/> that filters, collapses, aggregates and counts
/// it into a <see cref="GraphSnapshot"/>, rendered in any of the six output formats. It is the ENTRY
/// of a report chain — built from the entry points <c>Project.ProjectGraph()</c> /
/// <c>Project.Graph()</c> — and the accumulator of the query options that shape the snapshot.
/// </summary>
/// <remarks>
/// <para>
/// Rendering is two steps: build a snapshot, then render it. <see cref="Build"/> produces the
/// snapshot every renderer consumes — the query's scope as nodes, its dependencies aggregated to one
/// edge per label pair, and the summary counts — and the string forms, <see cref="ToDot"/>,
/// <see cref="ToMermaid"/>, <see cref="ToD2"/>, <see cref="ToCsv"/>, <see cref="ToJson"/> and
/// <see cref="ToHtml"/>, render that snapshot; the file forms, <see cref="ExportAsDot"/>,
/// <see cref="ExportAsMermaid"/>, <see cref="ExportAsD2"/>, <see cref="ExportAsCsv"/>,
/// <see cref="ExportAsJson"/> and <see cref="ExportAsHtml"/>, write a format's text to a file and
/// return the path they wrote. The file form is the module's only disk boundary; the rendering
/// itself is pure. The snapshot a query builds is identical on every call, because the query is
/// immutable.
/// </para>
/// <para>
/// <see cref="Check(CheckOptions?)"/> is the query's rule terminal: it reports one
/// <see cref="EmptyTestViolation"/> when the scope matched no files, unless the query's check options
/// allow empty tests, and nothing otherwise. <see cref="Build"/> and the render forms are data
/// terminals — an empty snapshot is visible data, not a violation.
/// </para>
/// <para>
/// The options are the modifiers of the chain: <see cref="IncludingExternalDependencies"/>,
/// <see cref="IncludingSelfDependencies"/>, <see cref="FocusingOn"/>, <see cref="ReachableFrom"/>,
/// <see cref="DependentsOf"/>, <see cref="CollapsedToFolderDepth"/>, <see cref="CollapsedByPattern"/>,
/// <see cref="Titled"/> and <see cref="WithCheckOptions"/>. They combine freely and are
/// order-independent, except that collapse rules apply in the order they were added and the first
/// rule that relabels a file wins.
/// </para>
/// <para>
/// Every method returns a new <see cref="GraphReport"/> instance and never mutates the one it was
/// called on, so a half-built query can be stored in a variable and branched from without one branch
/// seeing another's options. This type is immutable and safe for concurrent use.
/// </para>
/// </remarks>
public sealed class GraphReport : ICheckable
{
    private readonly ArchUnitSharp.Common.Extraction.Graph _graph;
    private readonly GraphQueryOptions _options;

    /// <summary>
    /// Creates a report query over every file of <paramref name="graph"/> with the default options:
    /// no restrictions, external and self dependencies excluded, no collapse and no title.
    /// </summary>
    /// <param name="graph">The project's dependency graph. Must not be <see langword="null"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="graph"/> is <see langword="null"/>.</exception>
    public GraphReport(ArchUnitSharp.Common.Extraction.Graph graph)
    {
        ArgumentNullException.ThrowIfNull(graph);
        _graph = graph;
        _options = GraphQueryOptions.Default;
    }

    private GraphReport(ArchUnitSharp.Common.Extraction.Graph graph, GraphQueryOptions options)
    {
        _graph = graph;
        _options = options;
    }

    /// <summary>
    /// <c>including external dependencies</c>: the snapshot includes edges whose target lies outside
    /// the project, so a dependency that leaves the project is part of the report. Returns a new
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
    /// snapshot is the immutable data contract every renderer consumes. Building is deterministic —
    /// the same query always builds the identical snapshot.
    /// </summary>
    /// <returns>The snapshot the query describes.</returns>
    public GraphSnapshot Build() => GraphProjection.Build(_graph, _options);

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
        IReadOnlyList<string> scope = GraphProjection.Select(_graph, _options);
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
    /// <c>to dot()</c>: renders the report as a DOT digraph for Graphviz.
    /// </summary>
    /// <returns>The DOT source.</returns>
    public string ToDot() => DotRenderer.Render(Build());

    /// <summary>
    /// <c>to mermaid()</c>: renders the report as a Mermaid flowchart.
    /// </summary>
    /// <returns>The Mermaid source.</returns>
    public string ToMermaid() => MermaidRenderer.Render(Build());

    /// <summary>
    /// <c>to d2()</c>: renders the report as a D2 diagram.
    /// </summary>
    /// <returns>The D2 source.</returns>
    public string ToD2() => D2Renderer.Render(Build());

    /// <summary>
    /// <c>to csv()</c>: renders the report as a CSV table, one row per dependency.
    /// </summary>
    /// <returns>The CSV text.</returns>
    public string ToCsv() => CsvRenderer.Render(Build());

    /// <summary>
    /// <c>to json()</c>: renders the report as a JSON document with the nodes and edges arrays.
    /// </summary>
    /// <returns>The JSON document.</returns>
    public string ToJson() => JsonRenderer.Render(Build());

    /// <summary>
    /// <c>to html()</c>: renders the report as a self-contained HTML page with the graph drawn as
    /// inline SVG.
    /// </summary>
    /// <returns>The HTML document.</returns>
    public string ToHtml() => HtmlRenderer.Render(Build());

    /// <summary>
    /// <c>export as dot(path)</c>: renders the report as DOT and writes it to <paramref name="path"/>.
    /// </summary>
    /// <param name="path">The file to write. Must not be <see langword="null"/> or empty; its directory must exist.</param>
    /// <returns><paramref name="path"/>, which now holds the DOT source.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="path"/> is empty.</exception>
    /// <exception cref="TechnicalError">The file cannot be written.</exception>
    public string ExportAsDot(string path) => Export(path, ToDot());

    /// <summary>
    /// <c>export as mermaid(path)</c>: renders the report as Mermaid and writes it to
    /// <paramref name="path"/>.
    /// </summary>
    /// <param name="path">The file to write. Must not be <see langword="null"/> or empty; its directory must exist.</param>
    /// <returns><paramref name="path"/>, which now holds the Mermaid source.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="path"/> is empty.</exception>
    /// <exception cref="TechnicalError">The file cannot be written.</exception>
    public string ExportAsMermaid(string path) => Export(path, ToMermaid());

    /// <summary>
    /// <c>export as d2(path)</c>: renders the report as D2 and writes it to <paramref name="path"/>.
    /// </summary>
    /// <param name="path">The file to write. Must not be <see langword="null"/> or empty; its directory must exist.</param>
    /// <returns><paramref name="path"/>, which now holds the D2 source.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="path"/> is empty.</exception>
    /// <exception cref="TechnicalError">The file cannot be written.</exception>
    public string ExportAsD2(string path) => Export(path, ToD2());

    /// <summary>
    /// <c>export as csv(path)</c>: renders the report as CSV and writes it to <paramref name="path"/>.
    /// </summary>
    /// <param name="path">The file to write. Must not be <see langword="null"/> or empty; its directory must exist.</param>
    /// <returns><paramref name="path"/>, which now holds the CSV text.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="path"/> is empty.</exception>
    /// <exception cref="TechnicalError">The file cannot be written.</exception>
    public string ExportAsCsv(string path) => Export(path, ToCsv());

    /// <summary>
    /// <c>export as json(path)</c>: renders the report as JSON and writes it to <paramref name="path"/>.
    /// </summary>
    /// <param name="path">The file to write. Must not be <see langword="null"/> or empty; its directory must exist.</param>
    /// <returns><paramref name="path"/>, which now holds the JSON document.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="path"/> is empty.</exception>
    /// <exception cref="TechnicalError">The file cannot be written.</exception>
    public string ExportAsJson(string path) => Export(path, ToJson());

    /// <summary>
    /// <c>export as html(path)</c>: renders the report as self-contained HTML and writes it to
    /// <paramref name="path"/>.
    /// </summary>
    /// <param name="path">The file to write. Must not be <see langword="null"/> or empty; its directory must exist.</param>
    /// <returns><paramref name="path"/>, which now holds the HTML document.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="path"/> is empty.</exception>
    /// <exception cref="TechnicalError">The file cannot be written.</exception>
    public string ExportAsHtml(string path) => Export(path, ToHtml());

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

    private static string Export(string path, string content)
    {
        ArgumentNullException.ThrowIfNull(path);
        if (path.Length == 0)
        {
            throw new ArgumentException("Path must not be empty.", nameof(path));
        }

        try
        {
            File.WriteAllText(path, content);
            return path;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            throw new TechnicalError($"Failed to export the graph report to '{path}'.", exception);
        }
    }
}
