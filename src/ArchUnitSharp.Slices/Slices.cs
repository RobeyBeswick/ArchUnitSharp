namespace ArchUnitSharp.Slices;

using ArchUnitSharp.Common.Extraction;

/// <summary>
/// The slices domain module's fluent surface: a slicing policy over one project's <see cref="Graph"/>.
/// It is the ENTRY of a rule chain — built from the entry points <c>Project.ProjectSlices()</c> /
/// <c>Project.Slices()</c> — and the accumulator of the slice definitions and rules that make up a
/// policy. Defining a slice is <c>defined by(pattern)</c> or <c>defined by regex(pattern)</c>;
/// asserting a rule is <c>should (not) contain dependency(from, to)</c>.
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
    }

    private Slices(Graph graph, SliceDefinition[] definitions, SliceRule[] rules)
    {
        _graph = graph;
        _definitions = definitions;
        _rules = rules;
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
        Assertion.SlicesAssertion.Check(this, options);

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

    internal Slices AddDefinition(SliceDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        var definitions = new SliceDefinition[_definitions.Length + 1];
        Array.Copy(_definitions, definitions, _definitions.Length);
        definitions[_definitions.Length] = definition;
        return new Slices(_graph, definitions, _rules);
    }

    internal Slices AddRule(SliceRule rule)
    {
        ArgumentNullException.ThrowIfNull(rule);

        var rules = new SliceRule[_rules.Length + 1];
        Array.Copy(_rules, rules, _rules.Length);
        rules[_rules.Length] = rule;
        return new Slices(_graph, _definitions, rules);
    }
}
