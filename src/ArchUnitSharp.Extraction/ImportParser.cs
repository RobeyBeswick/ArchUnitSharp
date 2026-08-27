namespace ArchUnitSharp.Extraction;

using ArchUnitSharp.Common.Extraction;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

/// <summary>
/// Turns C# source text into the <see cref="Import"/> data its <c>using</c> directives carry. The
/// pure, syntax-only half of extraction: no filesystem, no resolution. A source text that fails to
/// parse yields no imports at all — an unreadable file is skipped, not fatal.
/// </summary>
/// <remarks>
/// <para>
/// Parsing is Roslyn's own — a <see cref="CSharpSyntaxTree"/> built from the text, never a
/// hand-rolled scan of the source. A file that parses with error diagnostics is treated as
/// unreadable: it returns an empty list, and callers that build a project graph skip it. A file with
/// only warnings parses normally.
/// </para>
/// <para>
/// The returned imports are sorted ordinally by name, then by kind, so the output is deterministic
/// and reports built from it are reproducible.
/// </para>
/// <para>
/// This type is stateless and safe for concurrent use. The list it returns is a fresh copy on every
/// call, and the <see cref="Import"/> values in it are immutable.
/// </para>
/// </remarks>
public static class ImportParser
{
    /// <summary>
    /// Parses <paramref name="sourceCode"/> and returns the imports its <c>using</c> directives
    /// carry, sorted by name then kind. Returns an empty list when the source fails to parse.
    /// </summary>
    /// <param name="sourceCode">The C# source text to parse. Must not be <see langword="null"/>.</param>
    /// <returns>The imports in the source, sorted; empty when the source fails to parse.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="sourceCode"/> is <see langword="null"/>.</exception>
    public static IReadOnlyList<Import> Parse(string sourceCode)
    {
        ArgumentNullException.ThrowIfNull(sourceCode);

        SyntaxTree tree = CSharpSyntaxTree.ParseText(sourceCode);
        if (HasErrors(tree))
        {
            return Array.Empty<Import>();
        }

        return UsingDirectiveReader.Collect(tree)
            .Select(static directive =>
            {
                (ImportKind kind, string name) = UsingDirectiveReader.Describe(directive);
                return new Import(kind, name);
            })
            .OrderBy(static import => import.Name, StringComparer.Ordinal)
            .ThenBy(static import => import.Kind)
            .ToArray();
    }

    internal static bool HasErrors(SyntaxTree tree) =>
        tree.GetDiagnostics().Any(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
}
