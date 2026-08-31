namespace ArchUnitSharp.Metrics.Calculation;

using ArchUnitSharp.Metrics;

/// <summary>
/// The metrics module's pure distance calculations: Robert C. Martin's five dependency-derived
/// metrics — abstractness, instability, distance from the main sequence, coupling factor and
/// normalised distance — and the value of one metric over one file. This is the one place a distance
/// metric's value is computed — a <see cref="DistanceMetric"/> is a name and a subject kind, and the
/// calculation turns the pair into the measured number — and the one place the zone guards'
/// abstractness/instability thresholds live.
/// </summary>
/// <remarks>
/// <para>
/// Each factory returns the <see cref="DistanceMetric"/> the fluent surface exposes. Every metric
/// measures one <see cref="DistanceInfo"/> and is computed from the file's types and its internal
/// dependency couplings, so the formulas are pure over the projected info.
/// </para>
/// <para>
/// <see cref="ValueOf(DistanceMetric, DistanceInfo)"/> computes one metric's value from a
/// <see cref="DistanceInfo"/>, with <c>A</c> the abstractness, <c>I</c> the instability, <c>Ca</c> and
/// <c>Ce</c> the afferent and efferent couplings and <c>n</c> the project's file count:
/// </para>
/// <list type="bullet">
/// <item>
/// <description>
/// <see cref="Abstractness()"/> is <c>abstract types / types</c>, zero when the file declares no types.
/// </description>
/// </item>
/// <item>
/// <description>
/// <see cref="Instability()"/> is <c>Ce / (Ca + Ce)</c>, zero when the file has no couplings.
/// </description>
/// </item>
/// <item>
/// <description>
/// <see cref="DistanceFromMainSequence()"/> is <c>|A + I − 1|</c>.
/// </description>
/// </item>
/// <item>
/// <description>
/// <see cref="CouplingFactor()"/> is <c>(Ca + Ce) / (2·(n − 1))</c>, zero for a one-file project.
/// </description>
/// </item>
/// <item>
/// <description>
/// <see cref="NormalisedDistance()"/> is the distance discounted by the file's size: the discount is
/// <c>min(lines/100, 1) · 0.5</c>, so a file of at least a hundred lines is discounted by the full
/// half and a shorter one by proportionally less, down to no discount for an empty file.
/// </description>
/// </item>
/// </list>
/// <para>
/// <see cref="InZone(DistanceInfo, DistanceZone)"/> decides the zone guards: a file is in the
/// <see cref="DistanceZone.Pain"/> when <c>A &lt; 0.3</c> and <c>I &lt; 0.3</c>, and in the
/// <see cref="DistanceZone.Uselessness"/> when <c>A &gt; 0.7</c> and <c>I &gt; 0.7</c>, with strict
/// boundaries in both directions.
/// </para>
/// <para>
/// All values are <see cref="double"/> in <c>[0, 1]</c>. This type is stateless and safe for
/// concurrent use.
/// </para>
/// </remarks>
internal static class DistanceMetrics
{
    /// <summary>
    /// The abstractness and instability threshold below which a file is in the
    /// <see cref="DistanceZone.Pain"/>: both must be strictly below it.
    /// </summary>
    public const double PainLimit = 0.3;

    /// <summary>
    /// The abstractness and instability threshold above which a file is in the
    /// <see cref="DistanceZone.Uselessness"/>: both must be strictly above it.
    /// </summary>
    public const double UselessnessLimit = 0.7;

    private const double SizeNormalisationLines = 100.0;
    private const double MaximumSizeDiscount = 0.5;

    /// <summary>
    /// The <c>abstractness</c> metric: the ratio of a file's abstract types to its types. A file-level
    /// metric.
    /// </summary>
    public static DistanceMetric Abstractness() => new(DistanceMetricKind.Abstractness, MetricSubject.File);

    /// <summary>
    /// The <c>instability</c> metric: a file's efferent coupling as a share of all its couplings. A
    /// file-level metric.
    /// </summary>
    public static DistanceMetric Instability() => new(DistanceMetricKind.Instability, MetricSubject.File);

    /// <summary>
    /// The <c>distance from main sequence</c> metric: <c>|A + I − 1|</c> for one file. A file-level
    /// metric.
    /// </summary>
    public static DistanceMetric DistanceFromMainSequence() =>
        new(DistanceMetricKind.DistanceFromMainSequence, MetricSubject.File);

    /// <summary>
    /// The <c>coupling factor</c> metric: one file's couplings as a share of the project's possible
    /// ones. A file-level metric.
    /// </summary>
    public static DistanceMetric CouplingFactor() => new(DistanceMetricKind.CouplingFactor, MetricSubject.File);

    /// <summary>
    /// The <c>normalised distance</c> metric: the distance from the main sequence discounted by one
    /// file's size. A file-level metric.
    /// </summary>
    public static DistanceMetric NormalisedDistance() =>
        new(DistanceMetricKind.NormalisedDistance, MetricSubject.File);

    /// <summary>
    /// Computes a distance metric's value over one file. Every distance metric is a file-level metric,
    /// so the subject is always a projected <see cref="DistanceInfo"/>.
    /// </summary>
    /// <param name="metric">The metric to compute. Must not be <see langword="null"/>.</param>
    /// <param name="info">The file to measure. Must not be <see langword="null"/>.</param>
    /// <returns>The metric's value for the file.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="metric"/> or <paramref name="info"/> is <see langword="null"/>.</exception>
    public static double ValueOf(DistanceMetric metric, DistanceInfo info)
    {
        ArgumentNullException.ThrowIfNull(metric);
        ArgumentNullException.ThrowIfNull(info);

        return metric.Kind switch
        {
            DistanceMetricKind.Abstractness => Abstractness(info),
            DistanceMetricKind.Instability => Instability(info),
            DistanceMetricKind.DistanceFromMainSequence => DistanceFromMainSequence(info),
            DistanceMetricKind.CouplingFactor => CouplingFactor(info),
            DistanceMetricKind.NormalisedDistance => NormalisedDistance(info),
            _ => throw new ArgumentOutOfRangeException(
                nameof(metric),
                metric.Kind,
                "Metric is not a defined distance metric."),
        };
    }

    /// <summary>
    /// Whether a file's abstractness/instability point falls in <paramref name="zone"/>: both axes
    /// strictly below <see cref="PainLimit"/> for <see cref="DistanceZone.Pain"/>, both strictly
    /// above <see cref="UselessnessLimit"/> for <see cref="DistanceZone.Uselessness"/>.
    /// </summary>
    /// <param name="info">The file to place on the diagram. Must not be <see langword="null"/>.</param>
    /// <param name="zone">The zone to test against.</param>
    /// <returns><see langword="true"/> when the file's point falls in the zone.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="info"/> is <see langword="null"/>.</exception>
    public static bool InZone(DistanceInfo info, DistanceZone zone)
    {
        ArgumentNullException.ThrowIfNull(info);

        return zone switch
        {
            DistanceZone.Pain => Abstractness(info) < PainLimit && Instability(info) < PainLimit,
            DistanceZone.Uselessness =>
                Abstractness(info) > UselessnessLimit && Instability(info) > UselessnessLimit,
            _ => throw new ArgumentOutOfRangeException(
                nameof(zone),
                zone,
                "Zone is not a defined DistanceZone value."),
        };
    }

    /// <summary>
    /// A file's abstractness: its abstract types over its types, zero when it declares no types.
    /// </summary>
    private static double Abstractness(DistanceInfo info) =>
        info.TypeCount == 0 ? 0.0 : info.AbstractTypeCount / (double)info.TypeCount;

    /// <summary>
    /// A file's instability: its efferent coupling over all its couplings, zero when it has none.
    /// </summary>
    private static double Instability(DistanceInfo info)
    {
        int total = info.AfferentCoupling + info.EfferentCoupling;
        return total == 0 ? 0.0 : info.EfferentCoupling / (double)total;
    }

    /// <summary>
    /// A file's distance from the main sequence: the absolute deviation of its abstractness plus
    /// instability from one.
    /// </summary>
    private static double DistanceFromMainSequence(DistanceInfo info) =>
        Math.Abs(Abstractness(info) + Instability(info) - 1.0);

    /// <summary>
    /// A file's coupling factor: its couplings over the <c>2·(n − 1)</c> couplings a project of
    /// <c>n</c> files could hold, zero when no such couplings exist.
    /// </summary>
    private static double CouplingFactor(DistanceInfo info)
    {
        int possible = 2 * (info.ProjectFileCount - 1);
        return possible <= 0
            ? 0.0
            : (info.AfferentCoupling + info.EfferentCoupling) / (double)possible;
    }

    /// <summary>
    /// A file's normalised distance: its distance from the main sequence discounted by its size, the
    /// discount at most half when the file has at least a hundred lines.
    /// </summary>
    private static double NormalisedDistance(DistanceInfo info)
    {
        double sizeRatio = Math.Min(info.LinesOfCode / SizeNormalisationLines, 1.0);
        double discount = sizeRatio * MaximumSizeDiscount;
        return DistanceFromMainSequence(info) * (1.0 - discount);
    }
}
