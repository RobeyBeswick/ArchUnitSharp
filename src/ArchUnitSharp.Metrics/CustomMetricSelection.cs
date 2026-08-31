namespace ArchUnitSharp.Metrics;

using ArchUnitSharp.Common.Extraction;

/// <summary>
/// One custom metric over one metrics scope: the PREDICATE-OBJECT of a custom-metric rule chain. Built
/// from <see cref="Metrics.CustomMetric"/>; its threshold methods complete the rule and each returns
/// the terminal that is checked with <see cref="ICheckable.Check(CheckOptions?)"/>.
/// </summary>
/// <remarks>
/// <para>
/// This type is the metric selection, nothing else: it carries the scope and the <see cref="CustomMetric"/>,
/// and a threshold method forwards both to the rule terminal. The threshold vocabulary is the count
/// section's — a metric's value is required to be below, above, equal to, below or equal to, or above
/// or equal to a threshold — plus the custom selection's <see cref="ShouldSatisfy"/>, whose predicate
/// receives both the measured value and the <see cref="ClassInfo"/> it was measured from, so a rule
/// can reason about the class itself, not only the number. Every rule reads as a sentence: <c>custom
/// metric 'member count' should be below 20</c>. The negated mood has no metric analogue: a
/// comparison's negation is another comparison, not a separate rule shape.
/// </para>
/// <para>
/// This type is immutable and safe for concurrent use. Building a rule from it never mutates the scope
/// it was built from, so a <see cref="CustomMetricSelection"/> value can be stored and reused.
/// </para>
/// </remarks>
public sealed class CustomMetricSelection
{
    private readonly Metrics _metrics;
    private readonly CustomMetric _metric;

    /// <summary>
    /// Creates a selection of one custom metric over one scope. Callers obtain a
    /// <see cref="CustomMetricSelection"/> from <see cref="Metrics.CustomMetric"/> rather than
    /// constructing one.
    /// </summary>
    /// <param name="metrics">The scope the metric is selected over.</param>
    /// <param name="metric">The selected metric.</param>
    internal CustomMetricSelection(Metrics metrics, CustomMetric metric)
    {
        _metrics = metrics;
        _metric = metric;
    }

    /// <summary>
    /// The scope the metric is selected over. Internal: a rule terminal reads it to check the rule.
    /// </summary>
    internal Metrics Metrics => _metrics;

    /// <summary>
    /// The selected metric. Internal: a rule terminal reads it to compute the measured values.
    /// </summary>
    internal CustomMetric Metric => _metric;

    /// <summary>
    /// <c>should be below</c>: every class's metric value must be strictly below
    /// <paramref name="threshold"/>. A class whose value is not is reported as one
    /// <see cref="CustomMetricViolation"/>, and the empty-test guard reports a rule whose subjects
    /// matched nothing.
    /// </summary>
    /// <param name="threshold">The upper bound, exclusive.</param>
    /// <returns>The rule's terminal, checked with <see cref="ICheckable.Check(CheckOptions?)"/>.</returns>
    public ICheckable ShouldBeBelow(int threshold) =>
        new CustomMetricRule(_metrics, _metric, MetricComparison.Below, threshold);

    /// <summary>
    /// <c>should be above</c>: every class's metric value must be strictly above
    /// <paramref name="threshold"/>. A class whose value is not is reported as one
    /// <see cref="CustomMetricViolation"/>, and the empty-test guard reports a rule whose subjects
    /// matched nothing.
    /// </summary>
    /// <param name="threshold">The lower bound, exclusive.</param>
    /// <returns>The rule's terminal, checked with <see cref="ICheckable.Check(CheckOptions?)"/>.</returns>
    public ICheckable ShouldBeAbove(int threshold) =>
        new CustomMetricRule(_metrics, _metric, MetricComparison.Above, threshold);

    /// <summary>
    /// <c>should be</c>: every class's metric value must equal <paramref name="threshold"/>. A class
    /// whose value does not is reported as one <see cref="CustomMetricViolation"/>, and the empty-test
    /// guard reports a rule whose subjects matched nothing.
    /// </summary>
    /// <param name="threshold">The exact value required.</param>
    /// <returns>The rule's terminal, checked with <see cref="ICheckable.Check(CheckOptions?)"/>.</returns>
    public ICheckable ShouldBe(int threshold) =>
        new CustomMetricRule(_metrics, _metric, MetricComparison.Equal, threshold);

    /// <summary>
    /// <c>should be below or equal to</c>: every class's metric value must be below or equal to
    /// <paramref name="threshold"/>. A class whose value is not is reported as one
    /// <see cref="CustomMetricViolation"/>, and the empty-test guard reports a rule whose subjects
    /// matched nothing.
    /// </summary>
    /// <param name="threshold">The upper bound, inclusive.</param>
    /// <returns>The rule's terminal, checked with <see cref="ICheckable.Check(CheckOptions?)"/>.</returns>
    public ICheckable ShouldBeBelowOrEqual(int threshold) =>
        new CustomMetricRule(_metrics, _metric, MetricComparison.BelowOrEqual, threshold);

    /// <summary>
    /// <c>should be above or equal to</c>: every class's metric value must be above or equal to
    /// <paramref name="threshold"/>. A class whose value is not is reported as one
    /// <see cref="CustomMetricViolation"/>, and the empty-test guard reports a rule whose subjects
    /// matched nothing.
    /// </summary>
    /// <param name="threshold">The lower bound, inclusive.</param>
    /// <returns>The rule's terminal, checked with <see cref="ICheckable.Check(CheckOptions?)"/>.</returns>
    public ICheckable ShouldBeAboveOrEqual(int threshold) =>
        new CustomMetricRule(_metrics, _metric, MetricComparison.AboveOrEqual, threshold);

    /// <summary>
    /// <c>should satisfy</c>: every class's metric value must satisfy <paramref name="predicate"/>. The
    /// predicate receives one class's measured value and the class's full <see cref="ClassInfo"/>, and
    /// must return <see langword="true"/> for the class to pass. A class the predicate rejects is
    /// reported as one <see cref="CustomMetricViolation"/> carrying <paramref name="message"/>, and the
    /// empty-test guard reports a rule whose subjects matched nothing.
    /// </summary>
    /// <param name="predicate">The custom predicate over one measured value and its class; must not be <see langword="null"/>.</param>
    /// <param name="message">The rule's description, reported with each violation; must not be <see langword="null"/> or empty.</param>
    /// <returns>The rule's terminal, checked with <see cref="ICheckable.Check(CheckOptions?)"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="predicate"/> or <paramref name="message"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="message"/> is empty.</exception>
    public ICheckable ShouldSatisfy(Func<int, ClassInfo, bool> predicate, string message)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        if (message is null)
        {
            throw new ArgumentNullException(nameof(message));
        }

        if (message.Length == 0)
        {
            throw new ArgumentException("Message must not be empty.", nameof(message));
        }

        return new CustomMetricRule(_metrics, _metric, predicate, message);
    }
}
