namespace ArchUnitSharp.Metrics;

using ArchUnitSharp.Common.Extraction;

/// <summary>
/// One cohesion metric over one metrics scope: the PREDICATE-OBJECT of a cohesion-metric rule chain.
/// Built from a <see cref="LcomMetrics"/> metric method; its threshold methods complete the rule and
/// each returns the terminal that is checked with <see cref="ICheckable.Check(CheckOptions?)"/>.
/// </summary>
/// <remarks>
/// <para>
/// This type is the metric selection, nothing else: it carries the scope and the <see cref="LcomMetric"/>,
/// and a threshold method forwards both to the rule terminal. The threshold vocabulary is fixed — a
/// metric's value is required to be below, above, equal to, below or equal to, or above or equal to a
/// threshold, or to satisfy a custom predicate — so every rule reads as a sentence: <c>lcom96b should
/// be below 0.3</c>. The negated mood has no metric analogue: a comparison's negation is another
/// comparison, not a separate rule shape.
/// </para>
/// <para>
/// This type is immutable and safe for concurrent use. Building a rule from it never mutates the scope
/// it was built from, so a <see cref="LcomMetricSelection"/> value can be stored and reused.
/// </para>
/// </remarks>
public sealed class LcomMetricSelection
{
    private readonly Metrics _metrics;
    private readonly LcomMetric _metric;

    /// <summary>
    /// Creates a selection of one cohesion metric over one scope. Callers obtain a
    /// <see cref="LcomMetricSelection"/> from a <see cref="LcomMetrics"/> metric method rather than
    /// constructing one.
    /// </summary>
    /// <param name="metrics">The scope the metric is selected over.</param>
    /// <param name="metric">The selected metric.</param>
    internal LcomMetricSelection(Metrics metrics, LcomMetric metric)
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
    internal LcomMetric Metric => _metric;

    /// <summary>
    /// <c>should be below</c>: every subject's metric value must be strictly below
    /// <paramref name="threshold"/>. A subject whose value is not is reported as one
    /// <see cref="LcomMetricViolation"/>, and the empty-test guard reports a rule whose subjects matched
    /// nothing.
    /// </summary>
    /// <param name="threshold">The upper bound, exclusive.</param>
    /// <returns>The rule's terminal, checked with <see cref="ICheckable.Check(CheckOptions?)"/>.</returns>
    public ICheckable ShouldBeBelow(double threshold) =>
        new LcomMetricRule(_metrics, _metric, MetricComparison.Below, threshold);

    /// <summary>
    /// <c>should be above</c>: every subject's metric value must be strictly above
    /// <paramref name="threshold"/>. A subject whose value is not is reported as one
    /// <see cref="LcomMetricViolation"/>, and the empty-test guard reports a rule whose subjects matched
    /// nothing.
    /// </summary>
    /// <param name="threshold">The lower bound, exclusive.</param>
    /// <returns>The rule's terminal, checked with <see cref="ICheckable.Check(CheckOptions?)"/>.</returns>
    public ICheckable ShouldBeAbove(double threshold) =>
        new LcomMetricRule(_metrics, _metric, MetricComparison.Above, threshold);

    /// <summary>
    /// <c>should be</c>: every subject's metric value must equal <paramref name="threshold"/>. A
    /// subject whose value does not is reported as one <see cref="LcomMetricViolation"/>, and the
    /// empty-test guard reports a rule whose subjects matched nothing.
    /// </summary>
    /// <param name="threshold">The exact value required.</param>
    /// <returns>The rule's terminal, checked with <see cref="ICheckable.Check(CheckOptions?)"/>.</returns>
    public ICheckable ShouldBe(double threshold) =>
        new LcomMetricRule(_metrics, _metric, MetricComparison.Equal, threshold);

    /// <summary>
    /// <c>should be below or equal to</c>: every subject's metric value must be below or equal to
    /// <paramref name="threshold"/>. A subject whose value is not is reported as one
    /// <see cref="LcomMetricViolation"/>, and the empty-test guard reports a rule whose subjects matched
    /// nothing.
    /// </summary>
    /// <param name="threshold">The upper bound, inclusive.</param>
    /// <returns>The rule's terminal, checked with <see cref="ICheckable.Check(CheckOptions?)"/>.</returns>
    public ICheckable ShouldBeBelowOrEqual(double threshold) =>
        new LcomMetricRule(_metrics, _metric, MetricComparison.BelowOrEqual, threshold);

    /// <summary>
    /// <c>should be above or equal to</c>: every subject's metric value must be above or equal to
    /// <paramref name="threshold"/>. A subject whose value is not is reported as one
    /// <see cref="LcomMetricViolation"/>, and the empty-test guard reports a rule whose subjects matched
    /// nothing.
    /// </summary>
    /// <param name="threshold">The lower bound, inclusive.</param>
    /// <returns>The rule's terminal, checked with <see cref="ICheckable.Check(CheckOptions?)"/>.</returns>
    public ICheckable ShouldBeAboveOrEqual(double threshold) =>
        new LcomMetricRule(_metrics, _metric, MetricComparison.AboveOrEqual, threshold);

    /// <summary>
    /// <c>should satisfy</c>: every subject's metric value must satisfy <paramref name="predicate"/>.
    /// The predicate receives one subject's measured value and must return <see langword="true"/> for
    /// the subject to pass. A subject whose value the predicate rejects is reported as one
    /// <see cref="LcomMetricViolation"/> carrying <paramref name="message"/>, and the empty-test guard
    /// reports a rule whose subjects matched nothing.
    /// </summary>
    /// <param name="predicate">The custom predicate over one measured value; must not be <see langword="null"/>.</param>
    /// <param name="message">The rule's description, reported with each violation; must not be <see langword="null"/> or empty.</param>
    /// <returns>The rule's terminal, checked with <see cref="ICheckable.Check(CheckOptions?)"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="predicate"/> or <paramref name="message"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="message"/> is empty.</exception>
    public ICheckable ShouldSatisfy(Func<double, bool> predicate, string message)
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

        return new LcomMetricRule(_metrics, _metric, predicate, message);
    }
}
