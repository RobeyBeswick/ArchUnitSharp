namespace ArchUnitSharp.Layers;

/// <summary>
/// The internal data model of one layer rule: the subject layer a rule asserts over, the target
/// layers it may or may not depend on, and the mood — <c>may only depend on</c> (allowlist) versus
/// <c>may not depend on</c> (blocklist). A constraint is produced by
/// <c>where layer(...)</c> followed by <c>may only depend on layers(...)</c> or
/// <c>may not depend on layers(...)</c>, and checked by <see cref="Assertion.LayersAssertion"/>.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Negate"/> is <see langword="true"/> for the blocklist mood and <see langword="false"/>
/// for the allowlist mood. A blocklist reports a dependency on a target layer in
/// <see cref="TargetLayers"/>; an allowlist reports a dependency on a target layer <em>not</em> in
/// <see cref="TargetLayers"/>, and an allowlist with no target layers is a sealed layer — any
/// cross-layer dependency is a violation.
/// </para>
/// <para>
/// This type is immutable and safe for concurrent use. The target-layer list is copied on
/// construction and copied again on every read, so a caller can never corrupt a constraint through a
/// reference it obtained from it.
/// </para>
/// </remarks>
internal sealed class LayerConstraint
{
    private readonly string _subjectLayer;
    private readonly string[] _targetLayers;
    private readonly bool _negate;

    /// <summary>
    /// The layer the rule asserts over.
    /// </summary>
    internal string SubjectLayer => _subjectLayer;

    /// <summary>
    /// The target layers the rule names, in the order they were given. Each access returns a fresh
    /// copy, so the returned list is always safe to hold or mutate.
    /// </summary>
    internal IReadOnlyList<string> TargetLayers => _targetLayers.ToArray();

    /// <summary>
    /// <see langword="true"/> for the blocklist mood (<c>may not depend on</c>), <see langword="false"/>
    /// for the allowlist mood (<c>may only depend on</c>).
    /// </summary>
    internal bool Negate => _negate;

    /// <summary>
    /// Creates a layer constraint.
    /// </summary>
    /// <param name="subjectLayer">The layer the rule asserts over. Must not be <see langword="null"/> or empty.</param>
    /// <param name="targetLayers">The target layers; empty means a sealed layer. Must not be <see langword="null"/>.</param>
    /// <param name="negate"><see langword="true"/> for the blocklist mood, <see langword="false"/> for the allowlist mood.</param>
    /// <exception cref="ArgumentNullException"><paramref name="subjectLayer"/> or <paramref name="targetLayers"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="subjectLayer"/> is empty, or <paramref name="targetLayers"/> contains a <see langword="null"/> or empty name.</exception>
    internal LayerConstraint(string subjectLayer, IReadOnlyList<string> targetLayers, bool negate)
    {
        _subjectLayer = LayerDeclaration.RequireName(subjectLayer);
        ArgumentNullException.ThrowIfNull(targetLayers);

        string[] copy = targetLayers.ToArray();
        foreach (string name in copy)
        {
            _ = LayerDeclaration.RequireName(name);
        }

        _targetLayers = copy;
        _negate = negate;
    }

    /// <summary>
    /// Describes this constraint as a rule, for a report: the entry phrase <c>project layers</c>,
    /// the subject clause <c>where layer 'X'</c>, and the mood clause
    /// <c>may only depend on layers 'A', 'B'</c> or <c>may not depend on layers 'A'</c>.
    /// </summary>
    internal string DescribeRule()
    {
        string targets = _targetLayers.Length == 0
            ? string.Empty
            : $" '{string.Join("', '", _targetLayers)}'";
        string mood = _negate ? "may not" : "may only";
        return $"project layers where layer '{_subjectLayer}' {mood} depend on layers{targets}";
    }
}
