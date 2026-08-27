namespace ArchUnitSharp.Extraction;

using ArchUnitSharp.Common.Extraction;

/// <summary>
/// Reads a project's source files from disk and extracts the canonical import <see cref="Edge"/>s of
/// its dependency graph. The filesystem half of import extraction: it turns a list of enumerated
/// <see cref="SourceFile"/>s into the edges <see cref="ImportResolver"/> would compute from their
/// contents, normalised by <see cref="ImportEdgeNormaliser"/>.
/// </summary>
/// <remarks>
/// <para>
/// Each file's text is read from its <see cref="SourceFile.AbsolutePath"/> and handed, along with
/// the file, to <see cref="ImportResolver.Resolve"/>. A file that cannot be read is an environment
/// failure and surfaces as a <see cref="TechnicalError"/>; a file whose text fails to parse is
/// skipped by the resolver, not fatal.
/// </para>
/// <para>
/// The resolver's raw output is normalised before it is returned: every file gets a self-edge, so a
/// file with no dependencies still appears as a node, and parallel edges are merged with their import
/// kinds unioned, so <c>(source, target)</c> is unique. The result is sorted.
/// </para>
/// <para>
/// Directives marked with the per-line ignore convention — a <c>// archunit: ignore</c> comment on
/// the directive's line or the line immediately above — produce no edges; see
/// <see cref="ImportResolver"/>.
/// </para>
/// <para>
/// This type is stateless and safe for concurrent use. The list it returns is a fresh copy on every
/// call, and the <see cref="Edge"/> values in it are immutable.
/// </para>
/// </remarks>
public static class ImportExtractor
{
    /// <summary>
    /// Reads <paramref name="sourceFiles"/> from disk and returns the canonical import edges of the
    /// project's dependency graph: a self-edge per file, the directives' edges with parallel edges
    /// merged and their import kinds unioned, sorted.
    /// </summary>
    /// <param name="sourceFiles">The project's source files. Must not be <see langword="null"/>.</param>
    /// <returns>The canonical import edges, sorted.</returns>
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

        return ImportEdgeNormaliser.Normalise(sourceFiles, ImportResolver.Resolve(sourceFiles, codes));
    }
}
