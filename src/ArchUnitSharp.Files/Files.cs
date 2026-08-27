namespace ArchUnitSharp.Files;

using ArchUnitSharp.Common.Extraction;

/// <summary>
/// The files domain module's fluent surface: a scoped selection of the files of one project's
/// <see cref="Graph"/>. It is the ENTRY and SCOPE of a rule chain — built from the entry points
/// <c>Project.ProjectFiles()</c> / <c>Project.Files()</c> and narrowed by the selectors
/// <see cref="WithName"/>, <see cref="InFolder"/>, <see cref="InPath"/> and <see cref="InFile"/>.
/// </summary>
/// <remarks>
/// <para>
/// A <see cref="Files"/> value names a set of files: every file in the graph when no selector has
/// been applied, otherwise exactly the files that match every selector applied so far — selectors
/// combine with AND. The MOOD, PREDICATE and TERMINAL of a rule are the assertion layer's concern and
/// come later; <see cref="Select"/> evaluates the scope's selection so a terminal can consume it.
/// </para>
/// <para>
/// Every selector returns a new <see cref="Files"/> instance and never mutates the one it was called
/// on, so a half-built selection can be stored in a variable and branched from without one branch
/// seeing another's selectors. This type is immutable and safe for concurrent use.
/// </para>
/// </remarks>
public sealed class Files
{
    private readonly Graph _graph;
    private readonly Filter[] _filters;

    /// <summary>
    /// Creates a selection over every file of <paramref name="graph"/>.
    /// </summary>
    /// <param name="graph">The project's dependency graph. Must not be <see langword="null"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="graph"/> is <see langword="null"/>.</exception>
    public Files(Graph graph)
    {
        ArgumentNullException.ThrowIfNull(graph);
        _graph = graph;
        _filters = Array.Empty<Filter>();
    }

    private Files(Graph graph, Filter[] filters)
    {
        _graph = graph;
        _filters = filters;
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
    /// Evaluates the scope: the identifiers of the files this selection names, sorted ordinally. With
    /// no selectors every file of the graph is selected; with one or more, exactly the files that
    /// match all of them. The returned list is a fresh copy on every call.
    /// </summary>
    /// <returns>The selected files' identifiers, sorted.</returns>
    public IReadOnlyList<string> Select() => Projection.FilesProjection.Select(_graph, _filters);

    private Files Add(Filter filter)
    {
        var filters = new Filter[_filters.Length + 1];
        Array.Copy(_filters, filters, _filters.Length);
        filters[_filters.Length] = filter;
        return new Files(_graph, filters);
    }
}
