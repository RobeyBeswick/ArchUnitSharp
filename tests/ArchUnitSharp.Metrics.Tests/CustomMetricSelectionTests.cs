using ArchUnitSharp.Common.Extraction;

namespace ArchUnitSharp.Metrics.Tests;

public class CustomMetricSelectionTests
{
    [Fact]
    public void ShouldBeBelow_builds_a_below_rule()
    {
        CustomMetricRule rule = Rule(Selection().ShouldBeBelow(20));

        Assert.Equal(MetricComparison.Below, rule.Comparison);
        Assert.Equal(20, rule.Threshold);
        Assert.Null(rule.Predicate);
    }

    [Fact]
    public void ShouldBeAbove_builds_an_above_rule()
    {
        CustomMetricRule rule = Rule(Selection().ShouldBeAbove(100));

        Assert.Equal(MetricComparison.Above, rule.Comparison);
        Assert.Equal(100, rule.Threshold);
        Assert.Null(rule.Predicate);
    }

    [Fact]
    public void ShouldBe_builds_an_equal_rule()
    {
        CustomMetricRule rule = Rule(Selection().ShouldBe(3));

        Assert.Equal(MetricComparison.Equal, rule.Comparison);
        Assert.Equal(3, rule.Threshold);
    }

    [Fact]
    public void ShouldBeBelowOrEqual_builds_a_below_or_equal_rule()
    {
        CustomMetricRule rule = Rule(Selection().ShouldBeBelowOrEqual(50));

        Assert.Equal(MetricComparison.BelowOrEqual, rule.Comparison);
        Assert.Equal(50, rule.Threshold);
    }

    [Fact]
    public void ShouldBeAboveOrEqual_builds_an_above_or_equal_rule()
    {
        CustomMetricRule rule = Rule(Selection().ShouldBeAboveOrEqual(1));

        Assert.Equal(MetricComparison.AboveOrEqual, rule.Comparison);
        Assert.Equal(1, rule.Threshold);
    }

    [Fact]
    public void ShouldSatisfy_builds_a_predicate_rule()
    {
        CustomMetricRule rule = Rule(Selection().ShouldSatisfy(
            static (value, info) => value < 20 && info.Name.StartsWith("App"),
            "focused classes"));

        Assert.NotNull(rule.Predicate);
        Assert.Equal("focused classes", rule.Message);
        Assert.Null(rule.Comparison);
        Assert.Null(rule.Threshold);
    }

    [Fact]
    public void Every_rule_carries_the_selection_scope_and_metric()
    {
        var scope = new Metrics(Graph(Self("a.cs"))).InFolder("src");
        var selection = scope.CustomMetric("member count", "d", static _ => 0);

        CustomMetricRule rule = Rule(selection.ShouldBeBelow(20));

        Assert.Same(scope, rule.Scope);
        Assert.Same(selection.Metric, rule.Metric);
    }

    [Fact]
    public void A_threshold_method_leaves_the_scope_unchanged()
    {
        var scope = new Metrics(Graph(Self("a.cs"))).InFolder("src");

        Rule(scope.CustomMetric("member count", "d", static _ => 0).ShouldBeBelow(100));

        Assert.Equal("project metrics in folder 'src'", scope.DescribeScope());
    }

    [Fact]
    public void ShouldSatisfy_rejects_a_null_predicate()
    {
        Assert.Throws<ArgumentNullException>(() => Selection().ShouldSatisfy(null!, "message"));
    }

    [Fact]
    public void ShouldSatisfy_rejects_a_null_message()
    {
        Assert.Throws<ArgumentNullException>(() =>
            Selection().ShouldSatisfy(static (value, info) => value > 0, null!));
    }

    [Fact]
    public void ShouldSatisfy_rejects_an_empty_message()
    {
        Assert.Throws<ArgumentException>(() =>
            Selection().ShouldSatisfy(static (value, info) => value > 0, string.Empty));
    }

    private static CustomMetricSelection Selection() =>
        new Metrics(Graph(Self("a.cs"))).CustomMetric("member count", "d", static _ => 0);

    private static CustomMetricRule Rule(ICheckable checkable) => Assert.IsType<CustomMetricRule>(checkable);

    private static Graph Graph(params Edge[] edges) => new(edges);

    private static Edge Self(string file) => new(file, file, external: false, ImportKind.None);
}
