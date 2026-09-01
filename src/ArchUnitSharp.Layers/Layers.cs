namespace ArchUnitSharp.Layers;

using ArchUnitSharp.Common.Extraction;

/// <summary>
/// The layers domain module's fluent surface: a named-layer policy over one project's
/// <see cref="Graph"/>. It is the ENTRY of a rule chain — built from the entry points
/// <c>Project.ProjectLayers()</c> / <c>Project.Layers()</c> — and the accumulator of the layer
/// declarations and rules that make up a policy. Declaring a layer is
/// <c>layer(name)</c> then <c>defined by</c> or <c>defined by folder</c>; asserting a rule is
/// <c>where layer(name)</c> then <c>may only depend on layers(...)</c> or
/// <c>may not depend on layers(...)</c>.
/// </summary>
/// <remarks>
/// <para>
/// A <see cref="Layers"/> value accumulates a set of named layers and a set of rules over them.
/// <see cref="Layer(string)"/> and <see cref="WhereLayer(string)"/> return the next word of the chain;
/// their completions return a new <see cref="Layers"/> with one more declaration or rule added, so a
/// policy is built by chaining and is checked as a whole with <see cref="Check(CheckOptions?)"/>. An
/// empty list of violations means every rule passed.
/// </para>
/// <para>
/// Every chaining method returns a new <see cref="Layers"/> instance and never mutates the one it was
/// called on, so a half-built policy can be stored in a variable and branched from without one branch
/// seeing another's declarations or rules. This type is immutable and safe for concurrent use.
/// </para>
/// </remarks>
public sealed class Layers : ICheckable
{
    private readonly Graph _graph;
    private readonly LayerDeclaration[] _declarations;
    private readonly LayerConstraint[] _constraints;

    /// <summary>
    /// Creates an empty policy over every file of <paramref name="graph"/>: no layers declared and no
    /// rules asserted. Checking it yields no violations.
    /// </summary>
    /// <param name="graph">The project's dependency graph. Must not be <see langword="null"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="graph"/> is <see langword="null"/>.</exception>
    public Layers(Graph graph)
    {
        ArgumentNullException.ThrowIfNull(graph);
        _graph = graph;
        _declarations = Array.Empty<LayerDeclaration>();
        _constraints = Array.Empty<LayerConstraint>();
    }

    private Layers(Graph graph, LayerDeclaration[] declarations, LayerConstraint[] constraints)
    {
        _graph = graph;
        _declarations = declarations;
        _constraints = constraints;
    }

    /// <summary>
    /// <c>layer(name)</c>: begins the declaration of a named layer. The declaration is completed with
    /// <see cref="LayerBuilder.DefinedBy"/> or <see cref="LayerBuilder.DefinedByFolder"/>. Returns a
    /// new <see cref="LayerBuilder"/>; the current policy is unchanged.
    /// </summary>
    /// <param name="name">The layer's name. Must not be <see langword="null"/> or empty.</param>
    /// <returns>A new <see cref="LayerBuilder"/> for the named layer.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is empty.</exception>
    public LayerBuilder Layer(string name) => new(this, name);

    /// <summary>
    /// <c>where layer(name)</c>: begins a rule over the named layer. The rule is completed with
    /// <see cref="LayerRule.MayOnlyDependOnLayers"/> or <see cref="LayerRule.MayNotDependOnLayers"/>.
    /// Returns a new <see cref="LayerRule"/>; the current policy is unchanged.
    /// </summary>
    /// <param name="name">The subject layer's name. Must not be <see langword="null"/> or empty.</param>
    /// <returns>A new <see cref="LayerRule"/> over the named layer.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is empty.</exception>
    public LayerRule WhereLayer(string name) => new(this, name);

    /// <summary>
    /// Checks every rule of this policy and returns the violations it found. An empty list means the
    /// policy passed.
    /// </summary>
    /// <param name="options">The options to check with; <see langword="null"/> means the defaults in <see cref="CheckOptions"/>.</param>
    /// <returns>The violations found; empty when the policy passed.</returns>
    public IReadOnlyList<Violation> Check(CheckOptions? options = null) =>
        CheckLogging.Run(options, logger => Assertion.LayersAssertion.Check(this, options, logger));

    /// <inheritdoc/>
    void ICheckable.ProhibitExternalImplementation()
    {
    }

    /// <summary>
    /// The project's dependency graph the policy reasons over.
    /// </summary>
    internal Graph Graph => _graph;

    /// <summary>
    /// The layer declarations, in the order they were added. Each access returns a fresh copy, so the
    /// returned list is always safe to hold or mutate.
    /// </summary>
    internal IReadOnlyList<LayerDeclaration> Declarations => _declarations.ToArray();

    /// <summary>
    /// The layer rules, in the order they were added. Each access returns a fresh copy, so the
    /// returned list is always safe to hold or mutate.
    /// </summary>
    internal IReadOnlyList<LayerConstraint> Constraints => _constraints.ToArray();

    internal Layers AddDeclaration(LayerDeclaration declaration)
    {
        ArgumentNullException.ThrowIfNull(declaration);

        var declarations = new LayerDeclaration[_declarations.Length + 1];
        Array.Copy(_declarations, declarations, _declarations.Length);
        declarations[_declarations.Length] = declaration;
        return new Layers(_graph, declarations, _constraints);
    }

    internal Layers AddConstraint(LayerConstraint constraint)
    {
        ArgumentNullException.ThrowIfNull(constraint);

        var constraints = new LayerConstraint[_constraints.Length + 1];
        Array.Copy(_constraints, constraints, _constraints.Length);
        constraints[_constraints.Length] = constraint;
        return new Layers(_graph, _declarations, constraints);
    }
}
