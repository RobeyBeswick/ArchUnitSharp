namespace ArchUnitSharp;

using ArchUnitSharp.Common.Extraction;
using ArchUnitSharp.Extraction;

/// <summary>
/// The composition root and the DSL's entry points: the noun phrases that begin a rule chain.
/// <c>project files</c> (alias <c>files</c>) locates a project, extracts its dependency graph and
/// hands the caller the files module's fluent surface over that graph.
/// </summary>
/// <remarks>
/// <para>
/// Each entry point returns an immutable <see cref="ArchUnitSharp.Files.Files"/> selection. The
/// parameterless overloads locate the project from the current working directory; the overloads that
/// take a <see cref="ProjectLocation"/> analyse exactly the located project, which is how a caller
/// targets a repository other than the current one.
/// </para>
/// <para>
/// This type is stateless and safe for concurrent use. The extraction it triggers goes through the
/// graph cache, so a suite that checks many rules over one project reads and parses its source files
/// once.
/// </para>
/// <para>
/// Each selection is wired with a source-text provider that reads a file's content from the located
/// project's root on demand, which is what an <c>adhere to</c> rule hands its custom predicate. The
/// read happens lazily, per selected file and per check, and only when such a rule runs.
/// </para>
/// </remarks>
public static class Project
{
    /// <summary>
    /// <c>project files</c>: the files of the project located from the current working directory.
    /// </summary>
    /// <returns>A selection of every file of the located project.</returns>
    /// <exception cref="TechnicalError">No <c>.sln</c> or <c>.csproj</c> exists at or above the current working directory, or the project cannot be read.</exception>
    public static ArchUnitSharp.Files.Files ProjectFiles() => ProjectFiles(ProjectLocator.Locate());

    /// <summary>
    /// <c>project files</c>: the files of exactly the given project.
    /// </summary>
    /// <param name="location">The project to analyse, as produced by <see cref="ProjectLocator.Locate()"/>. Must not be <see langword="null"/>.</param>
    /// <returns>A selection of every file of the located project.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="location"/> is <see langword="null"/>.</exception>
    /// <exception cref="TechnicalError">The project cannot be read.</exception>
    public static ArchUnitSharp.Files.Files ProjectFiles(ProjectLocation location) =>
        new ArchUnitSharp.Files.Files(GraphCache.Get(location), identifier => ReadSource(location, identifier));

    /// <summary>
    /// <c>files</c>, the alias of <c>project files</c>: the files of the project located from the
    /// current working directory.
    /// </summary>
    /// <returns>A selection of every file of the located project.</returns>
    /// <exception cref="TechnicalError">No <c>.sln</c> or <c>.csproj</c> exists at or above the current working directory, or the project cannot be read.</exception>
    public static ArchUnitSharp.Files.Files Files() => ProjectFiles();

    /// <summary>
    /// <c>files</c>, the alias of <c>project files</c>: the files of exactly the given project.
    /// </summary>
    /// <param name="location">The project to analyse, as produced by <see cref="ProjectLocator.Locate()"/>. Must not be <see langword="null"/>.</param>
    /// <returns>A selection of every file of the located project.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="location"/> is <see langword="null"/>.</exception>
    /// <exception cref="TechnicalError">The project cannot be read.</exception>
    public static ArchUnitSharp.Files.Files Files(ProjectLocation location) => ProjectFiles(location);

    /// <summary>
    /// The source-text provider's disk half: reads one file's full text, given its project-relative
    /// identifier, by joining it onto the located project's root. A file that cannot be read is an
    /// environment failure and surfaces as a <see cref="TechnicalError"/>, the same treatment
    /// extraction gives an unreadable source file.
    /// </summary>
    /// <param name="location">The project whose root resolves the identifier. Must not be <see langword="null"/>.</param>
    /// <param name="identifier">The file's project-relative identifier. Must not be <see langword="null"/>.</param>
    /// <returns>The file's full source text.</returns>
    /// <exception cref="TechnicalError">The file cannot be read from disk.</exception>
    private static string ReadSource(ProjectLocation location, string identifier)
    {
        try
        {
            return File.ReadAllText(Path.Combine(location.Root, identifier));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            throw new TechnicalError($"Failed to read source file '{identifier}'.", exception);
        }
    }
}
