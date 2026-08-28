namespace ArchUnitSharp.Layers;

using ArchUnitSharp.Common.Extraction;

/// <summary>
/// The layers domain module's fluent surface: the named-layer policy builder over one project's
/// <see cref="Graph"/>. It is the ENTRY and SCOPE of a layer rule chain — built from the entry points
/// <c>Project.ProjectLayers()</c> / <c>Project.Layers()</c>, extended by the declarations
/// <c>layer(name)</c> then <c>defined by</c> or <c>defined by folder</c>, and handed to the subject
/// selection <see cref="WhereLayer"/>.
/// </summary>
/// <remarks>
/// <para>
/// A <see cref="Layers"/> value carries the layers declared so far — none before the first
/// declaration, then the accumulated declarations — and <see cref="WhereLayer"/> selects a declared
/// layer as the subject of a rule, which the <see cref="LayerRule"/> completes.
/// </para>
/// <para>
/// Every method returns a new instance and never mutates the one it was called on, so a half-built
/// policy can be stored in a variable and branched from without one branch seeing another's layers.
/// This type is immutable and safe for concurrent use.
/// </para>
/// </remarks>
public sealed class Layers
{
    private readonly Graph _graph;
    private readonly Layer[] _layers;

    /// <summary>
    /// Creates a layer policy builder over every file of <paramref name="graph"/>, with no layers
    /// declared yet.
    /// </summary>
    /// <param name="graph">The project's dependency graph. Must not be <see langword="null"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="graph"/> is <see langword="null"/>.</exception>
    public Layers(Graph graph)
    {
        ArgumentNullException.ThrowIfNull(graph);
        _graph = graph;
        _layers = Array.Empty<Layer>();
    }

    private Layers(Graph graph, Layer[] layers)
    {
        _graph = graph;
        _layers = layers;
    }

    /// <summary>
    /// <c>layer(name)</c>: begins a layer declaration. The returned definition completes the
    /// declaration with <c>defined by</c> or <c>defined by folder</c> and hands the policy back with
    /// the layer added. Returns a new <see cref="LayerDefinition"/>; the current policy is unchanged.
    /// </summary>
    /// <param name="name">The layer's name, as later rules reference it. Must not be <see langword="null"/> or empty.</param>
    /// <returns>A definition that completes the layer's declaration.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is empty.</exception>
    public LayerDefinition Layer(string name) => new(this, RequireName(name));

    /// <summary>
    /// <c>where layer(name)</c>: selects the declared layer <paramref name="name"/> as the subject of
    /// a rule. The returned <see cref="LayerRule"/> completes the rule with
    /// <c>may only depend on layers(...)</c> or <c>may not depend on layers(...)</c>. Returns a new
    /// <see cref="LayerRule"/>; the current policy is unchanged.
    /// </summary>
    /// <param name="name">The subject layer's declared name. Must not be <see langword="null"/> or empty.</param>
    /// <returns>A rule over the selected layer.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is empty.</exception>
    /// <exception cref="UserError"><paramref name="name"/> is not a declared layer.</exception>
    public LayerRule WhereLayer(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        if (name.Length == 0)
        {
            throw new ArgumentException("A layer name must not be empty.", nameof(name));
        }

        Layer layer = Find(name)
            ?? throw new UserError(
                $"Layer '{name}' is not declared. Declare it with Layer(name).DefinedBy(...) "
                + "before selecting it with WhereLayer(...).");
        return new LayerRule(_graph, _layers, layer);
    }

    /// <summary>
    /// The project's dependency graph this policy draws its files from. Internal: the projections and
    /// assertions read it.
    /// </summary>
    internal Graph Graph => _graph;

    /// <summary>
    /// The layers declared so far, in declaration order. Internal: the subject selection and the rule
    /// carry them to the assertion.
    /// </summary>
    internal IReadOnlyList<Layer> DeclaredLayers => _layers;

    /// <summary>
    /// Returns the layer declared under <paramref name="name"/>, or <see langword="null"/> when no
    /// such layer is declared.
    /// </summary>
    internal Layer? Find(string name) => _layers.FirstOrDefault(layer =>
        string.Equals(layer.Name, name, StringComparison.Ordinal));

    /// <summary>
    /// Adds a layer declaration to this policy and returns the new policy. A name may be declared only
    /// once, so re-declaring a name is a <see cref="UserError"/>.
    /// </summary>
    internal Layers Add(Layer layer)
    {
        if (Find(layer.Name) is not null)
        {
            throw new UserError($"Layer '{layer.Name}' is already declared.");
        }

        var layers = new Layer[_layers.Length + 1];
        Array.Copy(_layers, layers, _layers.Length);
        layers[_layers.Length] = layer;
        return new Layers(_graph, layers);
    }

    private static string RequireName(string name) =>
        name is null
            ? throw new ArgumentNullException(nameof(name))
            : name.Length == 0
                ? throw new ArgumentException("A layer name must not be empty.", nameof(name))
                : name;
}
