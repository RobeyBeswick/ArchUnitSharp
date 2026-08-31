namespace ArchUnitSharp.Metrics;

using ArchUnitSharp.Common.Extraction;
using ArchUnitSharp.Metrics.Rendering;

/// <summary>
/// The cohesion-metric section of a metrics rule chain: <c>lcom</c>. Built from <see cref="Metrics.Lcom"/>;
/// its metric methods name what a rule measures and each returns the <see cref="LcomMetricSelection"/>
/// whose threshold methods complete the rule.
/// </summary>
/// <remarks>
/// <para>
/// This type is the lcom section, nothing else: it carries no rule logic and no metric value. Each
/// metric method forwards the scope and the named metric to a <see cref="LcomMetricSelection"/>, which
/// is where the rule's threshold is chosen and checked. The metric vocabulary is the sibling LCOM
/// family: the class-level <see cref="Lcom96a"/>, <see cref="Lcom96b"/>, <see cref="Lcom1"/>,
/// <see cref="Lcom2"/>, <see cref="Lcom3"/>, <see cref="Lcom4"/>, <see cref="Lcom5"/> and
/// <see cref="LcomStar"/>.
/// </para>
/// <para>
/// <see cref="ExportAsHtml"/> is the section's report terminal: it measures every cohesion metric over
/// the scope's classes and writes the measurements as a self-contained HTML page. A report is a data
/// form, not a rule, so an empty scope exports an explicit <c>No metric data.</c> page rather than a
/// violation.
/// </para>
/// <para>
/// This type is immutable and safe for concurrent use. Building a rule from it never mutates the scope
/// it was built from, so a <see cref="LcomMetrics"/> value can be stored and reused.
/// </para>
/// </remarks>
public sealed class LcomMetrics
{
    private readonly Metrics _metrics;

    /// <summary>
    /// Creates the lcom section over <paramref name="metrics"/>. Callers obtain a
    /// <see cref="LcomMetrics"/> from <see cref="Metrics.Lcom"/> rather than constructing one.
    /// </summary>
    /// <param name="metrics">The scope the lcom section belongs to.</param>
    internal LcomMetrics(Metrics metrics) => _metrics = metrics;

    /// <summary>
    /// The scope this lcom section belongs to. Internal: a metric selection reads it to check the rule
    /// the selection builds.
    /// </summary>
    internal Metrics Metrics => _metrics;

    /// <summary>
    /// <c>lcom96a</c>: the normalised method-field distance of each class. A class-level metric, so a
    /// rule reads <c>lcom96a should be below 0.8</c>.
    /// </summary>
    /// <returns>The metric selection whose threshold methods complete the rule.</returns>
    public LcomMetricSelection Lcom96a() => new(_metrics, Calculation.LcomMetrics.Lcom96a());

    /// <summary>
    /// <c>lcom96b</c>: the method-field density complement of each class. A class-level metric, so a
    /// rule reads <c>lcom96b should be below 0.3</c>.
    /// </summary>
    /// <returns>The metric selection whose threshold methods complete the rule.</returns>
    public LcomMetricSelection Lcom96b() => new(_metrics, Calculation.LcomMetrics.Lcom96b());

    /// <summary>
    /// <c>lcom1</c>: the non-sharing minus sharing method pairs of each class. A class-level metric, so
    /// a rule reads <c>lcom1 should be below 3</c>.
    /// </summary>
    /// <returns>The metric selection whose threshold methods complete the rule.</returns>
    public LcomMetricSelection Lcom1() => new(_metrics, Calculation.LcomMetrics.Lcom1());

    /// <summary>
    /// <c>lcom2</c>: the method-field density complement of each class, the same formula as
    /// <see cref="Lcom96b()"/>. A class-level metric, so a rule reads <c>lcom2 should be below 0.5</c>.
    /// </summary>
    /// <returns>The metric selection whose threshold methods complete the rule.</returns>
    public LcomMetricSelection Lcom2() => new(_metrics, Calculation.LcomMetrics.Lcom2());

    /// <summary>
    /// <c>lcom3</c>: the normalised method-field distance of each class, the same formula as
    /// <see cref="Lcom96a()"/>. A class-level metric, so a rule reads <c>lcom3 should be below 0.8</c>.
    /// </summary>
    /// <returns>The metric selection whose threshold methods complete the rule.</returns>
    public LcomMetricSelection Lcom3() => new(_metrics, Calculation.LcomMetrics.Lcom3());

    /// <summary>
    /// <c>lcom4</c>: the connected components of the method graph of each class. A class-level metric,
    /// so a rule reads <c>lcom4 should be 1</c>.
    /// </summary>
    /// <returns>The metric selection whose threshold methods complete the rule.</returns>
    public LcomMetricSelection Lcom4() => new(_metrics, Calculation.LcomMetrics.Lcom4());

    /// <summary>
    /// <c>lcom5</c>: the normalised method-field distance of each class, the same formula as
    /// <see cref="Lcom96a()"/>. A class-level metric, so a rule reads <c>lcom5 should be below 0.8</c>.
    /// </summary>
    /// <returns>The metric selection whose threshold methods complete the rule.</returns>
    public LcomMetricSelection Lcom5() => new(_metrics, Calculation.LcomMetrics.Lcom5());

    /// <summary>
    /// <c>lcom*</c>: the normalised method-field distance of each class, the same formula as
    /// <see cref="Lcom96a()"/>. A class-level metric, so a rule reads <c>lcom* should be below 0.8</c>.
    /// </summary>
    /// <returns>The metric selection whose threshold methods complete the rule.</returns>
    public LcomMetricSelection LcomStar() => new(_metrics, Calculation.LcomMetrics.LcomStar());

    /// <summary>
    /// <c>export as html(path)</c>: measures every cohesion metric over the scope's classes —
    /// <c>lcom96a</c>, <c>lcom96b</c>, <c>lcom1</c>, <c>lcom2</c>, <c>lcom3</c>, <c>lcom4</c>,
    /// <c>lcom5</c> and <c>lcom*</c> — and writes the measurements as a self-contained HTML page at
    /// <paramref name="path"/>, creating the file's directory when it does not exist. The title, the
    /// timestamp and the stylesheet come from <paramref name="options"/>, which defaults to
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
        MetricsExporter.ExportAsHtml(MetricsReportData.Lcom(_metrics), path, options);
}
