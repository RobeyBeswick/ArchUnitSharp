namespace ArchUnitSharp.Extraction;

using System.Collections.Immutable;
using ArchUnitSharp.Common.Extraction;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

/// <summary>
/// Resolves the <c>using</c> directives of a project's source files to targets, producing the import
/// <see cref="Edge"/>s of the dependency graph: an edge from each file to every file that declares
/// the namespace or type its directives reference, or to an external target when nothing in the
/// project does. The pure half of extraction: source texts in, edges out, no filesystem.
/// </summary>
/// <remarks>
/// <para>
/// Resolution uses Roslyn's own compiler: the files' syntax trees are parsed into a
/// <see cref="CSharpCompilation"/> and each directive's name is bound through the semantic model, so
/// the language's resolver — not path arithmetic on import strings — decides what the name refers to.
/// A directive whose name binds to a namespace or type the project declares yields one edge per file
/// declaring it; one that binds to nothing yields an external edge whose target is the name as
/// written.
/// </para>
/// <para>
/// A file whose source fails to parse is skipped: it contributes no edges and its namespaces are not
/// resolvable targets, so a directive that would have bound to it binds to nothing and becomes
/// external. A file with only warnings parses normally.
/// </para>
/// <para>
/// Conditional compilation is decided by Roslyn's own rules under a deliberate, documented symbol
/// set: the trees are parsed with <see cref="CSharpParseOptions"/> carrying no preprocessor symbols
/// at all. This resolver is the pure half of extraction and has no access to the project's build
/// configuration, so it cannot know the real <c>DefineConstants</c>; it treats every symbol as
/// undefined. A <c>using</c> inside a <c>#if</c>/<c>#else</c> region whose condition is false under
/// that empty set is inactive — its text is disabled trivia, not a directive — and produces no edge;
/// one under a condition that is true is collected. Relative to any one real build this can both omit
/// edges a build would have and add edges a build omits, which is the documented over-approximation
/// rather than a silent accident.
/// </para>
/// <para>
/// Edges are returned sorted by source, then target, then import kind, so the output is stable and
/// reports built from it are reproducible. Parallel edges are not merged here and self-edges are not
/// added here; the graph layer that consumes these edges is responsible for both.
/// </para>
/// <para>
/// This type is stateless and safe for concurrent use. The list it returns is a fresh copy on every
/// call, and the <see cref="Edge"/> values in it are immutable.
/// </para>
/// </remarks>
public static class ImportResolver
{
    private static readonly CSharpParseOptions ParseOptions =
        CSharpParseOptions.Default.WithPreprocessorSymbols(ImmutableArray<string>.Empty);

    /// <summary>
    /// Resolves the directives of the given source files and returns the import edges they imply.
    /// </summary>
    /// <param name="sourceFiles">The project's source files, in the order their codes are supplied. Must not be <see langword="null"/>.</param>
    /// <param name="sourceCodes">Each file's source text, matching <paramref name="sourceFiles"/> element for element. Must not be <see langword="null"/>.</param>
    /// <returns>The import edges implied by the files' directives, sorted.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="sourceFiles"/> or <paramref name="sourceCodes"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="sourceCodes"/> has a different number of entries than <paramref name="sourceFiles"/>, or contains a <see langword="null"/> entry.</exception>
    public static IReadOnlyList<Edge> Resolve(
        IReadOnlyList<SourceFile> sourceFiles,
        IReadOnlyList<string> sourceCodes)
    {
        ArgumentNullException.ThrowIfNull(sourceFiles);
        ArgumentNullException.ThrowIfNull(sourceCodes);
        if (sourceFiles.Count != sourceCodes.Count)
        {
            throw new ArgumentException(
                $"{nameof(sourceFiles)} and {nameof(sourceCodes)} must have the same number of entries.",
                nameof(sourceCodes));
        }

        var parsed = new List<(SourceFile File, SyntaxTree Tree)>(sourceFiles.Count);
        for (int index = 0; index < sourceFiles.Count; index++)
        {
            string code = sourceCodes[index]
                ?? throw new ArgumentException(
                    $"{nameof(sourceCodes)} must not contain a null entry.",
                    nameof(sourceCodes));
            parsed.Add((sourceFiles[index], CSharpSyntaxTree.ParseText(code, ParseOptions, path: sourceFiles[index].AbsolutePath)));
        }

        var resolvable = parsed
            .Where(static entry => !ImportParser.HasErrors(entry.Tree))
            .ToArray();

        CSharpCompilation compilation = CSharpCompilation.Create(
            "ArchUnitSharpExtraction",
            resolvable.Select(static entry => entry.Tree),
            references: Array.Empty<MetadataReference>());

        var identifierByTree = new Dictionary<SyntaxTree, string>();
        foreach ((SourceFile file, SyntaxTree tree) in resolvable)
        {
            identifierByTree[tree] = file.Identifier;
        }

        var edges = new List<Edge>();
        foreach ((SourceFile file, SyntaxTree tree) in resolvable)
        {
            SemanticModel model = compilation.GetSemanticModel(tree);
            foreach (UsingDirectiveSyntax directive in UsingDirectiveReader.Collect(tree))
            {
                (ImportKind kind, string name) = UsingDirectiveReader.Describe(directive);
                ISymbol? symbol = model.GetSymbolInfo(directive.Name!).Symbol;

                if (symbol is INamespaceSymbol or ITypeSymbol)
                {
                    string[] targets = DeclaringFileIdentifiers(symbol, identifierByTree);
                    if (targets.Length > 0)
                    {
                        foreach (string target in targets)
                        {
                            edges.Add(new Edge(file.Identifier, target, external: false, kind));
                        }

                        continue;
                    }
                }

                edges.Add(new Edge(file.Identifier, name, external: true, kind));
            }
        }

        edges.Sort(static (left, right) =>
        {
            int source = StringComparer.Ordinal.Compare(left.Source, right.Source);
            if (source != 0)
            {
                return source;
            }

            int target = StringComparer.Ordinal.Compare(left.Target, right.Target);
            if (target != 0)
            {
                return target;
            }

            return left.ImportKinds.CompareTo(right.ImportKinds);
        });

        return edges.ToArray();
    }

    private static string[] DeclaringFileIdentifiers(
        ISymbol symbol,
        IReadOnlyDictionary<SyntaxTree, string> identifierByTree)
    {
        var identifiers = new HashSet<string>(StringComparer.Ordinal);
        foreach (SyntaxReference reference in symbol.DeclaringSyntaxReferences)
        {
            if (identifierByTree.TryGetValue(reference.SyntaxTree, out string? identifier))
            {
                identifiers.Add(identifier);
            }
        }

        return identifiers.ToArray();
    }
}
