namespace ArchUnitSharp.Metrics;

using ArchUnitSharp.Common.Extraction;
using ArchUnitSharp.Metrics.Rendering;

/// <summary>
/// The count-metric section of a metrics rule chain: <c>count</c>. Built from
/// <see cref="Metrics.Count"/>; its metric methods name what a rule counts and each returns the
/// <see cref="MetricSelection"/> whose threshold methods complete the rule.
/// </summary>
/// <remarks>
/// <para>
/// This type is the count section, nothing else: it carries no rule logic and no metric value. Each
/// metric method forwards the scope and the named metric to a <see cref="MetricSelection"/>, which is
/// where the rule's threshold is chosen and checked. The metric vocabulary is the sibling count set:
/// the class-level <see cref="MethodCount"/> and <see cref="FieldCount"/> and the file-level
/// <see cref="LinesOfCode"/>, <see cref="Statements"/>, <see cref="Imports"/>, <see cref="Classes"/>
/// and <see cref="Interfaces"/>. There is no <c>functions</c> metric: C# has no file-level function
/// concept distinct from a type member, and the issue's rule is to skip a metric the language cannot
/// express rather than fake it.
/// </para>
/// <para>
/// <see cref="ExportAsHtml"/> is the section's report terminal: it measures every count metric over
/// the scope's subjects and writes the measurements as a self-contained HTML page. A report is a data
/// form, not a rule, so an empty scope exports an explicit <c>No metric data.</c> page rather than a
/// violation.
/// </para>
/// <para>
/// This type is immutable and safe for concurrent use. Building a rule from it never mutates the scope
/// it was built from, so a <see cref="CountMetrics"/> value can be stored and reused.
/// </para>
/// </remarks>
public sealed class CountMetrics
{
    private readonly Metrics _metrics;

    /// <summary>
    /// Creates the count section over <paramref name="metrics"/>. Callers obtain a
    /// <see cref="CountMetrics"/> from <see cref="Metrics.Count"/> rather than constructing one.
    /// </summary>
    /// <param name="metrics">The scope the count section belongs to.</param>
    internal CountMetrics(Metrics metrics) => _metrics = metrics;

    /// <summary>
    /// The scope this count section belongs to. Internal: a metric selection reads it to check the
    /// rule the selection builds.
    /// </summary>
    internal Metrics Metrics => _metrics;

    /// <summary>
    /// <c>method count</c>: the number of methods of each class. A class-level metric: each selected
    /// class is measured, so a rule reads <c>method count should be below 20</c>.
    /// </summary>
    /// <returns>The metric selection whose threshold methods complete the rule.</returns>
    public MetricSelection MethodCount() => new(_metrics, Calculation.CountMetrics.MethodCount());

    /// <summary>
    /// <c>field count</c>: the number of fields of each class. A class-level metric: each selected
    /// class is measured, so a rule reads <c>field count should be below 20</c>.
    /// </summary>
    /// <returns>The metric selection whose threshold methods complete the rule.</returns>
    public MetricSelection FieldCount() => new(_metrics, Calculation.CountMetrics.FieldCount());

    /// <summary>
    /// <c>lines of code</c>: the number of non-blank lines of each file. A file-level metric, so a
    /// rule reads <c>lines of code should be below 500</c>.
    /// </summary>
    /// <returns>The metric selection whose threshold methods complete the rule.</returns>
    public MetricSelection LinesOfCode() => new(_metrics, Calculation.CountMetrics.LinesOfCode());

    /// <summary>
    /// <c>statements</c>: the number of statements of each file. A file-level metric, so a rule reads
    /// <c>statements should be below 200</c>.
    /// </summary>
    /// <returns>The metric selection whose threshold methods complete the rule.</returns>
    public MetricSelection Statements() => new(_metrics, Calculation.CountMetrics.Statements());

    /// <summary>
    /// <c>imports</c>: the number of import directives of each file. A file-level metric, so a rule
    /// reads <c>imports should be below 20</c>.
    /// </summary>
    /// <returns>The metric selection whose threshold methods complete the rule.</returns>
    public MetricSelection Imports() => new(_metrics, Calculation.CountMetrics.Imports());

    /// <summary>
    /// <c>classes</c>: the number of classes of each file. A file-level metric, so a rule reads
    /// <c>classes should be below 3</c>.
    /// </summary>
    /// <returns>The metric selection whose threshold methods complete the rule.</returns>
    public MetricSelection Classes() => new(_metrics, Calculation.CountMetrics.Classes());

    /// <summary>
    /// <c>interfaces</c>: the number of interfaces of each file. A file-level metric, so a rule reads
    /// <c>interfaces should be below 3</c>.
    /// </summary>
    /// <returns>The metric selection whose threshold methods complete the rule.</returns>
    public MetricSelection Interfaces() => new(_metrics, Calculation.CountMetrics.Interfaces());

    /// <summary>
    /// <c>export as html(path)</c>: measures every count metric over the scope's subjects — the
    /// class-level <c>method count</c> and <c>field count</c> of each selected class and the file-level
    /// <c>lines of code</c>, <c>statements</c>, <c>imports</c>, <c>classes</c> and <c>interfaces</c> of
    /// each selected file — and writes the measurements as a self-contained HTML page at
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
        MetricsExporter.ExportAsHtml(MetricsReportData.Count(_metrics), path, options);
}
