namespace ArchUnitSharp.Metrics;

using ArchUnitSharp.Common.Extraction;
using ArchUnitSharp.Metrics.Rendering;

/// <summary>
/// The distance-metric section of a metrics rule chain: <c>distance</c>. Built from
/// <see cref="Metrics.Distance"/>; its metric methods name what a rule measures and each returns the
/// <see cref="DistanceMetricSelection"/> whose threshold methods complete the rule, and its zone
/// methods are the <c>not in zone of pain</c> and <c>not in zone of uselessness</c> guards.
/// </summary>
/// <remarks>
/// <para>
/// This type is the distance section, nothing else: it carries no rule logic and no metric value.
/// Each metric method forwards the scope and the named metric to a <see cref="DistanceMetricSelection"/>,
/// which is where the rule's threshold is chosen and checked. The metric vocabulary is Robert C.
/// Martin's dependency-derived set: the file-level <see cref="Abstractness"/>,
/// <see cref="Instability"/>, <see cref="DistanceFromMainSequence"/>, <see cref="CouplingFactor"/>
/// and <see cref="NormalisedDistance"/>. The zone methods each return the guard's terminal directly —
/// a rule reads <c>not in zone of pain</c>, not a comparison.
/// </para>
/// <para>
/// <see cref="ExportAsHtml"/> is the section's report terminal: it measures every distance metric over
/// the scope's files and writes the measurements as a self-contained HTML page. A report is a data
/// form, not a rule, so an empty scope exports an explicit <c>No metric data.</c> page rather than a
/// violation.
/// </para>
/// <para>
/// This type is immutable and safe for concurrent use. Building a rule from it never mutates the scope
/// it was built from, so a <see cref="DistanceMetrics"/> value can be stored and reused.
/// </para>
/// </remarks>
public sealed class DistanceMetrics
{
    private readonly Metrics _metrics;

    /// <summary>
    /// Creates the distance section over <paramref name="metrics"/>. Callers obtain a
    /// <see cref="DistanceMetrics"/> from <see cref="Metrics.Distance"/> rather than constructing one.
    /// </summary>
    /// <param name="metrics">The scope the distance section belongs to.</param>
    internal DistanceMetrics(Metrics metrics) => _metrics = metrics;

    /// <summary>
    /// The scope this distance section belongs to. Internal: a metric selection and a zone rule read
    /// it to check the rule they build.
    /// </summary>
    internal Metrics Metrics => _metrics;

    /// <summary>
    /// <c>abstractness</c>: the ratio of each file's abstract types to its types. A file-level metric,
    /// so a rule reads <c>abstractness should be above 0.3</c>.
    /// </summary>
    /// <returns>The metric selection whose threshold methods complete the rule.</returns>
    public DistanceMetricSelection Abstractness() =>
        new(_metrics, Calculation.DistanceMetrics.Abstractness());

    /// <summary>
    /// <c>instability</c>: each file's outgoing dependency share. A file-level metric, so a rule reads
    /// <c>instability should be below 0.8</c>.
    /// </summary>
    /// <returns>The metric selection whose threshold methods complete the rule.</returns>
    public DistanceMetricSelection Instability() =>
        new(_metrics, Calculation.DistanceMetrics.Instability());

    /// <summary>
    /// <c>distance from main sequence</c>: how far each file's abstractness/instability point falls
    /// from the balanced line. A file-level metric, so a rule reads <c>distance from main sequence
    /// should be below 0.5</c>.
    /// </summary>
    /// <returns>The metric selection whose threshold methods complete the rule.</returns>
    public DistanceMetricSelection DistanceFromMainSequence() =>
        new(_metrics, Calculation.DistanceMetrics.DistanceFromMainSequence());

    /// <summary>
    /// <c>coupling factor</c>: each file's share of the project's possible couplings. A file-level
    /// metric, so a rule reads <c>coupling factor should be below 0.8</c>.
    /// </summary>
    /// <returns>The metric selection whose threshold methods complete the rule.</returns>
    public DistanceMetricSelection CouplingFactor() =>
        new(_metrics, Calculation.DistanceMetrics.CouplingFactor());

    /// <summary>
    /// <c>normalised distance</c>: each file's distance from the main sequence, discounted by its
    /// size. A file-level metric, so a rule reads <c>normalised distance should be below 0.5</c>.
    /// </summary>
    /// <returns>The metric selection whose threshold methods complete the rule.</returns>
    public DistanceMetricSelection NormalisedDistance() =>
        new(_metrics, Calculation.DistanceMetrics.NormalisedDistance());

    /// <summary>
    /// <c>not in zone of pain</c>: no selected file's abstractness/instability point may fall in the
    /// zone of pain — abstractness and instability both below 0.3. A file whose point does is reported
    /// as one <see cref="DistanceZoneViolation"/>, and the empty-test guard reports a rule whose
    /// subjects matched nothing.
    /// </summary>
    /// <returns>The rule's terminal, checked with <see cref="ICheckable.Check(CheckOptions?)"/>.</returns>
    public ICheckable NotInZoneOfPain() => new DistanceZoneRule(_metrics, DistanceZone.Pain);

    /// <summary>
    /// <c>not in zone of uselessness</c>: no selected file's abstractness/instability point may fall
    /// in the zone of uselessness — abstractness and instability both above 0.7. A file whose point
    /// does is reported as one <see cref="DistanceZoneViolation"/>, and the empty-test guard reports a
    /// rule whose subjects matched nothing.
    /// </summary>
    /// <returns>The rule's terminal, checked with <see cref="ICheckable.Check(CheckOptions?)"/>.</returns>
    public ICheckable NotInZoneOfUselessness() => new DistanceZoneRule(_metrics, DistanceZone.Uselessness);

    /// <summary>
    /// <c>export as html(path)</c>: measures every distance metric over the scope's files —
    /// <c>abstractness</c>, <c>instability</c>, <c>distance from main sequence</c>, <c>coupling
    /// factor</c> and <c>normalised distance</c> — and writes the measurements as a self-contained
    /// HTML page at <paramref name="path"/>, creating the file's directory when it does not exist. The
    /// title, the timestamp and the stylesheet come from <paramref name="options"/>, which defaults to
    /// <c>new MetricsExportOptions()</c> when <see langword="null"/>.
    /// </summary>
    /// <param name="path">The file to write. Must not be <see langword="null"/> or empty.</param>
    /// <param name="options">The report's options; <see langword="null"/> means the defaults in <see cref="MetricsExportOptions"/>.</param>
    /// <returns><paramref name="path"/>, which now holds the HTML document.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="path"/> is empty.</exception>
    /// <exception cref="UserError">The scope was built without a source provider, so a selected file's text is unavailable.</exception>
    /// <exception cref="TechnicalError">The file cannot be written.</exception>
    public string ExportAsHtml(string path, MetricsExportOptions? options = null) =>
        MetricsExporter.ExportAsHtml(MetricsReportData.Distance(_metrics), path, options);
}
