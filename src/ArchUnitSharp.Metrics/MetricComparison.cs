namespace ArchUnitSharp.Metrics;

/// <summary>
/// The comparison of a count-metric rule: how a measured value relates to the rule's threshold. The
/// values are the fixed threshold vocabulary the library's rules may use — below, above, equal, below
/// or equal, above or equal — named so a rule reads as a sentence: <c>method count should be below
/// 20</c>. A <see cref="MetricViolation"/> carries the comparison of the rule that produced it, so a
/// report can say which way the value missed the threshold.
/// </summary>
public enum MetricComparison
{
    /// <summary>The value must be strictly below the threshold: <c>should be below</c>.</summary>
    Below,

    /// <summary>The value must be strictly above the threshold: <c>should be above</c>.</summary>
    Above,

    /// <summary>The value must equal the threshold: <c>should be</c>.</summary>
    Equal,

    /// <summary>The value must be below or equal to the threshold: <c>should be below or equal to</c>.</summary>
    BelowOrEqual,

    /// <summary>The value must be above or equal to the threshold: <c>should be above or equal to</c>.</summary>
    AboveOrEqual,
}
