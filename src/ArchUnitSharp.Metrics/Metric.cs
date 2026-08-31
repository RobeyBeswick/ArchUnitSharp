namespace ArchUnitSharp.Metrics;

/// <summary>
/// One count metric: a <see cref="CountMetricKind"/> and the <see cref="MetricSubject"/> it measures.
/// The calculation layer turns a metric and a subject — an extracted <see cref="ClassInfo"/> for a
/// class metric, an extracted <see cref="FileInfo"/> for a file metric — into the measured value.
/// </summary>
/// <remarks>
/// <para>
/// A metric is a value, not a rule: it names what is counted and nothing more. The rule's threshold
/// and comparison are supplied by the terminal that consumes it, so the same metric can back many
/// rules over the same scope. This type is immutable and value-semantic, so sharing a metric between
/// concurrent checks is safe.
/// </para>
/// </remarks>
public sealed record Metric
{
    /// <summary>
    /// Creates a metric of the given kind over subjects of the given kind. Internal: the calculation
    /// layer's factories are the only producers, and each pairs a class-level kind with a class
    /// subject and a file-level kind with a file subject, so every metric agrees with its kind — a
    /// <see cref="CountMetricKind.MethodCount"/> metric is always a <see cref="MetricSubject.Class"/>
    /// metric.
    /// </summary>
    /// <param name="kind">The count metric's kind.</param>
    /// <param name="subject">The kind of subject the metric measures.</param>
    internal Metric(CountMetricKind kind, MetricSubject subject)
    {
        Kind = kind;
        Subject = subject;
    }

    /// <summary>
    /// The count metric's kind, naming what is counted.
    /// </summary>
    public CountMetricKind Kind { get; }

    /// <summary>
    /// The kind of subject the metric measures.
    /// </summary>
    public MetricSubject Subject { get; }
}
