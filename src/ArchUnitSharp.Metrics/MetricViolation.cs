namespace ArchUnitSharp.Metrics;

using ArchUnitSharp.Common.Extraction;

/// <summary>
/// A violation produced by a count-metric rule: one subject — a file or a class — whose measured
/// metric value missed the rule's threshold or failed its predicate. Carries the data a report needs
/// and nothing else: which subject, which metric, what value it had, and what the rule required.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="File"/> is the file the subject belongs to, for both subject kinds. <see cref="Class"/>
/// is set for a class-level metric and carries the offending class's fully qualified name; it is
/// <see langword="null"/> for a file-level metric. A threshold rule — <c>should be below</c> and
/// friends — sets <see cref="Comparison"/> and <see cref="Threshold"/>; a <c>should satisfy</c> rule
/// sets <see cref="Message"/> instead. <see cref="Value"/> is the measured value in both shapes. It
/// carries <see cref="ViolationKind.Rule"/>, the same kind every rule predicate violation carries.
/// </para>
/// <para>
/// This type is immutable and value-semantic; two violations with the same data are equal.
/// </para>
/// </remarks>
public sealed record MetricViolation : Violation
{
    private readonly string _file;
    private readonly string? _class;

    /// <summary>
    /// The file the offending subject belongs to, for a file-level and a class-level metric alike.
    /// Must not be <see langword="null"/> or empty; both the constructor and a <see langword="with"/>
    /// expression route through the same validation, so neither can introduce a bad value.
    /// </summary>
    public string File
    {
        get => _file;
        init => _file = Require(value, nameof(File));
    }

    /// <summary>
    /// The offending class's fully qualified name for a class-level metric; <see langword="null"/> for
    /// a file-level metric.
    /// </summary>
    public string? Class
    {
        get => _class;
        init => _class = value;
    }

    /// <summary>
    /// The metric that was measured.
    /// </summary>
    public CountMetricKind MetricKind { get; }

    /// <summary>
    /// The measured value of the subject's metric.
    /// </summary>
    public int Value { get; }

    /// <summary>
    /// The comparison a threshold rule required, or <see langword="null"/> for a <c>should satisfy</c>
    /// rule.
    /// </summary>
    public MetricComparison? Comparison { get; init; }

    /// <summary>
    /// The threshold a threshold rule required, or <see langword="null"/> for a <c>should satisfy</c>
    /// rule.
    /// </summary>
    public int? Threshold { get; init; }

    /// <summary>
    /// The caller's description of a <c>should satisfy</c> rule's predicate, or <see langword="null"/>
    /// for a threshold rule.
    /// </summary>
    public string? Message { get; init; }

    /// <summary>
    /// Creates a violation for a subject whose value missed a threshold rule's comparison and
    /// threshold.
    /// </summary>
    /// <param name="file">The file the offending subject belongs to; must not be <see langword="null"/> or empty.</param>
    /// <param name="class">The offending class's name for a class-level metric, or <see langword="null"/> for a file-level metric.</param>
    /// <param name="metricKind">The metric that was measured.</param>
    /// <param name="value">The measured value.</param>
    /// <param name="comparison">The rule's comparison.</param>
    /// <param name="threshold">The rule's threshold.</param>
    /// <exception cref="ArgumentNullException"><paramref name="file"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="file"/> is empty.</exception>
    public MetricViolation(
        string file,
        string? @class,
        CountMetricKind metricKind,
        int value,
        MetricComparison comparison,
        int threshold)
        : base(ViolationKind.Rule)
    {
        _file = Require(file, nameof(File));
        _class = @class;
        MetricKind = metricKind;
        Value = value;
        Comparison = comparison;
        Threshold = threshold;
    }

    /// <summary>
    /// Creates a violation for a subject whose value failed a <c>should satisfy</c> rule's predicate.
    /// </summary>
    /// <param name="file">The file the offending subject belongs to; must not be <see langword="null"/> or empty.</param>
    /// <param name="class">The offending class's name for a class-level metric, or <see langword="null"/> for a file-level metric.</param>
    /// <param name="metricKind">The metric that was measured.</param>
    /// <param name="value">The measured value.</param>
    /// <param name="message">The caller's description of the predicate; must not be <see langword="null"/> or empty.</param>
    /// <exception cref="ArgumentNullException"><paramref name="file"/> or <paramref name="message"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="file"/> or <paramref name="message"/> is empty.</exception>
    public MetricViolation(
        string file,
        string? @class,
        CountMetricKind metricKind,
        int value,
        string message)
        : base(ViolationKind.Rule)
    {
        _file = Require(file, nameof(File));
        _class = @class;
        MetricKind = metricKind;
        Value = value;
        Message = Require(message, nameof(Message));
    }

    private static string Require(string value, string propertyName) =>
        value is null
            ? throw new ArgumentNullException(propertyName)
            : value.Length == 0
                ? throw new ArgumentException($"{propertyName} must not be empty.", propertyName)
                : value;
}
