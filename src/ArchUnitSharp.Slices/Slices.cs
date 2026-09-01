namespace ArchUnitSharp.Slices;

using ArchUnitSharp.Common.Extraction;
using ArchUnitSharp.Projection;
using ArchUnitSharp.Slices.Projection;
using ArchUnitSharp.Slices.Uml;

/// <summary>
/// The slices domain module's fluent surface: a slicing policy over one project's <see cref="Graph"/>.
/// It is the ENTRY of a rule chain — built from the entry points <c>Project.ProjectSlices()</c> /
/// <c>Project.Slices()</c> — and the accumulator of the slice definitions and rules that make up a
/// policy. Defining a slice is <c>defined by(pattern)</c> or <c>defined by regex(pattern)</c>;
/// asserting a rule is <c>should (not) contain dependency(from, to)</c> or
/// <c>should adhere to diagram(text)</c>; generating a report is <c>to plantuml()</c> /
/// <c>export as plantuml(path)</c>.
/// </summary>
/// <remarks>
/// <para>
/// A <see cref="Slices"/> value accumulates a set of slice definitions and a set of rules over the
/// slicing they produce. <see cref="DefinedBy"/> and <see cref="DefinedByRegex"/> return the next word
/// of the chain; <see cref="Should"/> and <see cref="ShouldNot"/> return the mood whose completion adds
/// a rule; checking is a whole-policy operation, <see cref="Check(CheckOptions?)"/>. An empty list of
/// violations means every rule passed, and a policy with no rules passes.
/// </para>
/// <para>
/// A slice definition assigns each file of the graph to a slice — the text a <c>(**)</c> capture in a
/// <c>defined by</c> glob, or a <c>defined by regex</c> pattern's first group, captures — or to no
/// slice. A file belongs to the slice the first definition that matches it names; a file no definition
/// names is unsliced and outside every rule's scope. A definition whose pattern contains no capture
/// cannot name a slice and raises a <see cref="UserError"/>.
/// </para>
/// <para>
/// <c>ToPlantUml()</c> and <c>ExportAsPlantUml(path)</c> are report terminals: they render the
/// slicing's projected dependency graph as a PlantUML component diagram, unguarded like every report,
/// so an empty slicing renders a valid empty document rather than a violation.
/// </para>
/// <para>
/// Every chaining method returns a new <see cref="Slices"/> instance and never mutates the one it was
/// called on, so a half-built policy can be stored in a variable and branched from without one branch
/// seeing another's definitions or rules. This type is immutable and safe for concurrent use.
/// </para>
/// </remarks>
public sealed class Slices : ICheckable
{
    private readonly Graph _graph;
    private readonly SliceDefinition[] _definitions;
    private readonly SliceRule[] _rules;
    private readonly DiagramRule[] _diagramRules;

    /// <summary>
    /// Creates an empty policy over every file of <paramref name="graph"/>: no slices defined and no
    /// rules asserted. Checking it yields no violations.
    /// </summary>
    /// <param name="graph">The project's dependency graph. Must not be <see langword="null"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="graph"/> is <see langword="null"/>.</exception>
    public Slices(Graph graph)
    {
        ArgumentNullException.ThrowIfNull(graph);
        _graph = graph;
        _definitions = Array.Empty<SliceDefinition>();
        _rules = Array.Empty<SliceRule>();
        _diagramRules = Array.Empty<DiagramRule>();
    }

    private Slices(Graph graph, SliceDefinition[] definitions, SliceRule[] rules, DiagramRule[] diagramRules)
    {
        _graph = graph;
        _definitions = definitions;
        _rules = rules;
        _diagramRules = diagramRules;
    }

    /// <summary>
    /// <c>defined by(pattern)</c>: adds a slice definition from a glob with a <c>(**)</c> capture —
    /// a file whose whole path matches is assigned to the slice named by the captured text. Returns a
    /// new <see cref="Slices"/> with the definition added; the current policy is unchanged.
    /// </summary>
    /// <param name="glob">The glob to match each file's whole path against. Must not be <see langword="null"/> or empty, and must contain a <c>(**)</c> capture.</param>
    /// <returns>A new policy with the definition added.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="glob"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="glob"/> is empty.</exception>
    /// <exception cref="UserError"><paramref name="glob"/> contains no <c>(**)</c> capture, so it cannot name a slice.</exception>
    public Slices DefinedBy(string glob) => AddDefinition(SliceDefinition.ByPattern(glob));

    /// <summary>
    /// <c>defined by regex(pattern)</c>: adds a slice definition from a regex pattern with a capture
    /// group — a file whose whole path matches the anchored pattern is assigned to the slice named by
    /// the first group's captured text. Returns a new <see cref="Slices"/> with the definition added;
    /// the current policy is unchanged.
    /// </summary>
    /// <param name="pattern">The regex to match each file's whole path against. Must not be <see langword="null"/> or empty, and must contain a capture group.</param>
    /// <returns>A new policy with the definition added.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="pattern"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="pattern"/> is empty.</exception>
    /// <exception cref="UserError"><paramref name="pattern"/> contains no capture group, so it cannot name a slice.</exception>
    public Slices DefinedByRegex(string pattern) => AddDefinition(SliceDefinition.ByRegex(pattern));

    /// <summary>
    /// <c>should</c>: begins a rule over this slicing with the positive mood. Returns a new
    /// <see cref="Should"/>; the current policy is unchanged.
    /// </summary>
    /// <returns>A new <see cref="Should"/> over this policy.</returns>
    public Should Should() => new(this);

    /// <summary>
    /// <c>should not</c>: begins a rule over this slicing with the negated mood. Returns a new
    /// <see cref="ShouldNot"/>; the current policy is unchanged.
    /// </summary>
    /// <returns>A new <see cref="ShouldNot"/> over this policy.</returns>
    public ShouldNot ShouldNot() => new(this);

    /// <summary>
    /// Checks every rule of this policy and returns the violations it found. An empty list means the
    /// policy passed.
    /// </summary>
    /// <param name="options">The options to check with; <see langword="null"/> means the defaults in <see cref="CheckOptions"/>.</param>
    /// <returns>The violations found; empty when the policy passed.</returns>
    public IReadOnlyList<Violation> Check(CheckOptions? options = null) =>
        CheckLogging.Run(options, logger => Assertion.SlicesAssertion.Check(this, options, logger));

    /// <inheritdoc/>
    void ICheckable.ProhibitExternalImplementation()
    {
    }

    /// <summary>
    /// The project's dependency graph the policy reasons over.
    /// </summary>
    internal Graph Graph => _graph;

    /// <summary>
    /// The slice definitions, in the order they were added. Each access returns a fresh copy, so the
    /// returned list is always safe to hold or mutate.
    /// </summary>
    internal IReadOnlyList<SliceDefinition> Definitions => _definitions.ToArray();

    /// <summary>
    /// The slice rules, in the order they were added. Each access returns a fresh copy, so the
    /// returned list is always safe to hold or mutate.
    /// </summary>
    internal IReadOnlyList<SliceRule> Rules => _rules.ToArray();

    /// <summary>
    /// The diagram rules, in the order they were added. Each access returns a fresh copy, so the
    /// returned list is always safe to hold or mutate.
    /// </summary>
    internal IReadOnlyList<DiagramRule> DiagramRules => _diagramRules.ToArray();

    internal Slices AddDefinition(SliceDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        var definitions = new SliceDefinition[_definitions.Length + 1];
        Array.Copy(_definitions, definitions, _definitions.Length);
        definitions[_definitions.Length] = definition;
        return new Slices(_graph, definitions, _rules, _diagramRules);
    }

    internal Slices AddRule(SliceRule rule)
    {
        ArgumentNullException.ThrowIfNull(rule);

        var rules = new SliceRule[_rules.Length + 1];
        Array.Copy(_rules, rules, _rules.Length);
        rules[_rules.Length] = rule;
        return new Slices(_graph, _definitions, rules, _diagramRules);
    }

    internal Slices AddDiagramRule(DiagramRule rule)
    {
        ArgumentNullException.ThrowIfNull(rule);

        var rules = new DiagramRule[_diagramRules.Length + 1];
        Array.Copy(_diagramRules, rules, _diagramRules.Length);
        rules[_diagramRules.Length] = rule;
        return new Slices(_graph, _definitions, _rules, rules);
    }

    /// <summary>
    /// <c>to plantuml()</c>: renders the slicing's projected dependency graph as a PlantUML component
    /// diagram — one <c>component [Name]</c> per slice and one <c>[source] --&gt; [target]</c> arrow per
    /// dependency between slices, external dependencies included with the module name as the target
    /// component. A slice with no dependencies is still declared as a component. The report is a data
    /// form: an empty slicing renders a valid empty document, not a violation.
    /// </summary>
    /// <returns>The diagram source.</returns>
    /// <exception cref="UserError">A slice name cannot be embedded in the diagram's bracketed form.</exception>
    public string ToPlantUml()
    {
        IReadOnlyList<ProjectedEdge> edges = ArchUnitSharp.Projection.Projection.Edges(_graph, DiagramMap());
        IReadOnlyList<string> components = SlicesProjection.Slices(_graph, _definitions)
            .Concat(edges.Select(static edge => edge.Target))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static name => name, StringComparer.Ordinal)
            .ToArray();
        return PlantUmlRenderer.Render(edges, components);
    }

    /// <summary>
    /// <c>export as plantuml(path)</c>: renders the slicing's projected dependency graph as a PlantUML
    /// component diagram and writes it to <paramref name="path"/>. The write is the report's only disk
    /// boundary; a file that cannot be written is a <see cref="TechnicalError"/>.
    /// </summary>
    /// <param name="path">The file to write. Must not be <see langword="null"/> or empty; its directory must exist.</param>
    /// <returns><paramref name="path"/>, which now holds the diagram source.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="path"/> is empty.</exception>
    /// <exception cref="TechnicalError">The file cannot be written.</exception>
    public string ExportAsPlantUml(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        if (path.Length == 0)
        {
            throw new ArgumentException("Path must not be empty.", nameof(path));
        }

        try
        {
            File.WriteAllText(path, ToPlantUml());
            return path;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            throw new TechnicalError($"Failed to export the PlantUML diagram to '{path}'.", exception);
        }
    }

    /// <summary>
    /// The relabelling hook the diagram view of this policy projects the graph with: each file to the
    /// slice the policy's definitions assign it, external targets kept as the module name as written.
    /// </summary>
    private MapFunction DiagramMap() =>
        SlicesProjection.DiagramMap(identifier => SlicesProjection.SliceOf(_definitions, identifier));
}
