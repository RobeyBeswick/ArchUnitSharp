namespace ArchUnitSharp.Layers;

using ArchUnitSharp.Common.Extraction;
using ArchUnitSharp.Layers.Assertion;

/// <summary>
/// The predicate and terminal of a layer rule chain begun by <see cref="Layers.WhereLayer"/>: the
/// subject layer, completed by <c>may only depend on layers(...)</c> and/or
/// <c>may not depend on layers(...)</c>, and checked with <see cref="Check(CheckOptions?)"/>.
/// </summary>
/// <remarks>
/// <para>
/// <c>may only depend on layers(...)</c> is the allowlist: the subject layer may depend on the named
/// layers and on itself (intra-layer dependencies are always allowed), and on nothing else. With no
/// arguments it means a sealed layer — the subject layer may depend on nothing outside itself.
/// <c>may not depend on layers(...)</c> is the blocklist: the subject layer may not depend on the
/// named layers. Both can be chained on one subject, and the check evaluates blocklists before
/// allowlists, so a dependency both permit and forbid is a violation of the blocklist. Edges where
/// either end belongs to no declared layer are ignored in both predicates.
/// </para>
/// <para>
/// Every method returns a new <see cref="LayerRule"/> instance and never mutates the one it was called
/// on, so a half-built rule can be stored in a variable and branched from without one branch seeing
/// another's constraints. This type is immutable and safe for concurrent use.
/// </para>
/// </remarks>
public sealed class LayerRule : ICheckable
{
    private readonly Graph _graph;
    private readonly Layer[] _layers;
    private readonly Layer _subject;
    private readonly LayerConstraint[] _constraints;

    /// <summary>
    /// Creates a rule over the subject layer with no constraints. Callers obtain a
    /// <see cref="LayerRule"/> from <see cref="Layers.WhereLayer"/> rather than constructing one.
    /// </summary>
    /// <param name="graph">The project's dependency graph.</param>
    /// <param name="layers">Every declared layer, the subject included.</param>
    /// <param name="subject">The subject layer.</param>
    internal LayerRule(Graph graph, IReadOnlyList<Layer> layers, Layer subject)
    {
        _graph = graph;
        _layers = layers.ToArray();
        _subject = subject;
        _constraints = Array.Empty<LayerConstraint>();
    }

    private LayerRule(
        Graph graph,
        Layer[] layers,
        Layer subject,
        LayerConstraint[] constraints)
    {
        _graph = graph;
        _layers = layers;
        _subject = subject;
        _constraints = constraints;
    }

    /// <summary>
    /// <c>may only depend on layers(...)</c>: the subject layer may depend only on the named layers
    /// and on itself. With no arguments the layer is sealed — it may depend on nothing outside itself.
    /// Returns a new <see cref="LayerRule"/>; the current rule is unchanged.
    /// </summary>
    /// <param name="layerNames">The names of the layers the subject may depend on. Each must be declared and must not be <see langword="null"/> or empty.</param>
    /// <returns>A new rule with the allowlist added.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="layerNames"/> or any of its elements is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Any element of <paramref name="layerNames"/> is empty.</exception>
    /// <exception cref="UserError">Any name in <paramref name="layerNames"/> is not a declared layer.</exception>
    public LayerRule MayOnlyDependOnLayers(params string[] layerNames) =>
        AddConstraint(LayerConstraintKind.AllowOnly, layerNames);

    /// <summary>
    /// <c>may not depend on layers(...)</c>: the subject layer may not depend on the named layers.
    /// Returns a new <see cref="LayerRule"/>; the current rule is unchanged.
    /// </summary>
    /// <param name="layerNames">The names of the layers the subject may not depend on. Each must be declared, must not be the subject layer itself, and must not be <see langword="null"/> or empty.</param>
    /// <returns>A new rule with the blocklist added.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="layerNames"/> or any of its elements is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Any element of <paramref name="layerNames"/> is empty.</exception>
    /// <exception cref="UserError">Any name in <paramref name="layerNames"/> is not a declared layer, or is the subject layer itself.</exception>
    public LayerRule MayNotDependOnLayers(params string[] layerNames) =>
        AddConstraint(LayerConstraintKind.Forbid, layerNames);

    /// <summary>
    /// Checks this rule and returns the violations it found: one <see cref="LayerViolation"/> per
    /// forbidden dependency. An empty list means the rule passed. The empty-test guard reports a rule
    /// whose subject layer matched no files or whose constraints are all vacuous.
    /// </summary>
    /// <param name="options">The options to check with; <see langword="null"/> means the defaults in <see cref="CheckOptions"/>.</param>
    /// <returns>The violations found; empty when the rule passed.</returns>
    /// <exception cref="UserError">This rule carries no constraint, so there is nothing to assert.</exception>
    public IReadOnlyList<Violation> Check(CheckOptions? options = null) =>
        LayersAssertion.Check(this, options);

    /// <inheritdoc/>
    void ICheckable.ProhibitExternalImplementation()
    {
    }

    /// <summary>
    /// The project's dependency graph this rule draws its files from.
    /// </summary>
    internal Graph Graph => _graph;

    /// <summary>
    /// Every declared layer, the subject included, in declaration order.
    /// </summary>
    internal IReadOnlyList<Layer> Layers => _layers;

    /// <summary>
    /// The subject layer the rule asserts over.
    /// </summary>
    internal Layer Subject => _subject;

    /// <summary>
    /// The rule's constraints, in the order they were applied.
    /// </summary>
    internal IReadOnlyList<LayerConstraint> Constraints => _constraints;

    /// <summary>
    /// Returns the layer declared under <paramref name="name"/>, or <see langword="null"/> when no
    /// such layer is declared.
    /// </summary>
    internal Layer? Find(string name) => _layers.FirstOrDefault(layer =>
        string.Equals(layer.Name, name, StringComparison.Ordinal));

    private LayerRule AddConstraint(LayerConstraintKind kind, string[] layerNames)
    {
        ArgumentNullException.ThrowIfNull(layerNames);

        var names = new string[layerNames.Length];
        for (int index = 0; index < layerNames.Length; index++)
        {
            string name = RequireLayerName(layerNames[index]);

            if (Find(name) is null)
            {
                throw new UserError(
                    $"Layer '{name}' is not declared. Declare it with Layer(name).DefinedBy(...) "
                    + "before referencing it in a layers(...) list.");
            }

            if (kind == LayerConstraintKind.Forbid
                && string.Equals(name, _subject.Name, StringComparison.Ordinal))
            {
                throw new UserError(
                    $"Layer '{name}' cannot be named in 'may not depend on layers(...)': "
                    + "intra-layer dependencies are always allowed.");
            }

            names[index] = name;
        }

        var constraints = new LayerConstraint[_constraints.Length + 1];
        Array.Copy(_constraints, constraints, _constraints.Length);
        constraints[_constraints.Length] = new LayerConstraint(kind, names);
        return new LayerRule(_graph, _layers, _subject, constraints);
    }

    private static string RequireLayerName(string name) =>
        name is null
            ? throw new ArgumentNullException(nameof(name))
            : name.Length == 0
                ? throw new ArgumentException("A layer name must not be empty.", nameof(name))
                : name;
}
