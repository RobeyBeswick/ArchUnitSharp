namespace ArchUnitSharp.Metrics;

using ArchUnitSharp.Common.Extraction;

/// <summary>
/// A violation produced by a custom-metric rule: one class whose measured metric value missed the
/// rule's threshold or failed its predicate. Carries the data a report needs and nothing else: which
/// subject, which metric and its description, what value it had, and what the rule required.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="File"/> is the file the offending class belongs to and <see cref="Class"/> its fully
/// qualified name. <see cref="MetricName"/> and <see cref="Description"/> name the metric in the
/// caller's own words, unlike a built-in metric's violation, which carries a kind. A threshold rule —
/// <c>should be below</c> and friends — sets <see cref="Comparison"/> and <see cref="Threshold"/>; a
/// <c>should satisfy</c> rule sets <see cref="Message"/> instead. <see cref="Value"/> is the measured
/// value in both shapes. It carries <see cref="ViolationKind.Rule"/>, the same kind every rule
/// predicate violation carries.
/// </para>
/// <para>
/// This type is immutable and value-semantic; two violations with the same data are equal.
/// </para>
/// </remarks>
public sealed record CustomMetricViolation : Violation
{
    private readonly string _file;
    private readonly string _class;
    private readonly string _metricName;
    private readonly string _description;

    /// <summary>
    /// The file the offending class belongs to. Must not be <see langword="null"/> or empty; both the
    /// constructor and a <see langword="with"/> expression route through the same validation, so
    /// neither can introduce a bad value.
    /// </summary>
    public string File
    {
        get => _file;
        init => _file = Require(value, nameof(File));
    }

    /// <summary>
    /// The offending class's fully qualified name. Must not be <see langword="null"/> or empty; both
    /// the constructor and a <see langword="with"/> expression route through the same validation, so
    /// neither can introduce a bad value.
    /// </summary>
    public string Class
    {
        get => _class;
        init => _class = Require(value, nameof(Class));
    }

    /// <summary>
    /// The custom metric's name, as the caller named it. Must not be <see langword="null"/> or empty;
    /// both the constructor and a <see langword="with"/> expression route through the same validation,
    /// so neither can introduce a bad value.
    /// </summary>
    public string MetricName
    {
        get => _metricName;
        init => _metricName = Require(value, nameof(MetricName));
    }

    /// <summary>
    /// The custom metric's description, as the caller wrote it. Must not be <see langword="null"/> or
    /// empty; both the constructor and a <see langword="with"/> expression route through the same
    /// validation, so neither can introduce a bad value.
    /// </summary>
    public string Description
    {
        get => _description;
        init => _description = Require(value, nameof(Description));
    }

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
    /// Creates a violation for a class whose value missed a threshold rule's comparison and threshold.
    /// </summary>
    /// <param name="file">The file the offending class belongs to; must not be <see langword="null"/> or empty.</param>
    /// <param name="class">The offending class's fully qualified name; must not be <see langword="null"/> or empty.</param>
    /// <param name="metricName">The custom metric's name; must not be <see langword="null"/> or empty.</param>
    /// <param name="description">The custom metric's description; must not be <see langword="null"/> or empty.</param>
    /// <param name="value">The measured value.</param>
    /// <param name="comparison">The rule's comparison.</param>
    /// <param name="threshold">The rule's threshold.</param>
    /// <exception cref="ArgumentNullException"><paramref name="file"/>, <paramref name="class"/>, <paramref name="metricName"/> or <paramref name="description"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="file"/>, <paramref name="class"/>, <paramref name="metricName"/> or <paramref name="description"/> is empty.</exception>
    public CustomMetricViolation(
        string file,
        string @class,
        string metricName,
        string description,
        int value,
        MetricComparison comparison,
        int threshold)
        : base(ViolationKind.Rule)
    {
        _file = Require(file, nameof(File));
        _class = Require(@class, nameof(Class));
        _metricName = Require(metricName, nameof(MetricName));
        _description = Require(description, nameof(Description));
        Value = value;
        Comparison = comparison;
        Threshold = threshold;
    }

    /// <summary>
    /// Creates a violation for a class whose value failed a <c>should satisfy</c> rule's predicate.
    /// </summary>
    /// <param name="file">The file the offending class belongs to; must not be <see langword="null"/> or empty.</param>
    /// <param name="class">The offending class's fully qualified name; must not be <see langword="null"/> or empty.</param>
    /// <param name="metricName">The custom metric's name; must not be <see langword="null"/> or empty.</param>
    /// <param name="description">The custom metric's description; must not be <see langword="null"/> or empty.</param>
    /// <param name="value">The measured value.</param>
    /// <param name="message">The caller's description of the predicate; must not be <see langword="null"/> or empty.</param>
    /// <exception cref="ArgumentNullException"><paramref name="file"/>, <paramref name="class"/>, <paramref name="metricName"/>, <paramref name="description"/> or <paramref name="message"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="file"/>, <paramref name="class"/>, <paramref name="metricName"/>, <paramref name="description"/> or <paramref name="message"/> is empty.</exception>
    public CustomMetricViolation(
        string file,
        string @class,
        string metricName,
        string description,
        int value,
        string message)
        : base(ViolationKind.Rule)
    {
        _file = Require(file, nameof(File));
        _class = Require(@class, nameof(Class));
        _metricName = Require(metricName, nameof(MetricName));
        _description = Require(description, nameof(Description));
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
