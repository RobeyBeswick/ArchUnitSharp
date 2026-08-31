namespace ArchUnitSharp.Metrics;

/// <summary>
/// One field of an extracted <see cref="ClassInfo"/>: the field's name and the methods that access it.
/// The name is what lets the class-level count metrics — and the cohesion metrics — refer to the
/// field; the accessing methods are what the cohesion metrics' field-sharing between methods is built
/// on.
/// </summary>
/// <remarks>
/// <para>
/// A field declaration that names several variables contributes one <see cref="FieldInfo"/> per
/// variable: <c>int a, b;</c> yields the fields <c>a</c> and <c>b</c>. <see cref="AccessedBy"/> names
/// the class's methods that read or write the field, derived from the methods' own accessed-field
/// facts: a method that accesses the field appears exactly once, and the list is sorted ordinally. A
/// field no method accesses has an empty list.
/// </para>
/// <para>
/// This type is immutable and value-semantic; two field infos with the same name and accessing methods
/// are equal.
/// </para>
/// </remarks>
public sealed record FieldInfo
{
    private readonly string _name;
    private readonly IReadOnlyList<string> _accessedBy;

    /// <summary>
    /// The field's name, as written in its declaration — <c>_count</c> for <c>private int _count;</c>.
    /// Must not be <see langword="null"/> or empty; both the constructor and a <see langword="with"/>
    /// expression route through the same validation, so neither can introduce a bad value.
    /// </summary>
    public string Name
    {
        get => _name;
        init => _name = Require(value, nameof(Name));
    }

    /// <summary>
    /// The methods of the field's class that access this field, sorted ordinally and free of
    /// duplicates. Each access returns a fresh copy, so the returned list is always safe to hold or
    /// mutate.
    /// </summary>
    public IReadOnlyList<string> AccessedBy
    {
        get => _accessedBy.ToArray();
        init => _accessedBy = Normalize(value, nameof(AccessedBy));
    }

    /// <summary>
    /// Creates a field info for the field named <paramref name="name"/> that no method accesses.
    /// </summary>
    /// <param name="name">The field's name; must not be <see langword="null"/> or empty.</param>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is empty.</exception>
    public FieldInfo(string name)
        : this(name, Array.Empty<string>())
    {
    }

    /// <summary>
    /// Creates a field info for the field named <paramref name="name"/> that the given methods access.
    /// The accessing methods are copied, deduplicated and sorted ordinally, so a caller's list is never
    /// held and a duplicate or unsorted input yields the same info as the sorted unique one.
    /// </summary>
    /// <param name="name">The field's name; must not be <see langword="null"/> or empty.</param>
    /// <param name="accessedBy">The methods that access the field; must not be <see langword="null"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> or <paramref name="accessedBy"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is empty.</exception>
    public FieldInfo(string name, IReadOnlyList<string> accessedBy)
    {
        _name = Require(name, nameof(Name));
        _accessedBy = Normalize(accessedBy, nameof(AccessedBy));
    }

    /// <summary>
    /// Two field infos are equal when their names and accessing methods are equal, the accessing
    /// methods compared element by element and ordinally.
    /// </summary>
    /// <param name="other">The other field info.</param>
    /// <returns><see langword="true"/> when both are equal.</returns>
    public bool Equals(FieldInfo? other) =>
        other is not null
        && string.Equals(Name, other.Name, StringComparison.Ordinal)
        && AccessedBy.SequenceEqual(other.AccessedBy, StringComparer.Ordinal);

    /// <summary>
    /// A hash code consistent with <see cref="Equals(FieldInfo?)"/>, computed over the name and every
    /// accessing method.
    /// </summary>
    /// <returns>A hash code over the field info's facts.</returns>
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Name);
        foreach (string method in AccessedBy)
        {
            hash.Add(method);
        }

        return hash.ToHashCode();
    }

    private static IReadOnlyList<string> Normalize(IReadOnlyList<string> values, string propertyName)
    {
        ArgumentNullException.ThrowIfNull(values);
        return values
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static field => field, StringComparer.Ordinal)
            .ToArray();
    }

    private static string Require(string value, string propertyName) =>
        value is null
            ? throw new ArgumentNullException(propertyName)
            : value.Length == 0
                ? throw new ArgumentException($"{propertyName} must not be empty.", propertyName)
                : value;
}
