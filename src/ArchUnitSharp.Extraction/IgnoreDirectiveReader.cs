namespace ArchUnitSharp.Extraction;

using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

/// <summary>
/// The per-line ignore convention: a <c>// archunit: ignore</c> comment that excludes the single
/// <c>using</c> directive it marks from the dependency graph. ArchUnitPython's
/// <c># archunit: ignore</c> rendered in the C# comment idiom.
/// </summary>
/// <remarks>
/// <para>
/// An ignore comment applies to exactly one directive. It can trail the directive on its own line —
/// <c>using System.Linq; // archunit: ignore</c> — or stand alone on the line immediately above it.
/// A directive that is marked produces no edges at all, whether the name it references resolves to a
/// project file or stays external. It is not that the edge is filtered out later; the directive is
/// skipped at resolution, so it contributes nothing to the graph.
/// </para>
/// <para>
/// The directive may be scoped to named namespaces: listing names after <c>ignore</c> —
/// <c>// archunit: ignore MyApp.Models</c> — restricts the ignore to directives whose referenced
/// name is one of the listed names or lies beneath one (equal to a listed name, or starting with
/// <c>listed.</c>). A scoped comment that names none of the directive's possible names is inert and
/// the directive is kept. Both the <c>archunit:</c> and <c>archunit-</c> spellings are recognised;
/// every other comment — including doc comments (<c>///</c>) and block comments — is not an ignore
/// directive.
/// </para>
/// <para>
/// The comment must be in the directive's own trivia: the directive's trailing trivia on its own
/// line, or its leading trivia on the line immediately above. A comment anywhere else in the file
/// cannot mark a directive, and a standalone comment two or more lines away is ignored.
/// </para>
/// <para>
/// This type is stateless and safe for concurrent use. It reads only syntax trivia; nothing in it
/// touches the filesystem.
/// </para>
/// </remarks>
internal static class IgnoreDirectiveReader
{
    private static readonly Regex DirectivePattern = new(
        @"\A//\s*archunit(?::|-)\s*ignore(?<modules>(?:\s+[\w.]+)*)\s*\z",
        RegexOptions.Compiled);

    /// <summary>
    /// Returns <see langword="true"/> when the directive is marked by an applicable ignore comment,
    /// and so should produce no edges.
    /// </summary>
    public static bool ShouldIgnore(UsingDirectiveSyntax directive)
    {
        string name = directive.Name?.ToString() ?? string.Empty;
        FileLinePositionSpan span = directive.GetLocation().GetLineSpan();

        return HasApplicableComment(directive.GetTrailingTrivia(), span.EndLinePosition.Line, name)
            || HasApplicableComment(directive.GetLeadingTrivia(), span.StartLinePosition.Line - 1, name);
    }

    private static bool HasApplicableComment(SyntaxTriviaList trivia, int commentLine, string name)
    {
        foreach (SyntaxTrivia entry in trivia)
        {
            if (!entry.IsKind(SyntaxKind.SingleLineCommentTrivia)
                || entry.GetLocation().GetLineSpan().StartLinePosition.Line != commentLine)
            {
                continue;
            }

            string[]? modules = MatchModules(entry.ToString());
            if (modules is null)
            {
                continue;
            }

            if (modules.Length == 0 || MatchesScope(name, modules))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Returns the scoped namespaces an ignore comment names, or <see langword="null"/> when the
    /// comment is not an ignore comment at all.
    /// </summary>
    private static string[]? MatchModules(string commentText)
    {
        Match match = DirectivePattern.Match(commentText);
        if (!match.Success)
        {
            return null;
        }

        string modules = match.Groups["modules"].Value.Trim();
        return modules.Length == 0
            ? Array.Empty<string>()
            : modules.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
    }

    private static bool MatchesScope(string name, string[] modules)
    {
        foreach (string module in modules)
        {
            if (name == module || name.StartsWith(module + ".", StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
