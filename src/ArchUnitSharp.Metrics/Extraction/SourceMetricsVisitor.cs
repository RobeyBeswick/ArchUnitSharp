namespace ArchUnitSharp.Metrics.Extraction;

using System.Collections.Generic;
using ArchUnitSharp.Metrics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

/// <summary>
/// The syntax walk that extracts the <see cref="ClassInfo"/> values of one C# file: one info per
/// <c>class</c> declaration, nested declarations included, each carrying its fully qualified name and
/// its direct <c>method</c> and field declarations. It is the class half of the metrics extraction;
/// the file-level counts (<c>lines of code</c>, <c>statements</c>, <c>imports</c> and
/// <c>interfaces</c>) are computed by <see cref="MetricsExtractor"/> from the same syntax tree the
/// visitor walks.
/// </summary>
/// <remarks>
/// <para>
/// The visitor descends the whole compilation unit, so a nested class declaration inside a class,
/// record, struct or interface is visited and becomes its own <see cref="ClassInfo"/> — its methods
/// and fields are its own, not its enclosing type's. A class's methods are its direct
/// <c>method</c> declarations (constructors, destructors, operators and accessors are not methods)
/// and its fields are one per variable its field declarations name. Each info's <see cref="ClassInfo.Name"/>
/// is the class's fully qualified name with dots: the namespace and every enclosing type joined to the
/// class's own name.
/// </para>
/// <para>
/// Roslyn's non-generic <see cref="CSharpSyntaxVisitor"/> does not traverse on its own — its
/// <c>DefaultVisit</c> is a no-op — so this visitor overrides <see cref="DefaultVisit"/> to walk every
/// child node, which is what carries the visit from the compilation unit down to each class
/// declaration.
/// </para>
/// <para>
/// This type is stateless between walks beyond the accumulated <see cref="ClassInfos"/> list and is
/// not safe for concurrent use: create one visitor per file.
/// </para>
/// </remarks>
internal sealed class SourceMetricsVisitor : CSharpSyntaxVisitor
{
    private readonly string _filePath;
    private readonly List<ClassInfo> _classInfos = new();

    /// <summary>
    /// Creates a visitor that attributes each class it finds to <paramref name="filePath"/>.
    /// </summary>
    /// <param name="filePath">The file the visitor extracts classes from.</param>
    public SourceMetricsVisitor(string filePath) => _filePath = filePath;

    /// <summary>
    /// The classes the visit collected, in document order. Each access returns a fresh copy, so the
    /// returned list is always safe to hold or mutate. The caller is expected to read this only after
    /// the visit completed.
    /// </summary>
    public IReadOnlyList<ClassInfo> ClassInfos => _classInfos.ToArray();

    /// <inheritdoc/>
    public override void DefaultVisit(SyntaxNode node)
    {
        foreach (SyntaxNode child in node.ChildNodes())
        {
            Visit(child);
        }
    }

    /// <inheritdoc/>
    public override void VisitClassDeclaration(ClassDeclarationSyntax node)
    {
        _classInfos.Add(new ClassInfo(
            QualifiedNameOf(node),
            _filePath,
            MethodsOf(node),
            FieldsOf(node)));
        base.VisitClassDeclaration(node);
    }

    private static IReadOnlyList<MethodInfo> MethodsOf(ClassDeclarationSyntax node) =>
        node.Members
            .OfType<MethodDeclarationSyntax>()
            .Select(static member => new MethodInfo(member.Identifier.Text))
            .OrderBy(static method => method.Name, StringComparer.Ordinal)
            .ToArray();

    private static IReadOnlyList<FieldInfo> FieldsOf(ClassDeclarationSyntax node) =>
        node.Members
            .OfType<FieldDeclarationSyntax>()
            .SelectMany(static member => member.Declaration.Variables)
            .Select(static variable => new FieldInfo(variable.Identifier.Text))
            .OrderBy(static field => field.Name, StringComparer.Ordinal)
            .ToArray();

    /// <summary>
    /// The fully qualified name of a class declaration: the namespace and every enclosing type joined
    /// to the class's own name with dots. <c>namespace App.Models { public class Car { } }</c> yields
    /// <c>App.Models.Car</c>; a class nested in <c>Outer</c> yields <c>Outer.Nested</c>.
    /// </summary>
    private static string QualifiedNameOf(ClassDeclarationSyntax node)
    {
        var parts = new List<string>();
        for (SyntaxNode? current = node; current is not null; current = current.Parent)
        {
            switch (current)
            {
                case ClassDeclarationSyntax classDeclaration:
                    parts.Add(classDeclaration.Identifier.Text);
                    break;

                case RecordDeclarationSyntax recordDeclaration:
                    parts.Add(recordDeclaration.Identifier.Text);
                    break;

                case StructDeclarationSyntax structDeclaration:
                    parts.Add(structDeclaration.Identifier.Text);
                    break;

                case InterfaceDeclarationSyntax interfaceDeclaration:
                    parts.Add(interfaceDeclaration.Identifier.Text);
                    break;

                case BaseNamespaceDeclarationSyntax namespaceDeclaration:
                    parts.Add(namespaceDeclaration.Name.ToString());
                    break;
            }
        }

        parts.Reverse();
        return string.Join('.', parts);
    }
}
