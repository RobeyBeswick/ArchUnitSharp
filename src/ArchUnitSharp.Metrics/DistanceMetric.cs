namespace ArchUnitSharp.Metrics;

/// <summary>
/// One distance metric: a <see cref="DistanceMetricKind"/> and the <see cref="MetricSubject"/> it
/// measures. The calculation layer turns a metric and a subject — always an extracted
/// <see cref="DistanceInfo"/>, so every distance metric carries the <see cref="MetricSubject.File"/>
/// subject — into the measured value.
/// </summary>
/// <remarks>
/// <para>
/// A metric is a value, not a rule: it names what is measured and nothing more. The rule's threshold
/// and comparison are supplied by the terminal that consumes it, so the same metric can back many
/// rules over the same scope. This type is immutable and value-semantic, so sharing a metric between
/// concurrent checks is safe.
/// </para>
/// </remarks>
public sealed record DistanceMetric
{
    /// <summary>
    /// Creates a distance metric of the given kind over file subjects. Internal: the calculation
    /// layer's factories are the only producers, and each pairs a file-level kind with a file subject,
    /// so every metric agrees with its kind.
    /// </summary>
    /// <param name="kind">The distance metric's kind.</param>
    /// <param name="subject">The kind of subject the metric measures.</param>
    internal DistanceMetric(DistanceMetricKind kind, MetricSubject subject)
    {
        Kind = kind;
        Subject = subject;
    }

    /// <summary>
    /// The distance metric's kind, naming what is measured.
    /// </summary>
    public DistanceMetricKind Kind { get; }

    /// <summary>
    /// The kind of subject the metric measures.
    /// </summary>
    public MetricSubject Subject { get; }
}
