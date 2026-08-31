namespace ArchUnitSharp.Metrics.Rendering;

using System.Globalization;
using ArchUnitSharp.Common.Extraction;
using ArchUnitSharp.Metrics;
using ArchUnitSharp.Metrics.Extraction;
using ArchUnitSharp.Metrics.Projection;
using CountMetricCalculation = ArchUnitSharp.Metrics.Calculation.CountMetrics;
using DistanceCalculation = ArchUnitSharp.Metrics.Calculation.DistanceMetrics;
using LcomCalculation = ArchUnitSharp.Metrics.Calculation.LcomMetrics;

/// <summary>
/// The metrics module's report data: the <c>metric [subject]</c> → value map an HTML metrics report
/// renders. One method per metric family — <see cref="Count"/>, <see cref="Lcom"/> and
/// <see cref="Distance"/> — measures every metric the family's builder exposes over the subjects the
/// scope leaves in scope, so a report over a count scope shows all seven count metrics, a report over
/// a cohesion scope all eight LCOM metrics, and a report over a distance scope all five distance
/// metrics.
/// </summary>
/// <remarks>
/// <para>
/// A report is a data form, not a rule: the data map is measured the same way a rule's assertion
/// measures its subjects — the scope's files extracted, the class selector applied — but an empty
/// scope yields an empty map, which the renderer shows as an explicit <c>No metric data.</c> state
/// rather than a violation. A scope built without a source provider raises a <see cref="UserError"/>
/// when a selected file's text is read, exactly as a rule over it does.
/// </para>
/// <para>
/// Every key is the metric's own word followed by the subject's identifier in brackets — <c>method
/// count [App.Models.Car]</c> for one class, <c>lines of code [src/Models/Car.cs]</c> for one file —
/// and every value is the measured number, formatted in the invariant culture so a report is the same
/// on every machine. The map is a fresh copy on every call and the renderer sorts its rows, so the
/// report is stable and reproducible.
/// </para>
/// <para>
/// This type is stateless and safe for concurrent use.
/// </para>
/// </remarks>
internal static class MetricsReportData
{
    /// <summary>
    /// The count report's data map: the class-level <c>method count</c> and <c>field count</c> of
    /// every selected class and the file-level <c>lines of code</c>, <c>statements</c>, <c>imports</c>,
    /// <c>classes</c> and <c>interfaces</c> of every selected file.
    /// </summary>
    /// <param name="scope">The scope to measure. Must not be <see langword="null"/>.</param>
    /// <returns>The metric values, keyed by metric and subject.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="scope"/> is <see langword="null"/>.</exception>
    /// <exception cref="UserError"><paramref name="scope"/> was built without a source provider, so a selected file's text is unavailable.</exception>
    public static IReadOnlyDictionary<string, string> Count(Metrics scope)
    {
        ArgumentNullException.ThrowIfNull(scope);

        IReadOnlyList<FileInfo> files = Extract(scope);
        var data = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (ClassInfo classInfo in MetricsProjection.SelectClasses(files, scope.ClassFilters))
        {
            Add(data, MetricWords.Count(CountMetricKind.MethodCount), classInfo.Identifier,
                CountMetricCalculation.ValueOf(CountMetricCalculation.MethodCount(), classInfo));
            Add(data, MetricWords.Count(CountMetricKind.FieldCount), classInfo.Identifier,
                CountMetricCalculation.ValueOf(CountMetricCalculation.FieldCount(), classInfo));
        }

        foreach (FileInfo file in MetricsProjection.SelectFileSubjects(files, scope.ClassFilters))
        {
            Add(data, MetricWords.Count(CountMetricKind.LinesOfCode), file.Path,
                CountMetricCalculation.ValueOf(CountMetricCalculation.LinesOfCode(), file));
            Add(data, MetricWords.Count(CountMetricKind.Statements), file.Path,
                CountMetricCalculation.ValueOf(CountMetricCalculation.Statements(), file));
            Add(data, MetricWords.Count(CountMetricKind.Imports), file.Path,
                CountMetricCalculation.ValueOf(CountMetricCalculation.Imports(), file));
            Add(data, MetricWords.Count(CountMetricKind.Classes), file.Path,
                CountMetricCalculation.ValueOf(CountMetricCalculation.Classes(), file));
            Add(data, MetricWords.Count(CountMetricKind.Interfaces), file.Path,
                CountMetricCalculation.ValueOf(CountMetricCalculation.Interfaces(), file));
        }

        return data;
    }

    /// <summary>
    /// The cohesion report's data map: all eight LCOM metrics — <c>lcom96a</c>, <c>lcom96b</c>,
    /// <c>lcom1</c>, <c>lcom2</c>, <c>lcom3</c>, <c>lcom4</c>, <c>lcom5</c> and <c>lcom*</c> — of
    /// every selected class.
    /// </summary>
    /// <param name="scope">The scope to measure. Must not be <see langword="null"/>.</param>
    /// <returns>The metric values, keyed by metric and subject.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="scope"/> is <see langword="null"/>.</exception>
    /// <exception cref="UserError"><paramref name="scope"/> was built without a source provider, so a selected file's text is unavailable.</exception>
    public static IReadOnlyDictionary<string, string> Lcom(Metrics scope)
    {
        ArgumentNullException.ThrowIfNull(scope);

        IReadOnlyList<FileInfo> files = Extract(scope);
        var data = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (ClassInfo classInfo in MetricsProjection.SelectClasses(files, scope.ClassFilters))
        {
            Add(data, MetricWords.Lcom(LcomMetricKind.Lcom96a), classInfo.Identifier,
                LcomCalculation.ValueOf(LcomCalculation.Lcom96a(), classInfo));
            Add(data, MetricWords.Lcom(LcomMetricKind.Lcom96b), classInfo.Identifier,
                LcomCalculation.ValueOf(LcomCalculation.Lcom96b(), classInfo));
            Add(data, MetricWords.Lcom(LcomMetricKind.Lcom1), classInfo.Identifier,
                LcomCalculation.ValueOf(LcomCalculation.Lcom1(), classInfo));
            Add(data, MetricWords.Lcom(LcomMetricKind.Lcom2), classInfo.Identifier,
                LcomCalculation.ValueOf(LcomCalculation.Lcom2(), classInfo));
            Add(data, MetricWords.Lcom(LcomMetricKind.Lcom3), classInfo.Identifier,
                LcomCalculation.ValueOf(LcomCalculation.Lcom3(), classInfo));
            Add(data, MetricWords.Lcom(LcomMetricKind.Lcom4), classInfo.Identifier,
                LcomCalculation.ValueOf(LcomCalculation.Lcom4(), classInfo));
            Add(data, MetricWords.Lcom(LcomMetricKind.Lcom5), classInfo.Identifier,
                LcomCalculation.ValueOf(LcomCalculation.Lcom5(), classInfo));
            Add(data, MetricWords.Lcom(LcomMetricKind.LcomStar), classInfo.Identifier,
                LcomCalculation.ValueOf(LcomCalculation.LcomStar(), classInfo));
        }

        return data;
    }

    /// <summary>
    /// The distance report's data map: the file-level <c>abstractness</c>, <c>instability</c>,
    /// <c>distance from main sequence</c>, <c>coupling factor</c> and <c>normalised distance</c> of
    /// every selected file.
    /// </summary>
    /// <param name="scope">The scope to measure. Must not be <see langword="null"/>.</param>
    /// <returns>The metric values, keyed by metric and subject.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="scope"/> is <see langword="null"/>.</exception>
    /// <exception cref="UserError"><paramref name="scope"/> was built without a source provider, so a selected file's text is unavailable.</exception>
    public static IReadOnlyDictionary<string, string> Distance(Metrics scope)
    {
        ArgumentNullException.ThrowIfNull(scope);

        var data = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (DistanceInfo info in DistanceSubjects(scope))
        {
            Add(data, MetricWords.Distance(DistanceMetricKind.Abstractness), info.File,
                DistanceCalculation.ValueOf(DistanceCalculation.Abstractness(), info));
            Add(data, MetricWords.Distance(DistanceMetricKind.Instability), info.File,
                DistanceCalculation.ValueOf(DistanceCalculation.Instability(), info));
            Add(data, MetricWords.Distance(DistanceMetricKind.DistanceFromMainSequence), info.File,
                DistanceCalculation.ValueOf(DistanceCalculation.DistanceFromMainSequence(), info));
            Add(data, MetricWords.Distance(DistanceMetricKind.CouplingFactor), info.File,
                DistanceCalculation.ValueOf(DistanceCalculation.CouplingFactor(), info));
            Add(data, MetricWords.Distance(DistanceMetricKind.NormalisedDistance), info.File,
                DistanceCalculation.ValueOf(DistanceCalculation.NormalisedDistance(), info));
        }

        return data;
    }

    /// <summary>
    /// Extracts the files the scope's file selectors name, in sorted identifier order.
    /// </summary>
    private static IReadOnlyList<FileInfo> Extract(Metrics scope)
    {
        IReadOnlyList<string> selected = MetricsProjection.SelectFiles(scope.Graph, scope.FileFilters);
        if (selected.Count == 0)
        {
            return Array.Empty<FileInfo>();
        }

        return selected
            .Select(path => MetricsExtractor.Extract(path, scope.SourceText(path)))
            .ToArray();
    }

    /// <summary>
    /// The scope's file-level distance subjects as distance infos, the same projection a distance rule
    /// measures: the files the file selectors name, narrowed to the files that contain a matching
    /// class, each enriched with its couplings in the whole project's graph.
    /// </summary>
    private static IReadOnlyList<DistanceInfo> DistanceSubjects(Metrics scope)
    {
        IReadOnlyList<FileInfo> files = Extract(scope);
        IReadOnlyList<FileInfo> subjects = MetricsProjection.SelectFileSubjects(files, scope.ClassFilters);
        if (subjects.Count == 0)
        {
            return Array.Empty<DistanceInfo>();
        }

        return DistanceProjection.Build(subjects, scope.Graph);
    }

    /// <summary>
    /// Adds one integer measurement to the data map, its value formatted in the invariant culture.
    /// </summary>
    private static void Add(Dictionary<string, string> data, string metric, string subject, int value) =>
        data[$"{metric} [{subject}]"] = value.ToString(CultureInfo.InvariantCulture);

    /// <summary>
    /// Adds one fractional measurement to the data map, its value formatted in the invariant culture.
    /// </summary>
    private static void Add(Dictionary<string, string> data, string metric, string subject, double value) =>
        data[$"{metric} [{subject}]"] = value.ToString(CultureInfo.InvariantCulture);
}
