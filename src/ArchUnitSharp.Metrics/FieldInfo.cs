namespace ArchUnitSharp.Metrics;

/// <summary>
/// One field of an extracted <see cref="ClassInfo"/>: the field's name, and nothing else. The name is
/// what lets the class-level count metrics — and, later, the cohesion metrics that build on this
/// extraction — refer to the field, and the class metric's field count is the number of these.
/// </summary>
/// <remarks>
/// <para>
/// A field declaration that names several variables contributes one <see cref="FieldInfo"/> per
/// variable: <c>int a, b;</c> yields the fields <c>a</c> and <c>b</c>.
/// </para>
/// <para>
/// This type is immutable and value-semantic; two field infos with the same name are equal.
/// </para>
/// </remarks>
public sealed record FieldInfo
{
    private readonly string _name;

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
    /// Creates a field info for the field named <paramref name="name"/>.
    /// </summary>
    /// <param name="name">The field's name; must not be <see langword="null"/> or empty.</param>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is empty.</exception>
    public FieldInfo(string name) => _name = Require(name, nameof(Name));

    private static string Require(string value, string propertyName) =>
        value is null
            ? throw new ArgumentNullException(propertyName)
            : value.Length == 0
                ? throw new ArgumentException($"{propertyName} must not be empty.", propertyName)
                : value;
}
