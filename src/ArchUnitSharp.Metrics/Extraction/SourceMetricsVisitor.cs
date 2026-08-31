namespace ArchUnitSharp.Metrics.Extraction;

using System.Collections.Generic;
using ArchUnitSharp.Metrics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

/// <summary>
/// The syntax walk that extracts the <see cref="ClassInfo"/> values of one C# file: one info per
/// <c>class</c> declaration, nested declarations included, each carrying its fully qualified name, its
/// direct <c>method</c> and field declarations, and — for each method, the fields it accesses, and for
/// each field, the methods that access it. It is the class half of the metrics extraction; the
/// file-level counts (<c>lines of code</c>, <c>statements</c>, <c>imports</c> and <c>interfaces</c>)
/// are computed by <see cref="MetricsExtractor"/> from the same syntax tree the visitor walks.
/// </summary>
/// <remarks>
/// <para>
/// The visitor descends the whole compilation unit, so a nested class declaration inside a class,
/// record, struct or interface is visited and becomes its own <see cref="ClassInfo"/> — its methods
/// and fields are its own, not its enclosing type's. A class's methods are its direct
/// <c>method</c> declarations (constructors, destructors, operators and accessors are not methods)
/// and its fields are one per variable its field declarations name. A method's accessed fields are
/// the class's field names that appear among the identifiers of the method's body or expression body;
/// a field's accessing methods are the methods whose accessed fields include it, so the two facts are
/// the same relationship read in either direction. Each info's <see cref="ClassInfo.Name"/>
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
        IReadOnlyList<MethodInfo> methods = MethodsOf(node);
        _classInfos.Add(new ClassInfo(
            QualifiedNameOf(node),
            _filePath,
            methods,
            FieldsOf(node, methods)));
        base.VisitClassDeclaration(node);
    }

    private static IReadOnlyList<MethodInfo> MethodsOf(ClassDeclarationSyntax node)
    {
        HashSet<string> fieldNames = FieldNamesOf(node);
        return node.Members
            .OfType<MethodDeclarationSyntax>()
            .Select(member => new MethodInfo(
                member.Identifier.Text,
                AccessedFieldsOf(member, fieldNames)))
            .OrderBy(static method => method.Name, StringComparer.Ordinal)
            .ToArray();
    }

    private static HashSet<string> FieldNamesOf(ClassDeclarationSyntax node) =>
        new(
            node.Members
                .OfType<FieldDeclarationSyntax>()
                .SelectMany(static member => member.Declaration.Variables)
                .Select(static variable => variable.Identifier.Text),
            StringComparer.Ordinal);

    /// <summary>
    /// The fields of a method's class that the method accesses: the class's field names that appear
    /// among the identifiers of the method's body or expression body, each once — the
    /// <see cref="MethodInfo"/> constructor sorts and deduplicates. An identifier that merely shares a
    /// field's name — a local variable, parameter or another object's member — is counted, the
    /// deliberate approximation of a textual match.
    /// </summary>
    private static IReadOnlyList<string> AccessedFieldsOf(
        MethodDeclarationSyntax method,
        HashSet<string> fieldNames)
    {
        IEnumerable<string> identifiers = method.Body is not null
            ? method.Body.DescendantNodes()
                .OfType<IdentifierNameSyntax>()
                .Select(static identifier => identifier.Identifier.Text)
            : Array.Empty<string>();
        if (method.ExpressionBody is not null)
        {
            identifiers = identifiers.Concat(
                method.ExpressionBody.DescendantNodes()
                    .OfType<IdentifierNameSyntax>()
                    .Select(static identifier => identifier.Identifier.Text));
        }

        return identifiers
            .Where(fieldNames.Contains)
            .ToArray();
    }

    private static IReadOnlyList<FieldInfo> FieldsOf(
        ClassDeclarationSyntax node,
        IReadOnlyList<MethodInfo> methods) =>
        node.Members
            .OfType<FieldDeclarationSyntax>()
            .SelectMany(static member => member.Declaration.Variables)
            .Select(variable => new FieldInfo(
                variable.Identifier.Text,
                methods
                    .Where(method => method.AccessedFields.Contains(variable.Identifier.Text, StringComparer.Ordinal))
                    .Select(static method => method.Name)
                    .ToArray()))
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
