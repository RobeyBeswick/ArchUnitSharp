namespace ArchUnitSharp.Metrics.Extraction;

using System.Linq;
using ArchUnitSharp.Metrics;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

/// <summary>
/// The metrics module's extraction boundary: parses one file's source text and produces the
/// <see cref="FileInfo"/> the count and cohesion metrics measure. It is the module's only Roslyn
/// contact — the calculation, projection and assertion layers are pure and never parse — and it is
/// the only place a source file's text becomes count facts and method-field access facts.
/// </summary>
/// <remarks>
/// <para>
/// The extraction reads no filesystem: it parses the source text it is handed, which the fluent
/// surface's source provider supplies (wired by the composition root, exactly as the files module's
/// <c>adhere to</c> provider is). It parses with <see cref="CSharpParseOptions.Default"/>, the same
/// deliberate no-preprocessor-symbols choice the graph extraction makes, so text under a false
/// <c>#if</c> condition is disabled trivia and contributes nothing — a directive there is not an
/// import, a statement there is not a statement.
/// </para>
/// <para>
/// <see cref="FileInfo.LinesOfCode"/> counts the lines of the raw text that are not blank or
/// whitespace only. <see cref="FileInfo.StatementCount"/> counts every statement of the syntax tree
/// that is not itself a block — an <c>if</c>, a <c>return</c>, a declaration and a local function
/// each count one, and the blocks that group them do not. <see cref="FileInfo.ImportCount"/> counts
/// every <c>using</c> directive of the syntax tree. <see cref="FileInfo.ClassCount"/> and
/// <see cref="FileInfo.InterfaceCount"/> count the file's <c>class</c> and <c>interface</c>
/// declarations, and <see cref="FileInfo.ClassInfos"/> carries a <see cref="ClassInfo"/> per class.
/// Each class's <see cref="MethodInfo"/> and <see cref="FieldInfo"/> carry the method-field access
/// facts the cohesion metrics are computed from.
/// </para>
/// <para>
/// This type is stateless and safe for concurrent use.
/// </para>
/// </remarks>
internal static class MetricsExtractor
{
    /// <summary>
    /// Parses <paramref name="sourceText"/> and returns the file's extracted info: the non-blank line
    /// count, statement count, import count, class count, interface count and per-class info.
    /// </summary>
    /// <param name="path">The file's graph identifier. Must not be <see langword="null"/> or empty.</param>
    /// <param name="sourceText">The file's full source text. Must not be <see langword="null"/>.</param>
    /// <returns>The file's extracted info.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> or <paramref name="sourceText"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="path"/> is empty.</exception>
    public static FileInfo Extract(string path, string sourceText)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(sourceText);

        var tree = CSharpSyntaxTree.ParseText(sourceText, CSharpParseOptions.Default);
        CompilationUnitSyntax root = tree.GetCompilationUnitRoot();

        var visitor = new SourceMetricsVisitor(path);
        visitor.Visit(root);

        ClassInfo[] classInfos = visitor.ClassInfos
            .OrderBy(static info => info.Identifier, StringComparer.Ordinal)
            .ToArray();

        return new FileInfo(
            path,
            NonBlankLineCount(sourceText),
            root.DescendantNodes()
                .OfType<StatementSyntax>()
                .Count(static statement => statement is not BlockSyntax),
            root.DescendantNodes().OfType<UsingDirectiveSyntax>().Count(),
            classInfos.Length,
            root.DescendantNodes().OfType<InterfaceDeclarationSyntax>().Count(),
            classInfos);
    }

    /// <summary>
    /// Counts the lines of a source text that are not blank or whitespace only. Windows line endings
    /// are counted once: a trailing carriage return makes a line non-blank but an empty line's
    /// carriage return is whitespace, so <c>"a\r\n\r\nb\r\n"</c> counts two.
    /// </summary>
    private static int NonBlankLineCount(string sourceText)
    {
        int count = 0;
        foreach (string line in sourceText.Split('\n'))
        {
            if (!string.IsNullOrWhiteSpace(line))
            {
                count++;
            }
        }

        return count;
    }
}
