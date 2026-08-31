namespace ArchUnitSharp.Metrics.Calculation;

using ArchUnitSharp.Metrics;

/// <summary>
/// The metrics module's pure count calculations: the seven count metrics and the value of one metric
/// over one subject. This is the one place a count metric's value is computed — a
/// <see cref="Metric"/> is a name and a subject kind, and the calculation turns the pair into the
/// measured number, so nothing downstream re-implements a count.
/// </summary>
/// <remarks>
/// <para>
/// Each factory returns the <see cref="Metric"/> the fluent surface exposes: the class-level
/// <c>method count</c> and <c>field count</c>, and the file-level <c>lines of code</c>,
/// <c>statements</c>, <c>imports</c>, <c>classes</c> and <c>interfaces</c>. The <c>functions</c>
/// file-level count the sibling implementations carry is absent, because C# has no concept of a
/// file-level function distinct from a type member — every method belongs to a class — and the issue's
/// rule is to skip a metric C# cannot express rather than fake it.
/// </para>
/// <para>
/// <see cref="ValueOf(Metric, FileInfo)"/> computes a file-level metric's value from an extracted
/// <see cref="FileInfo"/> and <see cref="ValueOf(Metric, ClassInfo)"/> a class-level metric's value
/// from an extracted <see cref="ClassInfo"/>; handing a metric the other subject kind is a caller
/// error and raises <see cref="ArgumentOutOfRangeException"/>. Values are pure counts of the info:
/// a file's lines of code is its <see cref="FileInfo.LinesOfCode"/>, a class's method count is the
/// number of its <see cref="ClassInfo.Methods"/>, and so on.
/// </para>
/// <para>
/// This type is stateless and safe for concurrent use.
/// </para>
/// </remarks>
internal static class CountMetrics
{
    /// <summary>
    /// The <c>method count</c> metric: the number of methods of one class. A class-level metric.
    /// </summary>
    public static Metric MethodCount() => new(CountMetricKind.MethodCount, MetricSubject.Class);

    /// <summary>
    /// The <c>field count</c> metric: the number of fields of one class. A class-level metric.
    /// </summary>
    public static Metric FieldCount() => new(CountMetricKind.FieldCount, MetricSubject.Class);

    /// <summary>
    /// The <c>lines of code</c> metric: the number of non-blank lines of one file. A file-level metric.
    /// </summary>
    public static Metric LinesOfCode() => new(CountMetricKind.LinesOfCode, MetricSubject.File);

    /// <summary>
    /// The <c>statements</c> metric: the number of statements of one file. A file-level metric.
    /// </summary>
    public static Metric Statements() => new(CountMetricKind.Statements, MetricSubject.File);

    /// <summary>
    /// The <c>imports</c> metric: the number of import directives of one file. A file-level metric.
    /// </summary>
    public static Metric Imports() => new(CountMetricKind.Imports, MetricSubject.File);

    /// <summary>
    /// The <c>classes</c> metric: the number of classes of one file. A file-level metric.
    /// </summary>
    public static Metric Classes() => new(CountMetricKind.Classes, MetricSubject.File);

    /// <summary>
    /// The <c>interfaces</c> metric: the number of interfaces of one file. A file-level metric.
    /// </summary>
    public static Metric Interfaces() => new(CountMetricKind.Interfaces, MetricSubject.File);

    /// <summary>
    /// Computes a file-level metric's value over one file: the file's own count fact for the metric's
    /// kind. A class-level metric is not a file metric and raises.
    /// </summary>
    /// <param name="metric">The metric to compute. Must not be <see langword="null"/>.</param>
    /// <param name="file">The file to measure. Must not be <see langword="null"/>.</param>
    /// <returns>The metric's value for the file.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="metric"/> or <paramref name="file"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="metric"/> is not a file-level metric.</exception>
    public static int ValueOf(Metric metric, FileInfo file)
    {
        ArgumentNullException.ThrowIfNull(metric);
        ArgumentNullException.ThrowIfNull(file);

        return metric.Kind switch
        {
            CountMetricKind.LinesOfCode => file.LinesOfCode,
            CountMetricKind.Statements => file.StatementCount,
            CountMetricKind.Imports => file.ImportCount,
            CountMetricKind.Classes => file.ClassCount,
            CountMetricKind.Interfaces => file.InterfaceCount,
            _ => throw new ArgumentOutOfRangeException(
                nameof(metric),
                metric.Kind,
                "Metric is not a file-level metric."),
        };
    }

    /// <summary>
    /// Computes a class-level metric's value over one class: the class's own count fact for the
    /// metric's kind. A file-level metric is not a class metric and raises.
    /// </summary>
    /// <param name="metric">The metric to compute. Must not be <see langword="null"/>.</param>
    /// <param name="classInfo">The class to measure. Must not be <see langword="null"/>.</param>
    /// <returns>The metric's value for the class.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="metric"/> or <paramref name="classInfo"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="metric"/> is not a class-level metric.</exception>
    public static int ValueOf(Metric metric, ClassInfo classInfo)
    {
        ArgumentNullException.ThrowIfNull(metric);
        ArgumentNullException.ThrowIfNull(classInfo);

        return metric.Kind switch
        {
            CountMetricKind.MethodCount => classInfo.Methods.Count,
            CountMetricKind.FieldCount => classInfo.Fields.Count,
            _ => throw new ArgumentOutOfRangeException(
                nameof(metric),
                metric.Kind,
                "Metric is not a class-level metric."),
        };
    }
}
