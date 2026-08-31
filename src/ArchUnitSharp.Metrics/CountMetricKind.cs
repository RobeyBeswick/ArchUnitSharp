namespace ArchUnitSharp.Metrics;

/// <summary>
/// Discriminates the count metrics the metrics module measures. Each kind belongs to one
/// <see cref="MetricSubject"/>: the class-level metrics — <see cref="MethodCount"/> and
/// <see cref="FieldCount"/> — measure one extracted <see cref="ClassInfo"/> each, and the file-level
/// metrics — <see cref="LinesOfCode"/>, <see cref="Statements"/>, <see cref="Imports"/>,
/// <see cref="Classes"/> and <see cref="Interfaces"/> — measure one extracted <see cref="FileInfo"/>
/// each. A <see cref="Metric"/> carries a kind, and the calculation layer turns a kind and a subject
/// into a value.
/// </summary>
/// <remarks>
/// <para>
/// The file-level metric vocabulary is the sibling implementations' count set: Ruby's and TypeScript's
/// count metrics name lines of code, statements, imports, classes and interfaces per file, plus methods
/// and fields per class. The <c>functions</c> file metric the siblings carry is deliberately absent:
/// C# has no concept of a file-level function distinct from a type member — every method belongs to a
/// class — so counting one would require inventing a meaning the language does not have. That is the
/// "skip the class-level metrics rather than faking them" rule applied to a metric C# cannot express.
/// </para>
/// </remarks>
public enum CountMetricKind
{
    /// <summary>
    /// The number of methods of one class: the class's members declared with <c>method</c> syntax.
    /// Constructors, destructors, operators, accessors and nested types are not methods.
    /// </summary>
    MethodCount,

    /// <summary>
    /// The number of fields of one class: every variable declared by the class's field declarations.
    /// A declaration like <c>int a, b;</c> declares two fields.
    /// </summary>
    FieldCount,

    /// <summary>
    /// The number of lines of one file that are not blank or whitespace only.
    /// </summary>
    LinesOfCode,

    /// <summary>
    /// The number of statements of one file: every statement in its syntax tree that is not itself a
    /// block. An <c>if</c>, a <c>return</c>, a declaration and a local function each count one; the
    /// blocks that group them do not.
    /// </summary>
    Statements,

    /// <summary>
    /// The number of import directives of one file: every <c>using</c> directive in its syntax tree,
    /// at the top level, inside a namespace, or declared <c>global</c>.
    /// </summary>
    Imports,

    /// <summary>
    /// The number of classes of one file: every <c>class</c> declaration in its syntax tree, nested
    /// declarations included. Records, structs, interfaces and enums are not classes.
    /// </summary>
    Classes,

    /// <summary>
    /// The number of interfaces of one file: every <c>interface</c> declaration in its syntax tree.
    /// </summary>
    Interfaces,
}
