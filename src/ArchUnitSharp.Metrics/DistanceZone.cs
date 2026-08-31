namespace ArchUnitSharp.Metrics;

/// <summary>
/// The architectural zone a <see cref="DistanceZoneRule"/> guards against: the two discouraged
/// regions of the abstractness/instability diagram. The zone of pain is the corner where abstractness
/// and instability are both low — a concrete file that many files depend on and nothing balances — and
/// the zone of uselessness is the opposite corner where both are high — an abstract file that depends
/// on much of the project and nothing depends on it.
/// </summary>
public enum DistanceZone
{
    /// <summary>
    /// The zone of pain: abstractness below 0.3 and instability below 0.3. A concrete, stable file is
    /// painful because changing it ripples through its dependents and it has no abstraction to absorb
    /// the change.
    /// </summary>
    Pain,

    /// <summary>
    /// The zone of uselessness: abstractness above 0.7 and instability above 0.7. An abstract,
    /// unstable file is useless because its abstractions have no concrete dependents to use them.
    /// </summary>
    Uselessness,
}
