namespace ArchUnitSharp.Metrics;

/// <summary>
/// Discriminates the distance metrics the metrics module measures: Robert C. Martin's
/// dependency-derived metrics over one file — <see cref="Abstractness"/>, <see cref="Instability"/>,
/// <see cref="DistanceFromMainSequence"/>, <see cref="CouplingFactor"/> and
/// <see cref="NormalisedDistance"/>. Every kind is a file-level metric — it measures one
/// <see cref="DistanceInfo"/> carrying the file's types and its internal dependency couplings — so a
/// <see cref="DistanceMetric"/> always carries the <see cref="MetricSubject.File"/> subject.
/// </summary>
/// <remarks>
/// <para>
/// Abstractness and instability are the two axes of Martin's main-sequence diagram; distance and
/// normalised distance measure how far a file's point falls from the balanced line
/// <c>A + I = 1</c>, and the coupling factor is the file's share of the project's possible internal
/// couplings. The formulas live in the calculation layer's <c>DistanceMetrics</c>, one place, and are
/// the sibling implementations' reading of the classic formulas. Every value is a
/// <see cref="double"/> in <c>[0, 1]</c>.
/// </para>
/// </remarks>
public enum DistanceMetricKind
{
    /// <summary>
    /// Abstractness: the ratio of the file's abstract types — its interfaces plus abstract classes —
    /// to its total types (classes plus interfaces). Zero when the file declares no types.
    /// </summary>
    Abstractness,

    /// <summary>
    /// Instability: the ratio of the file's efferent couplings — the distinct internal files it
    /// depends on — to all its internal couplings. Zero when the file has no couplings; one when it
    /// only depends outward.
    /// </summary>
    Instability,

    /// <summary>
    /// Distance from the main sequence: the absolute deviation <c>|A + I − 1|</c> of the file's
    /// abstractness/instability point from the balanced line. Zero sits on the line; one is as far
    /// from it as a point can be.
    /// </summary>
    DistanceFromMainSequence,

    /// <summary>
    /// Coupling factor: the file's afferent plus efferent couplings as a share of the couplings the
    /// project could possibly hold, <c>(Ca + Ce) / (2·(n − 1))</c> for <c>n</c> project files. Zero
    /// for a one-file project.
    /// </summary>
    CouplingFactor,

    /// <summary>
    /// Normalised distance: the distance from the main sequence discounted by the file's size — a
    /// larger file's deviation counts for less — so a file of at least a hundred lines is discounted
    /// by the full half and a shorter one by proportionally less, down to no discount for an empty
    /// file.
    /// </summary>
    NormalisedDistance,
}
