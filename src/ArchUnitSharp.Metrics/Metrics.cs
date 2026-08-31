namespace ArchUnitSharp.Metrics;

using System.Text;
using ArchUnitSharp.Common.Extraction;
using ArchUnitSharp.Metrics.Projection;

/// <summary>
/// The metrics domain module's fluent surface: a scoped set of files and classes over one project's
/// <see cref="Graph"/>. It is the ENTRY and SCOPE of a metric rule chain — built from the entry
/// points <c>Project.ProjectMetrics()</c> / <c>Project.Metrics()</c>, narrowed by the file selectors
/// <see cref="WithName"/>, <see cref="InFolder"/>, <see cref="InPath"/> and the class selector
/// <see cref="ForClassesMatching"/>, and handed to the count section <see cref="Count"/>, the
/// cohesion section <see cref="Lcom"/>, the distance section <see cref="Distance"/> or the custom
/// section <see cref="CustomMetric"/>.
/// </summary>
/// <remarks>
/// <para>
/// A <see cref="Metrics"/> value names a set of files: every file in the graph when no file selector
/// has been applied, otherwise exactly the files that match every file selector applied so far —
/// selectors combine with AND. Each selector's <c>except</c> companion
/// (<see cref="Except(string)"/> / <see cref="Except(Filter)"/>) narrows that one selector: a
/// subject an exclusion matches is not selected by it, so "everything under <c>app/</c>, but not the
/// generated folder" is one chain rather than an inverted rule. <see cref="ForClassesMatching"/>
/// narrows by class rather than file: it
/// keeps the files that declare at least one class whose fully qualified name matches, and is what a
/// class-level metric's subjects and a file-level metric's in-scope files are drawn from. The
/// PREDICATE and TERMINAL of a rule chain are the <see cref="CountMetrics"/> / <see cref="LcomMetrics"/>
/// / <see cref="DistanceMetrics"/> builder's and the metric selection's concern — a custom metric's
/// is <see cref="CustomMetricSelection"/>'s; <see cref="SelectFiles"/> evaluates the scope's file
/// selection so a rule can consume it.
/// </para>
/// <para>
/// Every selector returns a new <see cref="Metrics"/> instance and never mutates the one it was called
/// on, so a half-built scope can be stored in a variable and branched from without one branch seeing
/// another's selectors. This type is immutable and safe for concurrent use.
/// </para>
/// <para>
/// A scope also carries a source-text provider, the boundary through which a metric rule reads each
/// selected file's content to extract its count, method-field access and type facts. The provider
/// is wired by the composition root — the entry points build it from the located project — and a
/// scope built from a bare <see cref="Graph"/> has no source to read, so a metric rule over it
/// raises a <see cref="UserError"/> rather than fabricating empty text.
/// </para>
/// </remarks>
public sealed class Metrics
{
    private readonly Graph _graph;
    private readonly Filter[] _fileFilters;
    private readonly Filter[] _classFilters;
    private readonly Func<string, string> _sourceText;
    private readonly bool _lastSelectorWasClass;

    /// <summary>
    /// Creates a scope over every file of <paramref name="graph"/>. The scope has no access to the
    /// files' source text, so a metric rule over it raises a <see cref="UserError"/>; build the scope
    /// from <c>Project.ProjectMetrics</c> to run such a rule.
    /// </summary>
    /// <param name="graph">The project's dependency graph. Must not be <see langword="null"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="graph"/> is <see langword="null"/>.</exception>
    public Metrics(Graph graph)
    {
        ArgumentNullException.ThrowIfNull(graph);
        _graph = graph;
        _fileFilters = Array.Empty<Filter>();
        _classFilters = Array.Empty<Filter>();
        _sourceText = NoSource;
        _lastSelectorWasClass = false;
    }

    /// <summary>
    /// Creates a scope over every file of <paramref name="graph"/> whose source text is read through
    /// <paramref name="sourceText"/>: an identifier in, the file's full text out. Internal: the
    /// composition root wires a provider that reads the located project's files from disk, and a test
    /// wires a fixture map.
    /// </summary>
    /// <param name="graph">The project's dependency graph. Must not be <see langword="null"/>.</param>
    /// <param name="sourceText">The source-text provider. Must not be <see langword="null"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="graph"/> or <paramref name="sourceText"/> is <see langword="null"/>.</exception>
    internal Metrics(Graph graph, Func<string, string> sourceText)
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(sourceText);
        _graph = graph;
        _fileFilters = Array.Empty<Filter>();
        _classFilters = Array.Empty<Filter>();
        _sourceText = sourceText;
        _lastSelectorWasClass = false;
    }

    private Metrics(
        Graph graph,
        Filter[] fileFilters,
        Filter[] classFilters,
        Func<string, string> sourceText,
        bool lastSelectorWasClass)
    {
        _graph = graph;
        _fileFilters = fileFilters;
        _classFilters = classFilters;
        _sourceText = sourceText;
        _lastSelectorWasClass = lastSelectorWasClass;
    }

    /// <summary>
    /// Narrows the scope to the files whose name matches <paramref name="glob"/>. The name is the
    /// file's name with no directory part, so a file identified by <c>src/Models/Car.cs</c> has the
    /// name <c>Car.cs</c>. Returns a new <see cref="Metrics"/>; the current scope is unchanged.
    /// </summary>
    /// <param name="glob">The glob to match the file name against. Must not be <see langword="null"/> or empty.</param>
    /// <returns>A new scope narrowed to the files whose name matches.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="glob"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="glob"/> is empty.</exception>
    public Metrics WithName(string glob) =>
        AddFile(new Filter(new Pattern(glob), MatchTarget.Filename));

    /// <summary>
    /// Narrows the scope to the files that sit in the folder that matches <paramref name="glob"/>.
    /// The folder is the file's identifier with its name removed, so a file identified by
    /// <c>src/Models/Car.cs</c> sits in the folder <c>src/Models</c>. Returns a new
    /// <see cref="Metrics"/>; the current scope is unchanged.
    /// </summary>
    /// <param name="glob">The glob to match the folder against. Must not be <see langword="null"/> or empty.</param>
    /// <returns>A new scope narrowed to the files whose folder matches.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="glob"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="glob"/> is empty.</exception>
    public Metrics InFolder(string glob) =>
        AddFile(new Filter(new Pattern(glob), MatchTarget.PathWithoutFilename));

    /// <summary>
    /// Narrows the scope to the files whose whole path matches <paramref name="glob"/>. The path is
    /// the file's project-relative identifier, folders and name together, so a file identified by
    /// <c>src/Models/Car.cs</c> has the path <c>src/Models/Car.cs</c>. Returns a new
    /// <see cref="Metrics"/>; the current scope is unchanged.
    /// </summary>
    /// <param name="glob">The glob to match the whole path against. Must not be <see langword="null"/> or empty.</param>
    /// <returns>A new scope narrowed to the files whose path matches.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="glob"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="glob"/> is empty.</exception>
    public Metrics InPath(string glob) =>
        AddFile(new Filter(new Pattern(glob), MatchTarget.Path));

    /// <summary>
    /// Narrows the scope by class: keeps the files that declare at least one class whose fully
    /// qualified name — namespace and enclosing types joined to the class's own name with dots, so
    /// <c>namespace App.Models { public class Car { } }</c> yields <c>App.Models.Car</c> — matches
    /// <paramref name="glob"/>. A class-level metric's subjects are the matching classes; a file-level
    /// metric's subjects are the files that contain one, measured whole. Returns a new
    /// <see cref="Metrics"/>; the current scope is unchanged.
    /// </summary>
    /// <param name="glob">The glob to match each class's fully qualified name against. Must not be <see langword="null"/> or empty.</param>
    /// <returns>A new scope narrowed to the files that declare a matching class.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="glob"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="glob"/> is empty.</exception>
    public Metrics ForClassesMatching(string glob) =>
        AddClass(new Filter(new Pattern(glob), MatchTarget.Path));

    /// <summary>
    /// <c>except</c>: narrows the most recently applied selector by excluding the subjects whose own
    /// target part matches <paramref name="glob"/>. The glob is matched against the same part of a
    /// subject the selector just applied matches — a scope narrowed by <c>InPath("app/**")</c> and
    /// then <c>Except("app/generated/**")</c> keeps every file under <c>app/</c> but not the
    /// generated folder. To exclude by a different part, pass a fully targeted filter to
    /// <see cref="Except(Filter)"/> instead. Returns a new <see cref="Metrics"/>; the current scope
    /// is unchanged. Must follow a selector: there is nothing to exclude from otherwise.
    /// </summary>
    /// <param name="glob">The glob to match the excluded subjects' target part against. Must not be <see langword="null"/> or empty.</param>
    /// <returns>A new scope with the most recently applied selector's exclusion added.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="glob"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="glob"/> is empty.</exception>
    /// <exception cref="UserError">No selector has been applied, so there is nothing to exclude from.</exception>
    public Metrics Except(string glob)
    {
        var pattern = new Pattern(glob);
        Filter last = LastSelector();
        return ReplaceLast(last.WithExclusion(new Filter(pattern, last.Target)));
    }

    /// <summary>
    /// <c>except</c>: narrows the most recently applied selector by excluding the subjects
    /// <paramref name="exclusion"/> matches. The exclusion is a fully targeted filter, evaluated
    /// against the same subject the selector just applied matches — a scope narrowed by
    /// <c>ForClassesMatching("*Service")</c> and then
    /// <c>Except(MatcherFactory.Path("*Legacy*"))</c> keeps the classes named <c>*Service</c> but
    /// not <c>*Legacy*</c>. Returns a new <see cref="Metrics"/>; the current scope is unchanged. Must
    /// follow a selector: there is nothing to exclude from otherwise.
    /// </summary>
    /// <param name="exclusion">The exclusion to add. Must not be <see langword="null"/>.</param>
    /// <returns>A new scope with the most recently applied selector's exclusion added.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="exclusion"/> is <see langword="null"/>.</exception>
    /// <exception cref="UserError">No selector has been applied, so there is nothing to exclude from.</exception>
    public Metrics Except(Filter exclusion)
    {
        ArgumentNullException.ThrowIfNull(exclusion);
        return ReplaceLast(LastSelector().WithExclusion(exclusion));
    }

    /// <summary>
    /// <c>count</c>: the count-metric section of a rule chain. Returns a new <see cref="CountMetrics"/>;
    /// the current scope is unchanged.
    /// </summary>
    /// <returns>A new <see cref="CountMetrics"/> over this scope.</returns>
    public CountMetrics Count() => new(this);

    /// <summary>
    /// <c>lcom</c>: the cohesion-metric section of a rule chain. Returns a new <see cref="LcomMetrics"/>;
    /// the current scope is unchanged.
    /// </summary>
    /// <returns>A new <see cref="LcomMetrics"/> over this scope.</returns>
    public LcomMetrics Lcom() => new(this);

    /// <summary>
    /// <c>distance</c>: the distance-metric section of a rule chain. Returns a new
    /// <see cref="DistanceMetrics"/>; the current scope is unchanged.
    /// </summary>
    /// <returns>A new <see cref="DistanceMetrics"/> over this scope.</returns>
    public DistanceMetrics Distance() => new(this);

    /// <summary>
    /// <c>custom metric</c>: the metrics module's escape hatch — a rule over a caller-named metric
    /// whose value the caller's own calculation computes from one class's full information. Returns a
    /// new <see cref="CustomMetricSelection"/>; the current scope is unchanged.
    /// </summary>
    /// <param name="name">The metric's name, as a report shows it; must not be <see langword="null"/> or empty.</param>
    /// <param name="description">The metric's description, the rule's intent in the caller's own words; must not be <see langword="null"/> or empty.</param>
    /// <param name="calculation">The calculation that turns one extracted <see cref="ClassInfo"/> into the metric's value; must not be <see langword="null"/>.</param>
    /// <returns>A new <see cref="CustomMetricSelection"/> over this scope.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="name"/>, <paramref name="description"/> or <paramref name="calculation"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> or <paramref name="description"/> is empty.</exception>
    public CustomMetricSelection CustomMetric(string name, string description, Func<ClassInfo, int> calculation) =>
        new(this, new CustomMetric(name, description, calculation));

    /// <summary>
    /// Evaluates the scope's file selection: the identifiers of the files this scope names, sorted
    /// ordinally. With no file selectors every file of the graph is selected; with one or more,
    /// exactly the files that match all of them. The returned list is a fresh copy on every call.
    /// </summary>
    /// <returns>The selected files' identifiers, sorted.</returns>
    public IReadOnlyList<string> SelectFiles() => MetricsProjection.SelectFiles(_graph, _fileFilters);

    private Metrics AddFile(Filter filter)
    {
        var filters = new Filter[_fileFilters.Length + 1];
        Array.Copy(_fileFilters, filters, _fileFilters.Length);
        filters[_fileFilters.Length] = filter;
        return new Metrics(_graph, filters, _classFilters, _sourceText, lastSelectorWasClass: false);
    }

    private Metrics AddClass(Filter filter)
    {
        var filters = new Filter[_classFilters.Length + 1];
        Array.Copy(_classFilters, filters, _classFilters.Length);
        filters[_classFilters.Length] = filter;
        return new Metrics(_graph, _fileFilters, filters, _sourceText, lastSelectorWasClass: true);
    }

    /// <summary>
    /// The selector <c>except</c> narrows: the most recently applied one, whether a file selector or
    /// the class selector. There is none before any selector has been applied, which is a misuse of
    /// the fluent grammar and raises a <see cref="UserError"/> naming the mistake.
    /// </summary>
    private Filter LastSelector()
    {
        IReadOnlyList<Filter> filters = _lastSelectorWasClass ? _classFilters : _fileFilters;
        return filters.Count == 0
            ? throw new UserError(
                "except must follow a selector (with name, in folder, in path or for classes "
                + "matching): there is no selector to exclude from.")
            : filters[^1];
    }

    /// <summary>
    /// Returns a new scope whose most recently applied selector is replaced by
    /// <paramref name="replacement"/>, everything else unchanged. This is how <c>except</c> narrows
    /// one selector without mutating the scope it was called on.
    /// </summary>
    private Metrics ReplaceLast(Filter replacement)
    {
        if (_lastSelectorWasClass)
        {
            var classFilters = (Filter[])_classFilters.Clone();
            classFilters[^1] = replacement;
            return new Metrics(_graph, _fileFilters, classFilters, _sourceText, lastSelectorWasClass: true);
        }

        var fileFilters = (Filter[])_fileFilters.Clone();
        fileFilters[^1] = replacement;
        return new Metrics(_graph, fileFilters, _classFilters, _sourceText, lastSelectorWasClass: false);
    }

    /// <summary>
    /// The source provider of a scope built from a bare graph: there is no source text to read, so any
    /// attempt to materialise a file's counts raises a <see cref="UserError"/>.
    /// </summary>
    private static string NoSource(string identifier) =>
        throw new UserError(
            $"Source text is not available for file '{identifier}': this metrics scope was built from "
            + "a graph without its source files. Build the scope from Project.ProjectMetrics(...) to "
            + "run metric rules.");

    /// <summary>
    /// The project's dependency graph this scope draws its files from. Internal: the metric rules read
    /// it to compute the scope's file selection.
    /// </summary>
    internal Graph Graph => _graph;

    /// <summary>
    /// The scope's file selectors, in the order they were applied. Internal: the metric rules read
    /// them to compute the scope's file selection. Each access returns a fresh copy, so the returned
    /// list is always safe to hold or mutate.
    /// </summary>
    internal IReadOnlyList<Filter> FileFilters => _fileFilters.ToArray();

    /// <summary>
    /// The scope's class selectors, in the order they were applied. Internal: the metric rules read
    /// them to compute the class subjects of a class-level metric and the in-scope files of a
    /// file-level metric. Each access returns a fresh copy, so the returned list is always safe to
    /// hold or mutate.
    /// </summary>
    internal IReadOnlyList<Filter> ClassFilters => _classFilters.ToArray();

    /// <summary>
    /// The scope's source-text provider. Internal: the metric rules read each selected file's text
    /// through it. A scope built from a bare graph has the provider that raises a <see cref="UserError"/>.
    /// </summary>
    internal Func<string, string> SourceText => _sourceText;

    /// <summary>
    /// Describes this scope as the subject of a rule, for a report: the entry phrase
    /// <c>project metrics</c> followed by one clause per selector, in the selector's own words, and
    /// one <c>except</c> clause per exclusion. A scope
    /// narrowed by <c>WithName("Car.cs")</c> and <c>ForClassesMatching("*.Controller")</c> is described
    /// as <c>project metrics with name 'Car.cs' for classes matching '*.Controller'</c>.
    /// </summary>
    internal string DescribeScope()
    {
        var builder = new StringBuilder("project metrics");
        foreach (Filter filter in _fileFilters)
        {
            AppendFileSelectorClause(builder, filter);
        }

        foreach (Filter filter in _classFilters)
        {
            builder.Append(" for classes matching '");
            builder.Append(filter.Pattern.Glob);
            builder.Append('\'');
            foreach (Filter exclusion in filter.Exclusions)
            {
                builder.Append(" except for classes matching '");
                builder.Append(exclusion.Pattern.Glob);
                builder.Append('\'');
            }
        }

        return builder.ToString();
    }

    /// <summary>
    /// Appends one file selector's clause to a scope description: the selector's own words, the
    /// pattern's glob, and one <c>except</c> clause per exclusion in the exclusion's own words.
    /// </summary>
    private static void AppendFileSelectorClause(StringBuilder builder, Filter filter)
    {
        builder.Append(' ');
        builder.Append(SelectorWord(filter.Target));
        builder.Append(" '");
        builder.Append(filter.Pattern.Glob);
        builder.Append('\'');
        foreach (Filter exclusion in filter.Exclusions)
        {
            builder.Append(" except ");
            builder.Append(SelectorWord(exclusion.Target));
            builder.Append(" '");
            builder.Append(exclusion.Pattern.Glob);
            builder.Append('\'');
        }
    }

    /// <summary>
    /// The file selector's own words for a match target, shared with the scope's description.
    /// </summary>
    private static string SelectorWord(MatchTarget target) => target switch
    {
        MatchTarget.Filename => "with name",
        MatchTarget.PathWithoutFilename => "in folder",
        MatchTarget.Path => "in path",
        MatchTarget.Classname => "in file",
        _ => throw new ArgumentOutOfRangeException(
            nameof(target),
            target,
            "Target is not a defined MatchTarget value."),
    };
}
