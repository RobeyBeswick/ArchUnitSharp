namespace ArchUnitSharp.Extraction;

/// <summary>
/// Where a project lives on disk: the absolute path of the directory that <see cref="ProjectLocator"/>
/// chose as the project root, together with the solution or project file that made it the root.
/// </summary>
/// <remarks>
/// <para>
/// Exactly one of <see cref="SolutionFile"/> and <see cref="ProjectFile"/> is set by the locator: a
/// <c>.sln</c> wins when one exists at or above the search start, and a <c>.csproj</c> is used only
/// when no solution does. The constructor requires at least one of them, so a location always names
/// the file that locates it. A locating file cannot be removed once present: a
/// <see langword="with"/> expression may add the other locating file but cannot clear the one already
/// set, so changing which file locates a project — replacing a <c>.sln</c> with a <c>.csproj</c> or
/// vice versa — requires constructing a new <see cref="ProjectLocation"/>.
/// </para>
/// <para>
/// Every path this type stores is normalised to forward-slash separators, whatever the operating
/// system: the constructor and the init accessors normalise the values they receive, so a path built
/// with <c>Path.Combine</c> on Windows cannot leak backslashes into the graph.
/// <see cref="ProjectLocator"/> additionally supplies absolute paths, so a located project's root and
/// file paths are absolute as well as normalised. <see cref="Root"/> is the boundary from which file
/// identifiers are made relative, so identifiers and <see cref="Root"/> never mix conventions.
/// </para>
/// <para>
/// This type is immutable and safe for concurrent use: once constructed, its values never change. Two
/// locations are equal when their root, solution file and project file are equal.
/// </para>
/// </remarks>
public sealed record ProjectLocation
{
    private readonly string _root;
    private readonly string? _solutionFile;
    private readonly string? _projectFile;

    /// <summary>
    /// The path of the project root with forward-slash separators. When produced by
    /// <see cref="ProjectLocator"/> it is absolute; when constructed directly, the value given is
    /// stored with its separators normalised. Must not be <see langword="null"/> or empty; both the
    /// constructor and a <see langword="with"/> expression route through the same validation, so
    /// neither can introduce a bad value.
    /// </summary>
    public string Root
    {
        get => _root;
        init => _root = RequireRoot(value);
    }

    /// <summary>
    /// The forward-slash normalised path of the <c>.sln</c> that locates the project, or
    /// <see langword="null"/> when the project was located by a <see cref="ProjectFile"/> instead.
    /// When set, it must not be empty, and it cannot be cleared once present: removing a locating
    /// file from a location requires a new <see cref="ProjectLocation"/>.
    /// </summary>
    public string? SolutionFile
    {
        get => _solutionFile;
        init => _solutionFile = RequireLocatingFileValue(value, nameof(SolutionFile));
    }

    /// <summary>
    /// The forward-slash normalised path of the <c>.csproj</c> that locates the project, or
    /// <see langword="null"/> when the project was located by a <see cref="SolutionFile"/> instead.
    /// When set, it must not be empty, and it cannot be cleared once present: removing a locating
    /// file from a location requires a new <see cref="ProjectLocation"/>.
    /// </summary>
    public string? ProjectFile
    {
        get => _projectFile;
        init => _projectFile = RequireLocatingFileValue(value, nameof(ProjectFile));
    }

    /// <summary>
    /// Creates a project location.
    /// </summary>
    /// <param name="root">The path of the project root; must not be <see langword="null"/> or empty.</param>
    /// <param name="solutionFile">The path of the locating <c>.sln</c>, or <see langword="null"/> when the project is located by a project file.</param>
    /// <param name="projectFile">The path of the locating <c>.csproj</c>, or <see langword="null"/> when the project is located by a solution file.</param>
    /// <exception cref="ArgumentNullException"><paramref name="root"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="root"/> is empty, a supplied file path is empty, or both <paramref name="solutionFile"/> and <paramref name="projectFile"/> are <see langword="null"/>.</exception>
    public ProjectLocation(string root, string? solutionFile, string? projectFile)
    {
        _root = RequireRoot(root);
        _solutionFile = NormaliseOptionalPath(solutionFile, nameof(SolutionFile));
        _projectFile = NormaliseOptionalPath(projectFile, nameof(ProjectFile));
        RequireLocatingFile(solutionFile, projectFile, nameof(solutionFile));
    }

    private static string RequireRoot(string root) =>
        root is null
            ? throw new ArgumentNullException(nameof(Root))
            : root.Length == 0
                ? throw new ArgumentException("The project root must not be empty.", nameof(Root))
                : PathNormaliser.Normalise(root);

    private static string? NormaliseOptionalPath(string? value, string propertyName) =>
        value is { Length: 0 }
            ? throw new ArgumentException($"{propertyName} must not be empty when set.", propertyName)
            : value is null
                ? null
                : PathNormaliser.Normalise(value);

    private static string RequireLocatingFileValue(string? value, string propertyName) =>
        value is null
            ? throw new ArgumentException(
                $"{propertyName} cannot be removed from a project location; changing which file locates a project requires a new {nameof(ProjectLocation)}.",
                propertyName)
            : value.Length == 0
                ? throw new ArgumentException($"{propertyName} must not be empty when set.", propertyName)
                : PathNormaliser.Normalise(value);

    private static void RequireLocatingFile(string? solutionFile, string? projectFile, string parameterName)
    {
        if (solutionFile is null && projectFile is null)
        {
            throw new ArgumentException(
                "A project location must have a solution file or a project file.",
                parameterName);
        }
    }
}
