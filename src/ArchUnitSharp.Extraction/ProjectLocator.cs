namespace ArchUnitSharp.Extraction;

using ArchUnitSharp.Common.Extraction;

/// <summary>
/// Finds the project root by walking up from a start directory until it reaches a directory that
/// contains a <c>.sln</c> or a <c>.csproj</c>. Auto-detection from the current working directory is
/// the default; an explicit start directory is an optional argument, never required.
/// </summary>
/// <remarks>
/// <para>
/// A solution file wins over a project file: the walk looks for a <c>.sln</c> at or above the start
/// first, and only when no ancestor holds one does it look for a <c>.csproj</c>. The root is the
/// directory that contains the file that was found, so a solution at the top of a repository scopes
/// the analysis to the whole repository even when the search starts inside a project beneath it.
/// </para>
/// <para>
/// When a directory holds more than one matching file, the ordinally-first name is used, so the
/// result is deterministic. When no <c>.sln</c> or <c>.csproj</c> can be found, or the start path
/// does not exist, a <see cref="TechnicalError"/> is thrown — locating the project is an environment
/// failure, not a rule outcome.
/// </para>
/// <para>
/// This type is stateless and safe for concurrent use. The <see cref="ProjectLocation"/> it returns
/// is immutable.
/// </para>
/// </remarks>
public static class ProjectLocator
{
    /// <summary>
    /// Locates the project from the current working directory, walking up until a directory
    /// containing a <c>.sln</c> or a <c>.csproj</c> is found.
    /// </summary>
    /// <returns>The located project.</returns>
    /// <exception cref="TechnicalError">No <c>.sln</c> or <c>.csproj</c> exists at or above the current working directory.</exception>
    public static ProjectLocation Locate() => Locate(Environment.CurrentDirectory);

    /// <summary>
    /// Locates the project from <paramref name="startDirectory"/>, walking up until a directory
    /// containing a <c>.sln</c> or a <c>.csproj</c> is found. When <paramref name="startDirectory"/>
    /// names a file, the walk starts from that file's directory.
    /// </summary>
    /// <param name="startDirectory">The directory (or file) to start the search from. Must not be <see langword="null"/> or empty.</param>
    /// <returns>The located project.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="startDirectory"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="startDirectory"/> is empty.</exception>
    /// <exception cref="TechnicalError">The start path does not exist, or no <c>.sln</c> or <c>.csproj</c> exists at or above it.</exception>
    public static ProjectLocation Locate(string startDirectory)
    {
        ArgumentNullException.ThrowIfNull(startDirectory);
        if (startDirectory.Length == 0)
        {
            throw new ArgumentException("The start directory must not be empty.", nameof(startDirectory));
        }

        return TryLocate(startDirectory)
            ?? throw new TechnicalError(
                $"No project could be located at or above '{startDirectory}': no .sln or .csproj file found.");
    }

    private static ProjectLocation? TryLocate(string startDirectory)
    {
        try
        {
            string current = Path.GetFullPath(startDirectory);

            if (File.Exists(current))
            {
                current = Path.GetDirectoryName(current)
                    ?? throw new TechnicalError(
                        $"The path '{startDirectory}' resolves to a file with no parent directory.");
            }
            else if (!Directory.Exists(current))
            {
                throw new TechnicalError($"The start path '{startDirectory}' does not exist.");
            }

            current = PathNormaliser.Normalise(current);

            string? solution = FindUpward(current, "*.sln");
            if (solution is not null)
            {
                string root = Path.GetDirectoryName(solution)!;
                return new ProjectLocation(PathNormaliser.Normalise(root), PathNormaliser.Normalise(solution), null);
            }

            string? project = FindUpward(current, "*.csproj");
            if (project is not null)
            {
                string root = Path.GetDirectoryName(project)!;
                return new ProjectLocation(PathNormaliser.Normalise(root), null, PathNormaliser.Normalise(project));
            }

            return null;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            throw new TechnicalError($"Could not locate a project from '{startDirectory}'.", exception);
        }
    }

    private static string? FindUpward(string startDirectory, string searchPattern)
    {
        string current = startDirectory;
        while (true)
        {
            string? match = Directory
                .EnumerateFiles(current, searchPattern)
                .OrderBy(static path => path, StringComparer.Ordinal)
                .FirstOrDefault();

            if (match is not null)
            {
                return match;
            }

            string? parent = Path.GetDirectoryName(current);
            if (parent is null || parent == current)
            {
                return null;
            }

            current = parent;
        }
    }
}
