namespace ArchUnitSharp.Metrics;

using ArchUnitSharp.Common.Extraction;

/// <summary>
/// The terminal of a zone-guard rule chain: the scope and the <see cref="DistanceZone"/> it guards
/// against. Checked with <see cref="Check(CheckOptions?)"/>, which places each selected file's
/// abstractness/instability point on the diagram and reports every file that falls in the zone as one
/// <see cref="DistanceZoneViolation"/>.
/// </summary>
/// <remarks>
/// <para>
/// This type is built by a <see cref="DistanceMetrics"/> zone method and is the only rule shape the
/// zone guards produce; it is the whole terminal, not a selection with threshold methods, because
/// <c>not in zone of pain</c> and <c>not in zone of uselessness</c> are the rule. The zone's limits
/// are the calculation layer's fixed <c>0.3</c> and <c>0.7</c> boundaries, with the empty-test guard
/// reporting a rule whose subjects matched nothing.
/// </para>
/// <para>
/// This type is immutable and safe for concurrent use: checking never mutates it, so one rule can be
/// checked concurrently or repeatedly.
/// </para>
/// </remarks>
internal sealed class DistanceZoneRule : ICheckable
{
    private readonly Metrics _metrics;
    private readonly DistanceZone _zone;

    /// <summary>
    /// Creates a zone-guard rule over <paramref name="metrics"/> guarding against
    /// <paramref name="zone"/>.
    /// </summary>
    internal DistanceZoneRule(Metrics metrics, DistanceZone zone)
    {
        _metrics = metrics;
        _zone = zone;
    }

    /// <summary>
    /// The scope the rule asserts over.
    /// </summary>
    internal Metrics Scope => _metrics;

    /// <summary>
    /// The zone the rule guards against.
    /// </summary>
    internal DistanceZone Zone => _zone;

    /// <inheritdoc/>
    public IReadOnlyList<Violation> Check(CheckOptions? options = null) =>
        CheckLogging.Run(options, logger => Assertion.MetricsAssertion.CheckZone(this, options, logger));

    /// <inheritdoc/>
    void ICheckable.ProhibitExternalImplementation()
    {
    }
}
