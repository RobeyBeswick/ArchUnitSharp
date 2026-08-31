namespace ArchUnitSharp.Metrics;

/// <summary>
/// One method of an extracted <see cref="ClassInfo"/>: the method's name, and nothing else. The name
/// is what lets the class-level count metrics — and, later, the cohesion metrics that build on this
/// extraction — refer to the method, and the class metric's method count is the number of these.
/// </summary>
/// <remarks>
/// <para>
/// This type is immutable and value-semantic; two method infos with the same name are equal.
/// </para>
/// </remarks>
public sealed record MethodInfo
{
    private readonly string _name;

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
    /// Creates a method info for the method named <paramref name="name"/>.
    /// </summary>
    /// <param name="name">The method's name; must not be <see langword="null"/> or empty.</param>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is empty.</exception>
    public MethodInfo(string name) => _name = Require(name, nameof(Name));

    private static string Require(string value, string propertyName) =>
        value is null
            ? throw new ArgumentNullException(propertyName)
            : value.Length == 0
                ? throw new ArgumentException($"{propertyName} must not be empty.", propertyName)
                : value;
}
