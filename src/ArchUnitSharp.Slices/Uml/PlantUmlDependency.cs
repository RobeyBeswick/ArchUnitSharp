namespace ArchUnitSharp.Slices.Uml;

/// <summary>
/// One directed dependency between two components of a <see cref="PlantUmlDiagram"/>: the source
/// component depends on the target component. The atom of the diagram, exactly as an
/// <c>[source] -&gt; [target]</c> or <c>[source] --&gt; [target]</c> arrow declares it.
/// </summary>
/// <remarks>
/// <para>
/// Both names are the components' names as the diagram writes them — the text inside the square
/// brackets, trimmed — and both are non-empty. A dependency's endpoints are also components of the
/// diagram, even when no <c>component [Name]</c> declaration names them, because an arrow declares
/// its endpoints.
/// </para>
/// <para>
/// This type is immutable and value-semantic: two dependencies with the same source and target are
/// equal, which is what lets a <see cref="PlantUmlDiagram"/> compare an actual dependency against its
/// allowed set.
/// </para>
/// </remarks>
internal sealed record PlantUmlDependency
{
    private readonly string _source;
    private readonly string _target;

    /// <summary>
    /// The depending component's name.
    /// </summary>
    internal string Source
    {
        get => _source;
        init => _source = Require(value, nameof(Source));
    }

    /// <summary>
    /// The depended-on component's name.
    /// </summary>
    internal string Target
    {
        get => _target;
        init => _target = Require(value, nameof(Target));
    }

    /// <summary>
    /// Creates a diagram dependency between two components.
    /// </summary>
    /// <param name="source">The depending component's name. Must not be <see langword="null"/> or empty.</param>
    /// <param name="target">The depended-on component's name. Must not be <see langword="null"/> or empty.</param>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> or <paramref name="target"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="source"/> or <paramref name="target"/> is empty.</exception>
    internal PlantUmlDependency(string source, string target)
    {
        _source = Require(source, nameof(Source));
        _target = Require(target, nameof(Target));
    }

    private static string Require(string value, string parameterName) =>
        value is null
            ? throw new ArgumentNullException(parameterName)
            : value.Length == 0
                ? throw new ArgumentException($"{parameterName} must not be empty.", parameterName)
                : value;
}
