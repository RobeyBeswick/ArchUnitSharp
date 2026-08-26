namespace ArchUnitSharp.Extraction;

using ArchUnitSharp.Common.Extraction;

/// <summary>
/// Walks a project's directory tree and enumerates its C# source files, pruning directories named in
/// <see cref="SourceEnumerationOptions"/> and never following file or directory symlinks or junctions.
/// The result is sorted by identifier so reports built from it are reproducible.
/// </summary>
/// <remarks>
/// <para>
/// Every <c>*.cs</c> file under the project root becomes a <see cref="SourceFile"/> unless it sits in
/// a directory the options exclude or is itself a symlink or junction. Exclusion is by directory name
/// at any depth — a <c>bin</c> anywhere in the tree is skipped — and the default set is the
/// build-output, vendored-dependency, version-control and cache directories named in
/// <see cref="SourceEnumerationOptions"/>.
/// </para>
/// <para>
/// Reparse points (file and directory symlinks and junctions) are skipped during the walk: a symlink
/// to an ancestor cannot loop the enumeration, and a symlink pointing outside the project root cannot
/// pull out-of-tree files into the graph. Every enumerated file therefore lies physically inside the
/// project root, and its identifier is project-relative by construction.
/// </para>
/// <para>
/// Identifiers are the files' paths relative to the project root, normalised to forward-slash
/// separators; absolute paths are likewise normalised. Files are returned sorted by identifier under
/// an ordinal comparison, so the enumeration is stable and deterministic.
/// </para>
/// <para>
/// This type is stateless and safe for concurrent use. The <see cref="SourceFile"/> list it returns
/// is a fresh copy on every call.
/// </para>
/// </remarks>
public static class SourceEnumerator
{
    private static readonly EnumerationOptions _directoryOptions = new()
    {
        AttributesToSkip = FileAttributes.ReparsePoint,
        IgnoreInaccessible = false,
        RecurseSubdirectories = false,
    };

    private static readonly EnumerationOptions _fileOptions = new()
    {
        AttributesToSkip = FileAttributes.ReparsePoint,
        IgnoreInaccessible = false,
        RecurseSubdirectories = false,
    };

    /// <summary>
    /// Enumerates the C# source files under <paramref name="location"/>'s root, sorted by identifier.
    /// </summary>
    /// <param name="location">The project to enumerate. Must not be <see langword="null"/>.</param>
    /// <param name="options">The exclusion options; <see langword="null"/> means <see cref="SourceEnumerationOptions.DefaultExcludedDirectories"/>.</param>
    /// <returns>The project's source files, sorted by identifier.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="location"/> is <see langword="null"/>.</exception>
    /// <exception cref="TechnicalError">The project root does not exist, or the tree cannot be read.</exception>
    public static IReadOnlyList<SourceFile> Enumerate(ProjectLocation location, SourceEnumerationOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(location);

        string root = Path.GetFullPath(location.Root);

        if (!Directory.Exists(root))
        {
            throw new TechnicalError($"The project root '{location.Root}' does not exist.");
        }

        var excluded = new HashSet<string>(
            options?.ExcludedDirectories ?? SourceEnumerationOptions.DefaultExcludedDirectories,
            StringComparer.OrdinalIgnoreCase);

        var files = new List<SourceFile>();
        try
        {
            var pending = new Stack<string>();
            pending.Push(root);

            while (pending.Count > 0)
            {
                string directory = pending.Pop();

                foreach (string subdirectory in Directory.EnumerateDirectories(directory, "*", _directoryOptions))
                {
                    if (!IsReparsePoint(subdirectory) && !excluded.Contains(Path.GetFileName(subdirectory)))
                    {
                        pending.Push(subdirectory);
                    }
                }

                foreach (string file in Directory.EnumerateFiles(directory, "*.cs", _fileOptions))
                {
                    string identifier = PathNormaliser.Normalise(Path.GetRelativePath(root, file));
                    files.Add(new SourceFile(identifier, PathNormaliser.Normalise(file)));
                }
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            throw new TechnicalError($"Failed to enumerate source files under '{location.Root}'.", exception);
        }

        files.Sort(static (left, right) => StringComparer.Ordinal.Compare(left.Identifier, right.Identifier));
        return files.ToArray();
    }

    private static bool IsReparsePoint(string path) =>
        (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0;
}
