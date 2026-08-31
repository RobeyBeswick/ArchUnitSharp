namespace ArchUnitSharp.Metrics;

using ArchUnitSharp.Common.Extraction;

/// <summary>
/// The terminal of a count-metric rule chain: the scope, the <see cref="Metric"/>, and either a
/// <see cref="MetricComparison"/> plus threshold or a custom predicate plus message. Checked with
/// <see cref="Check(CheckOptions?)"/>, which computes the subjects' measured values and reports each
/// subject that misses the rule as one <see cref="MetricViolation"/>.
/// </summary>
/// <remarks>
/// <para>
/// This type is built by a <see cref="MetricSelection"/> threshold method and is the only rule shape
/// the count section produces. Its two modes are exclusive: a threshold rule carries a comparison and
/// a threshold, a <c>should satisfy</c> rule carries a predicate and a message. The assertion layer
/// reads the mode from <see cref="Predicate"/> being non-<see langword="null"/>.
/// </para>
/// <para>
/// This type is immutable and safe for concurrent use: checking never mutates it, so one rule can be
/// checked concurrently or repeatedly.
/// </para>
/// </remarks>
internal sealed class MetricRule : ICheckable
{
    private readonly Metrics _metrics;
    private readonly Metric _metric;
    private readonly MetricComparison? _comparison;
    private readonly int? _threshold;
    private readonly Func<int, bool>? _predicate;
    private readonly string? _message;

    /// <summary>
    /// Creates a threshold rule over <paramref name="metrics"/> measuring <paramref name="metric"/>.
    /// </summary>
    internal MetricRule(Metrics metrics, Metric metric, MetricComparison comparison, int threshold)
    {
        _metrics = metrics;
        _metric = metric;
        _comparison = comparison;
        _threshold = threshold;
    }

    /// <summary>
    /// Creates a <c>should satisfy</c> rule over <paramref name="metrics"/> measuring
    /// <paramref name="metric"/> with the given predicate and message.
    /// </summary>
    internal MetricRule(Metrics metrics, Metric metric, Func<int, bool> predicate, string message)
    {
        _metrics = metrics;
        _metric = metric;
        _predicate = predicate;
        _message = message;
    }

    /// <summary>
    /// The scope the rule asserts over.
    /// </summary>
    internal Metrics Scope => _metrics;

    /// <summary>
    /// The metric the rule measures.
    /// </summary>
    internal Metric Metric => _metric;

    /// <summary>
    /// The rule's comparison, or <see langword="null"/> for a <c>should satisfy</c> rule.
    /// </summary>
    internal MetricComparison? Comparison => _comparison;

    /// <summary>
    /// The rule's threshold, or <see langword="null"/> for a <c>should satisfy</c> rule.
    /// </summary>
    internal int? Threshold => _threshold;

    /// <summary>
    /// The rule's predicate, or <see langword="null"/> for a threshold rule. Its being non-null is
    /// what tells the assertion the rule is a <c>should satisfy</c> rule.
    /// </summary>
    internal Func<int, bool>? Predicate => _predicate;

    /// <summary>
    /// The rule's message, or <see langword="null"/> for a threshold rule.
    /// </summary>
    internal string? Message => _message;

    /// <inheritdoc/>
    public IReadOnlyList<Violation> Check(CheckOptions? options = null) =>
        Assertion.MetricsAssertion.Check(this, options);

    /// <inheritdoc/>
    void ICheckable.ProhibitExternalImplementation()
    {
    }
}
