namespace ArchUnitSharp.Metrics;

/// <summary>
/// One cohesion metric: a <see cref="LcomMetricKind"/> and the <see cref="MetricSubject"/> it measures.
/// The calculation layer turns a metric and a subject — always an extracted <see cref="ClassInfo"/>,
/// so every LCOM metric carries the <see cref="MetricSubject.Class"/> subject — into the measured
/// value.
/// </summary>
/// <remarks>
/// <para>
/// A metric is a value, not a rule: it names what is measured and nothing more. The rule's threshold
/// and comparison are supplied by the terminal that consumes it, so the same metric can back many
/// rules over the same scope. This type is immutable and value-semantic, so sharing a metric between
/// concurrent checks is safe.
/// </para>
/// </remarks>
public sealed record LcomMetric
{
    /// <summary>
    /// Creates a cohesion metric of the given kind over class subjects. Internal: the calculation
    /// layer's factories are the only producers, and each pairs a class-level kind with a class
    /// subject, so every metric agrees with its kind.
    /// </summary>
    /// <param name="kind">The cohesion metric's kind.</param>
    /// <param name="subject">The kind of subject the metric measures.</param>
    internal LcomMetric(LcomMetricKind kind, MetricSubject subject)
    {
        Kind = kind;
        Subject = subject;
    }

    /// <summary>
    /// The cohesion metric's kind, naming what is measured.
    /// </summary>
    public LcomMetricKind Kind { get; }

    /// <summary>
    /// The kind of subject the metric measures.
    /// </summary>
    public MetricSubject Subject { get; }
}
