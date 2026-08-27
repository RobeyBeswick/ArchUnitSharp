namespace ArchUnitSharp.Extraction;

using ArchUnitSharp.Common.Extraction;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

/// <summary>
/// The shared Roslyn walk over a C# syntax tree that both <see cref="ImportParser"/> (which turns
/// directives into <see cref="Import"/> data) and <see cref="ImportResolver"/> (which binds the
/// directives to targets) use, so the two never diverge on what counts as an import or how a
/// directive is classified.
/// </summary>
internal static class UsingDirectiveReader
{
    /// <summary>
    /// Returns every <c>using</c> directive in the tree, in source order.
    /// </summary>
    public static IReadOnlyList<UsingDirectiveSyntax> Collect(SyntaxTree tree) =>
        tree.GetRoot()
            .DescendantNodes()
            .OfType<UsingDirectiveSyntax>()
            .ToArray();

    /// <summary>
    /// Classifies a directive into the single <see cref="ImportKind"/> it carries and the name it
    /// references. The name is the directive's right-hand side exactly as written. A directive that
    /// combines markers — a <c>global using static</c> or a <c>global using Foo = Bar;</c> — is
    /// classified by the most distinctive marker present: global first, then static, then alias.
    /// </summary>
    public static (ImportKind Kind, string Name) Describe(UsingDirectiveSyntax directive)
    {
        ImportKind kind = directive switch
        {
            { GlobalKeyword.RawKind: not 0 } => ImportKind.GlobalUsing,
            { StaticKeyword.RawKind: not 0 } => ImportKind.UsingStatic,
            { Alias: not null } => ImportKind.AliasUsing,
            _ => ImportKind.Using,
        };

        return (kind, directive.Name!.ToString());
    }
}
