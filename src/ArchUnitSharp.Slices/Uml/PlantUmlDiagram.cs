namespace ArchUnitSharp.Slices.Uml;

/// <summary>
/// An immutable component diagram parsed from PlantUML text: the <see cref="Components"/> the diagram
/// declares and the <see cref="Dependencies"/> it allows between them. A dependency is allowed between
/// two components when the diagram carries an arrow for it, decided by <see cref="Allows"/>.
/// </summary>
/// <remarks>
/// <para>
/// A component is any name the diagram declares — a <c>component [Name]</c> declaration or an endpoint
/// of an arrow — and a dependency is one <c>[source] -&gt; [target]</c> or
/// <c>[source] --&gt; [target]</c> arrow. Duplicate declarations and duplicate arrows collapse into one,
/// and the order of both lists is the order the text declares them in, so two parses of the same text
/// yield equal diagrams.
/// </para>
/// <para>
/// This type is immutable and safe for concurrent use. The lists it returns are fresh copies on every
/// call.
/// </para>
/// </remarks>
internal sealed class PlantUmlDiagram
{
    private readonly string[] _components;
    private readonly PlantUmlDependency[] _dependencies;
    private readonly HashSet<PlantUmlDependency> _dependencySet;

    /// <summary>
    /// The components the diagram declares: every <c>component [Name]</c> name and every arrow
    /// endpoint, deduplicated, in declaration order.
    /// </summary>
    internal IReadOnlyList<string> Components => _components.ToArray();

    /// <summary>
    /// The dependencies the diagram allows: one entry per distinct arrow, in declaration order.
    /// </summary>
    internal IReadOnlyList<PlantUmlDependency> Dependencies => _dependencies.ToArray();

    /// <summary>
    /// Creates a diagram from the declared components and allowed dependencies. The supplied sequences
    /// are copied, deduplicated and their order preserved; every dependency's endpoints are also
    /// components, declared or not.
    /// </summary>
    /// <param name="components">The declared component names. Must not be <see langword="null"/>; every name must be non-empty.</param>
    /// <param name="dependencies">The allowed dependencies. Must not be <see langword="null"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="components"/> or <paramref name="dependencies"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">A declared component name is empty.</exception>
    internal PlantUmlDiagram(IEnumerable<string> components, IEnumerable<PlantUmlDependency> dependencies)
    {
        ArgumentNullException.ThrowIfNull(components);
        ArgumentNullException.ThrowIfNull(dependencies);

        PlantUmlDependency[] dependencyArray = dependencies.ToArray();
        var names = new List<string>(components);
        foreach (PlantUmlDependency dependency in dependencyArray)
        {
            names.Add(dependency.Source);
            names.Add(dependency.Target);
        }

        _components = names
            .Select(RequireComponent)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        _dependencies = dependencyArray.Distinct().ToArray();
        _dependencySet = new HashSet<PlantUmlDependency>(_dependencies);
    }

    /// <summary>
    /// Returns <see langword="true"/> when the diagram allows a dependency from
    /// <paramref name="source"/> to <paramref name="target"/>: an arrow between the two names exists.
    /// </summary>
    /// <param name="source">The depending component's name. Must not be <see langword="null"/>.</param>
    /// <param name="target">The depended-on component's name. Must not be <see langword="null"/>.</param>
    /// <returns><see langword="true"/> when the diagram allows the dependency.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> or <paramref name="target"/> is <see langword="null"/>.</exception>
    internal bool Allows(string source, string target)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(target);
        return _dependencySet.Contains(new PlantUmlDependency(source, target));
    }

    private static string RequireComponent(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        if (name.Length == 0)
        {
            throw new ArgumentException("A diagram component must not be empty.", nameof(name));
        }

        return name;
    }
}
