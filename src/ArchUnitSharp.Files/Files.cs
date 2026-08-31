namespace ArchUnitSharp.Files;

using System.Text;
using ArchUnitSharp.Common.Extraction;

/// <summary>
/// The files domain module's fluent surface: a scoped selection of the files of one project's
/// <see cref="Graph"/>. It is the ENTRY and SCOPE of a rule chain — built from the entry points
/// <c>Project.ProjectFiles()</c> / <c>Project.Files()</c>, narrowed by the selectors
/// <see cref="WithName"/>, <see cref="InFolder"/>, <see cref="InPath"/> and <see cref="InFile"/>,
/// and handed to the mood <see cref="Should"/> or <see cref="ShouldNot"/>.
/// </summary>
/// <remarks>
/// <para>
/// A <see cref="Files"/> value names a set of files: every file in the graph when no selector has
/// been applied, otherwise exactly the files that match every selector applied so far — selectors
/// combine with AND. Each selector's <c>except</c> companion (<see cref="Except(string)"/> /
/// <see cref="Except(Filter)"/>) narrows that one selector: a file an exclusion matches is not
/// selected by it, so "everything under <c>app/</c>, but not the generated folder" is one chain
/// rather than an inverted rule. The MOOD of a rule is chosen with <see cref="Should"/> or
/// <see cref="ShouldNot"/>; the PREDICATE and TERMINAL are the assertion layer's concern, and
/// <see cref="Select"/> evaluates the scope's selection so a terminal can consume it.
/// </para>
/// <para>
/// Every selector returns a new <see cref="Files"/> instance and never mutates the one it was called
/// on, so a half-built selection can be stored in a variable and branched from without one branch
/// seeing another's selectors. This type is immutable and safe for concurrent use.
/// </para>
/// <para>
/// A selection also carries a source-text provider, the boundary through which the <c>adhere to</c>
/// predicate reads each file's content. The provider is wired by the composition root — the entry
/// points build it from the located project — and a selection built from a bare <see cref="Graph"/>
/// has no source to read, so <c>adhere to</c> over it raises a <see cref="UserError"/> rather than
/// fabricating empty text.
/// </para>
/// </remarks>
public sealed class Files
{
    private readonly Graph _graph;
    private readonly Filter[] _filters;
    private readonly Func<string, string> _sourceText;

    /// <summary>
    /// Creates a selection over every file of <paramref name="graph"/>. The selection has no access
    /// to the files' source text, so an <c>adhere to</c> rule over it raises a
    /// <see cref="UserError"/>; build the selection from
    /// <c>Project.ProjectFiles</c> to run such a rule.
    /// </summary>
    /// <param name="graph">The project's dependency graph. Must not be <see langword="null"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="graph"/> is <see langword="null"/>.</exception>
    public Files(Graph graph)
    {
        ArgumentNullException.ThrowIfNull(graph);
        _graph = graph;
        _filters = Array.Empty<Filter>();
        _sourceText = NoSource;
    }

    /// <summary>
    /// Creates a selection over every file of <paramref name="graph"/> whose source text is read
    /// through <paramref name="sourceText"/>: an identifier in, the file's full text out. Internal:
    /// the composition root wires a provider that reads the located project's files from disk, and a
    /// test wires a fixture map.
    /// </summary>
    /// <param name="graph">The project's dependency graph. Must not be <see langword="null"/>.</param>
    /// <param name="sourceText">The source-text provider. Must not be <see langword="null"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="graph"/> or <paramref name="sourceText"/> is <see langword="null"/>.</exception>
    internal Files(Graph graph, Func<string, string> sourceText)
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(sourceText);
        _graph = graph;
        _filters = Array.Empty<Filter>();
        _sourceText = sourceText;
    }

    private Files(Graph graph, Filter[] filters, Func<string, string> sourceText)
    {
        _graph = graph;
        _filters = filters;
        _sourceText = sourceText;
    }

    /// <summary>
    /// Narrows the selection to the files whose name matches <paramref name="glob"/>. The name is the
    /// file's name with no directory part, so a file identified by <c>src/Models/Car.cs</c> has the
    /// name <c>Car.cs</c>. Returns a new <see cref="Files"/>; the current selection is unchanged.
    /// </summary>
    /// <param name="glob">The glob to match the file name against. Must not be <see langword="null"/> or empty.</param>
    /// <returns>A new selection narrowed to the files whose name matches.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="glob"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="glob"/> is empty.</exception>
    public Files WithName(string glob) => Add(new Filter(new Pattern(glob), MatchTarget.Filename));

    /// <summary>
    /// Narrows the selection to the files that sit in the folder that matches <paramref name="glob"/>.
    /// The folder is the file's identifier with its name removed, so a file identified by
    /// <c>src/Models/Car.cs</c> sits in the folder <c>src/Models</c>. Returns a new
    /// <see cref="Files"/>; the current selection is unchanged.
    /// </summary>
    /// <param name="glob">The glob to match the folder against. Must not be <see langword="null"/> or empty.</param>
    /// <returns>A new selection narrowed to the files whose folder matches.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="glob"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="glob"/> is empty.</exception>
    public Files InFolder(string glob) => Add(new Filter(new Pattern(glob), MatchTarget.PathWithoutFilename));

    /// <summary>
    /// Narrows the selection to the files whose whole path matches <paramref name="glob"/>. The path
    /// is the file's project-relative identifier, folders and name together, so a file identified by
    /// <c>src/Models/Car.cs</c> has the path <c>src/Models/Car.cs</c>. Returns a new
    /// <see cref="Files"/>; the current selection is unchanged.
    /// </summary>
    /// <param name="glob">The glob to match the whole path against. Must not be <see langword="null"/> or empty.</param>
    /// <returns>A new selection narrowed to the files whose path matches.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="glob"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="glob"/> is empty.</exception>
    public Files InPath(string glob) => Add(new Filter(new Pattern(glob), MatchTarget.Path));

    /// <summary>
    /// Narrows the selection to the files whose file name matches <paramref name="glob"/>, where the
    /// file name is the identifier's extension stripped and every separator turned into a dot — the
    /// name the file would carry as a class. A file identified by <c>src/Models/Car.cs</c> has the
    /// file name <c>src.Models.Car</c>. Returns a new <see cref="Files"/>; the current selection is
    /// unchanged.
    /// </summary>
    /// <param name="glob">The glob to match the file name against. Must not be <see langword="null"/> or empty.</param>
    /// <returns>A new selection narrowed to the files whose file name matches.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="glob"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="glob"/> is empty.</exception>
    public Files InFile(string glob) => Add(new Filter(new Pattern(glob), MatchTarget.Classname));

    /// <summary>
    /// <c>except</c>: narrows the most recently applied selector by excluding the identifiers whose
    /// own target part matches <paramref name="glob"/>. The glob is matched against the same part of
    /// an identifier the selector just applied matches — a selection narrowed by
    /// <c>InPath("app/**")</c> and then <c>Except("app/generated/**")</c> keeps every file under
    /// <c>app/</c> but not the generated folder. To exclude by a different part, pass a fully
    /// targeted filter to <see cref="Except(Filter)"/> instead. Returns a new <see cref="Files"/>;
    /// the current selection is unchanged. Must follow a selector: there is nothing to exclude from
    /// otherwise.
    /// </summary>
    /// <param name="glob">The glob to match the excluded identifiers' target part against. Must not be <see langword="null"/> or empty.</param>
    /// <returns>A new selection with the most recently applied selector's exclusion added.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="glob"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="glob"/> is empty.</exception>
    /// <exception cref="UserError">No selector has been applied, so there is nothing to exclude from.</exception>
    public Files Except(string glob)
    {
        var pattern = new Pattern(glob);
        Filter last = LastSelector();
        return ReplaceLast(last.WithExclusion(new Filter(pattern, last.Target)));
    }

    /// <summary>
    /// <c>except</c>: narrows the most recently applied selector by excluding the identifiers
    /// <paramref name="exclusion"/> matches. The exclusion is a fully targeted filter, evaluated
    /// against the same identifier the selector just applied matches — a selection narrowed by
    /// <c>InFolder("app")</c> and then <c>Except(MatcherFactory.Filename("index.ts"))</c> keeps the
    /// folder's files but not one named <c>index.ts</c>. Returns a new <see cref="Files"/>; the
    /// current selection is unchanged. Must follow a selector: there is nothing to exclude from
    /// otherwise.
    /// </summary>
    /// <param name="exclusion">The exclusion to add. Must not be <see langword="null"/>.</param>
    /// <returns>A new selection with the most recently applied selector's exclusion added.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="exclusion"/> is <see langword="null"/>.</exception>
    /// <exception cref="UserError">No selector has been applied, so there is nothing to exclude from.</exception>
    public Files Except(Filter exclusion)
    {
        ArgumentNullException.ThrowIfNull(exclusion);
        return ReplaceLast(LastSelector().WithExclusion(exclusion));
    }

    /// <summary>
    /// <c>should</c>: begins a rule over this selection with the positive mood. Returns a new
    /// <see cref="Should"/>; the current selection is unchanged.
    /// </summary>
    /// <returns>A new <see cref="Should"/> over this selection.</returns>
    public Should Should() => new(this);

    /// <summary>
    /// <c>should not</c>: begins a rule over this selection with the negated mood. Returns a new
    /// <see cref="ShouldNot"/>; the current selection is unchanged.
    /// </summary>
    /// <returns>A new <see cref="ShouldNot"/> over this selection.</returns>
    public ShouldNot ShouldNot() => new(this);

    /// <summary>
    /// Evaluates the scope: the identifiers of the files this selection names, sorted ordinally. With
    /// no selectors every file of the graph is selected; with one or more, exactly the files that
    /// match all of them. The returned list is a fresh copy on every call.
    /// </summary>
    /// <returns>The selected files' identifiers, sorted.</returns>
    public IReadOnlyList<string> Select() => Projection.FilesProjection.Select(_graph, _filters);

    /// <summary>
    /// The cycles of this selection's projected dependency graph: the closed file path of every
    /// elementary cycle in the subgraph the selected files induce, first and last entry the same file.
    /// A cycle is reported only when every file it passes through is selected. Internal: the cycles
    /// rule's terminal consumes it through the assertion.
    /// </summary>
    internal IReadOnlyList<IReadOnlyList<string>> Cycles() =>
        Projection.FilesProjection.Cycles(_graph, _filters);

    private Files Add(Filter filter)
    {
        var filters = new Filter[_filters.Length + 1];
        Array.Copy(_filters, filters, _filters.Length);
        filters[_filters.Length] = filter;
        return new Files(_graph, filters, _sourceText);
    }

    /// <summary>
    /// The selector <c>except</c> narrows: the most recently applied one. There is none before any
    /// selector has been applied, which is a misuse of the fluent grammar and raises a
    /// <see cref="UserError"/> naming the mistake.
    /// </summary>
    private Filter LastSelector() =>
        _filters.Length == 0
            ? throw new UserError(
                "except must follow a selector (with name, in folder, in path or in file): there is "
                + "no selector to exclude from.")
            : _filters[^1];

    /// <summary>
    /// Returns a new selection whose most recently applied selector is replaced by
    /// <paramref name="replacement"/>, everything else unchanged. This is how <c>except</c> narrows
    /// one selector without mutating the selection it was called on.
    /// </summary>
    private Files ReplaceLast(Filter replacement)
    {
        var filters = (Filter[])_filters.Clone();
        filters[^1] = replacement;
        return new Files(_graph, filters, _sourceText);
    }

    /// <summary>
    /// The per-file detail an <c>adhere to</c> rule's assertion materialises for one selected file:
    /// the file's identity and source text, read through this selection's source provider. Internal:
    /// the adhere-to assertion consumes it. A selection without a source provider — one built from a
    /// bare graph — raises a <see cref="UserError"/> when this is called.
    /// </summary>
    internal FileDetail FileDetailOf(string identifier) =>
        Projection.FilesProjection.Detail(identifier, _sourceText(identifier));

    /// <summary>
    /// The source provider of a selection built from a bare graph: there is no source text to read,
    /// so any attempt to materialise a file's detail raises a <see cref="UserError"/>.
    /// </summary>
    private static string NoSource(string identifier) =>
        throw new UserError(
            $"Source text is not available for file '{identifier}': this selection was built from a "
            + "graph without its source files. Build the selection from Project.ProjectFiles(...) to "
            + "run adhere-to rules.");

    /// <summary>
    /// The project's dependency graph this selection draws its files from. Internal: the depend-on
    /// assertions read it to compute the object's files or external modules and the dependencies
    /// between the two.
    /// </summary>
    internal Graph Graph => _graph;

    /// <summary>
    /// The scope's selectors, in the order they were applied. Internal: the depend-on assertions read
    /// them to compute the subject's dependency edges.
    /// </summary>
    internal IReadOnlyList<Filter> Filters => _filters;

    /// <summary>
    /// Describes this selection as the scope of a rule, for a report: the entry phrase
    /// <c>project files</c> followed by one clause per selector, in the selector's own words, and one
    /// <c>except</c> clause per exclusion in the exclusion's own words. A selection narrowed by
    /// <c>WithName("Car.cs")</c> is described as <c>project files with name 'Car.cs'</c>, and one
    /// narrowed by <c>InFolder("app")</c> and <c>Except("app/generated")</c> as
    /// <c>project files in folder 'app' except in folder 'app/generated'</c>.
    /// </summary>
    internal string DescribeScope()
    {
        var builder = new StringBuilder("project files");
        foreach (Filter filter in _filters)
        {
            AppendSelectorClause(builder, filter);
        }

        return builder.ToString();
    }

    /// <summary>
    /// Appends one selector's clause to a scope or object description: the selector's own words, the
    /// pattern's glob, and one <c>except</c> clause per exclusion. Shared with the depend-on object's
    /// description.
    /// </summary>
    internal static void AppendSelectorClause(StringBuilder builder, Filter filter)
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
    /// The selector's own words for a match target, shared with the depend-on object's description.
    /// </summary>
    internal static string SelectorWord(MatchTarget target) => target switch
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
