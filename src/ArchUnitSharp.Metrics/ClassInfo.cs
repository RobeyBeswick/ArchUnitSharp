namespace ArchUnitSharp.Metrics;

/// <summary>
/// Static information about one class declaration of an extracted file: its fully qualified name, the
/// file it is declared in, and its methods and fields — each method carrying the fields it accesses
/// and each field the methods that access it. The class-level count metrics — <c>method count</c> and
/// <c>field count</c> — and the LCOM cohesion metrics measure these; the <c>for classes matching</c>
/// selector narrows a rule's subjects to the classes whose <see cref="Name"/> matches.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Name"/> is the class's fully qualified name with dots: the namespace and any enclosing
/// types joined to the class's own name, so <c>namespace App.Models { public class Car { } }</c>
/// yields <c>App.Models.Car</c> and a nested class <c>Outer.Nested</c> yields <c>Outer.Nested</c>. It
/// is the name a <c>for classes matching</c> glob matches against. <see cref="Identifier"/> is
/// <c>file path:name</c> — the value that renders a report line, matches a <see cref="MetricViolation"/>
/// to a class, and orders the class subjects of a rule.
/// </para>
/// <para>
/// <see cref="Methods"/> are the class's <c>method</c> declarations; constructors, destructors,
/// operators and accessors are not methods, and the methods of a nested class belong to the nested
/// class's own info. <see cref="Fields"/> are every variable of the class's field declarations, one
/// info per variable.
/// </para>
/// <para>
/// This type is immutable and value-semantic; two class infos with the same name, file path, methods
/// and fields are equal.
/// </para>
/// </remarks>
public sealed record ClassInfo
{
    private readonly string _name;
    private readonly string _filePath;
    private readonly IReadOnlyList<MethodInfo> _methods;
    private readonly IReadOnlyList<FieldInfo> _fields;

    /// <summary>
    /// The class's fully qualified name with dots, namespace and enclosing types included. Must not be
    /// <see langword="null"/> or empty; both the constructor and a <see langword="with"/> expression
    /// route through the same validation, so neither can introduce a bad value.
    /// </summary>
    public string Name
    {
        get => _name;
        init => _name = Require(value, nameof(Name));
    }

    /// <summary>
    /// The file the class is declared in, as the graph's project-relative identifier. Must not be
    /// <see langword="null"/> or empty; both the constructor and a <see langword="with"/> expression
    /// route through the same validation, so neither can introduce a bad value.
    /// </summary>
    public string FilePath
    {
        get => _filePath;
        init => _filePath = Require(value, nameof(FilePath));
    }

    /// <summary>
    /// The class's methods, one info per <c>method</c> declaration, sorted by name. Each access
    /// returns a fresh copy, so the returned list is always safe to hold or mutate.
    /// </summary>
    public IReadOnlyList<MethodInfo> Methods
    {
        get => _methods.ToArray();
        init => _methods = Copy(value, nameof(Methods));
    }

    /// <summary>
    /// The class's fields, one info per declared variable, sorted by name. Each access returns a fresh
    /// copy, so the returned list is always safe to hold or mutate.
    /// </summary>
    public IReadOnlyList<FieldInfo> Fields
    {
        get => _fields.ToArray();
        init => _fields = Copy(value, nameof(Fields));
    }

    /// <summary>
    /// Creates a class info for a class declaration.
    /// </summary>
    /// <param name="name">The class's fully qualified name; must not be <see langword="null"/> or empty.</param>
    /// <param name="filePath">The file the class is declared in; must not be <see langword="null"/> or empty.</param>
    /// <param name="methods">The class's methods; must not be <see langword="null"/>.</param>
    /// <param name="fields">The class's fields; must not be <see langword="null"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="name"/>, <paramref name="filePath"/>, <paramref name="methods"/> or <paramref name="fields"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> or <paramref name="filePath"/> is empty.</exception>
    public ClassInfo(
        string name,
        string filePath,
        IReadOnlyList<MethodInfo> methods,
        IReadOnlyList<FieldInfo> fields)
    {
        _name = Require(name, nameof(Name));
        _filePath = Require(filePath, nameof(FilePath));
        _methods = Copy(methods, nameof(Methods));
        _fields = Copy(fields, nameof(Fields));
    }

    /// <summary>
    /// The value that identifies this class across files and classes: <c>file path:name</c>. It is the
    /// subject of a class-level metric's <see cref="MetricViolation"/>, what orders the class subjects
    /// of a rule, and what the <c>for classes matching</c> selector's narrowed set is sorted by.
    /// </summary>
    public string Identifier => $"{FilePath}:{Name}";

    /// <summary>
    /// Two class infos are equal when their names, file paths, methods and fields are equal, compared
    /// by value.
    /// </summary>
    /// <param name="other">The other class info.</param>
    /// <returns><see langword="true"/> when all four are equal.</returns>
    public bool Equals(ClassInfo? other) =>
        other is not null
        && string.Equals(Name, other.Name, StringComparison.Ordinal)
        && string.Equals(FilePath, other.FilePath, StringComparison.Ordinal)
        && Methods.SequenceEqual(other.Methods)
        && Fields.SequenceEqual(other.Fields);

    /// <summary>
    /// A hash code consistent with <see cref="Equals(ClassInfo?)"/>, computed over the name, file path
    /// and every method and field.
    /// </summary>
    /// <returns>A hash code over the class info's facts.</returns>
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Name);
        hash.Add(FilePath);
        foreach (MethodInfo method in Methods)
        {
            hash.Add(method);
        }

        foreach (FieldInfo field in Fields)
        {
            hash.Add(field);
        }

        return hash.ToHashCode();
    }

    private static IReadOnlyList<T> Copy<T>(IReadOnlyList<T> values, string propertyName)
    {
        ArgumentNullException.ThrowIfNull(values);
        return values.ToArray();
    }

    private static string Require(string value, string propertyName) =>
        value is null
            ? throw new ArgumentNullException(propertyName)
            : value.Length == 0
                ? throw new ArgumentException($"{propertyName} must not be empty.", propertyName)
                : value;
}
