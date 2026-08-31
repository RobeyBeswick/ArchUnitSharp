namespace ArchUnitSharp.Metrics;

/// <summary>
/// One method of an extracted <see cref="ClassInfo"/>: the method's name and the fields it accesses.
/// The name is what lets the class-level count metrics — and the cohesion metrics — refer to the
/// method; the accessed fields are what the cohesion metrics' field-sharing between methods is built
/// on.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="AccessedFields"/> names the fields of the method's own class that the method reads or
/// writes, in its body or expression body: the class's fields whose names appear among the method's
/// identifiers, so <c>_speed = 0;</c> and <c>return this._speed;</c> both access <c>_speed</c>. Each
/// name appears once and the list is sorted ordinally. A method that accesses no field has an empty
/// list. An identifier that merely shares a field's name — a local variable, parameter or another
/// object's member — is counted, which is the deliberate approximation of a textual match.
/// </para>
/// <para>
/// This type is immutable and value-semantic; two method infos with the same name and accessed fields
/// are equal.
/// </para>
/// </remarks>
public sealed record MethodInfo
{
    private readonly string _name;
    private readonly IReadOnlyList<string> _accessedFields;

    /// <summary>
    /// The method's name, as written in its declaration — <c>Run</c> for <c>public void Run()</c>.
    /// Must not be <see langword="null"/> or empty; both the constructor and a <see langword="with"/>
    /// expression route through the same validation, so neither can introduce a bad value.
    /// </summary>
    public string Name
    {
        get => _name;
        init => _name = Require(value, nameof(Name));
    }

    /// <summary>
    /// The fields of the method's class that this method accesses, sorted ordinally and free of
    /// duplicates. Each access returns a fresh copy, so the returned list is always safe to hold or
    /// mutate.
    /// </summary>
    public IReadOnlyList<string> AccessedFields
    {
        get => _accessedFields.ToArray();
        init => _accessedFields = Normalize(value, nameof(AccessedFields));
    }

    /// <summary>
    /// Creates a method info for the method named <paramref name="name"/> that accesses no field.
    /// </summary>
    /// <param name="name">The method's name; must not be <see langword="null"/> or empty.</param>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is empty.</exception>
    public MethodInfo(string name)
        : this(name, Array.Empty<string>())
    {
    }

    /// <summary>
    /// Creates a method info for the method named <paramref name="name"/> that accesses the given
    /// fields. The accessed fields are copied, deduplicated and sorted ordinally, so a caller's list is
    /// never held and a duplicate or unsorted input yields the same info as the sorted unique one.
    /// </summary>
    /// <param name="name">The method's name; must not be <see langword="null"/> or empty.</param>
    /// <param name="accessedFields">The fields the method accesses; must not be <see langword="null"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> or <paramref name="accessedFields"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is empty.</exception>
    public MethodInfo(string name, IReadOnlyList<string> accessedFields)
    {
        _name = Require(name, nameof(Name));
        _accessedFields = Normalize(accessedFields, nameof(AccessedFields));
    }

    /// <summary>
    /// Two method infos are equal when their names and accessed fields are equal, the accessed fields
    /// compared element by element and ordinally.
    /// </summary>
    /// <param name="other">The other method info.</param>
    /// <returns><see langword="true"/> when both are equal.</returns>
    public bool Equals(MethodInfo? other) =>
        other is not null
        && string.Equals(Name, other.Name, StringComparison.Ordinal)
        && AccessedFields.SequenceEqual(other.AccessedFields, StringComparer.Ordinal);

    /// <summary>
    /// A hash code consistent with <see cref="Equals(MethodInfo?)"/>, computed over the name and every
    /// accessed field.
    /// </summary>
    /// <returns>A hash code over the method info's facts.</returns>
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Name);
        foreach (string field in AccessedFields)
        {
            hash.Add(field);
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
