namespace ArchUnitSharp.Extraction;

using ArchUnitSharp.Common.Extraction;

/// <summary>
/// Reads a project's source files from disk and extracts the import <see cref="Edge"/>s of its
/// dependency graph. The filesystem half of import extraction: it turns a list of enumerated
/// <see cref="SourceFile"/>s into the edges <see cref="ImportResolver"/> would compute from their
/// contents.
/// </summary>
/// <remarks>
/// <para>
/// Each file's text is read from its <see cref="SourceFile.AbsolutePath"/> and handed, along with
/// the file, to <see cref="ImportResolver.Resolve"/>. A file that cannot be read is an environment
/// failure and surfaces as a <see cref="TechnicalError"/>; a file whose text fails to parse is
/// skipped by the resolver, not fatal.
/// </para>
/// <para>
/// This type is stateless and safe for concurrent use. The list it returns is a fresh copy on every
/// call, and the <see cref="Edge"/> values in it are immutable.
/// </para>
/// </remarks>
public static class ImportExtractor
{
    /// <summary>
    /// Reads <paramref name="sourceFiles"/> from disk and returns the import edges their directives
    /// imply, sorted.
    /// </summary>
    /// <param name="sourceFiles">The project's source files. Must not be <see langword="null"/>.</param>
    /// <returns>The import edges implied by the files' directives, sorted.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="sourceFiles"/> is <see langword="null"/>.</exception>
    /// <exception cref="TechnicalError">A file cannot be read from disk.</exception>
    public static IReadOnlyList<Edge> Extract(IReadOnlyList<SourceFile> sourceFiles)
    {
        ArgumentNullException.ThrowIfNull(sourceFiles);

        var codes = new List<string>(sourceFiles.Count);
        foreach (SourceFile file in sourceFiles)
        {
            try
            {
                codes.Add(File.ReadAllText(file.AbsolutePath));
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                throw new TechnicalError($"Failed to read source file '{file.Identifier}'.", exception);
            }
        }

        return ImportResolver.Resolve(sourceFiles, codes);
    }
}
