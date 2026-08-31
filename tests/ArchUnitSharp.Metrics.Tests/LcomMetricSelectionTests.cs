using ArchUnitSharp.Common.Extraction;

namespace ArchUnitSharp.Metrics.Tests;

public class LcomMetricSelectionTests
{
    [Fact]
    public void ShouldBeBelow_builds_a_below_rule()
    {
        LcomMetricRule rule = Rule(new Metrics(Graph(Self("a.cs"))).Lcom().Lcom96b().ShouldBeBelow(0.8));

        Assert.Equal(MetricComparison.Below, rule.Comparison);
        Assert.Equal(0.8, rule.Threshold);
        Assert.Null(rule.Predicate);
        Assert.Equal(MetricSubject.Class, rule.Metric.Subject);
    }

    [Fact]
    public void ShouldBeAbove_builds_an_above_rule()
    {
        LcomMetricRule rule = Rule(new Metrics(Graph(Self("a.cs"))).Lcom().Lcom1().ShouldBeAbove(2.0));

        Assert.Equal(MetricComparison.Above, rule.Comparison);
        Assert.Equal(2.0, rule.Threshold);
        Assert.Equal(MetricSubject.Class, rule.Metric.Subject);
    }

    [Fact]
    public void ShouldBe_builds_an_equal_rule()
    {
        LcomMetricRule rule = Rule(new Metrics(Graph(Self("a.cs"))).Lcom().Lcom4().ShouldBe(1));

        Assert.Equal(MetricComparison.Equal, rule.Comparison);
        Assert.Equal(1.0, rule.Threshold);
    }

    [Fact]
    public void ShouldBeBelowOrEqual_builds_a_below_or_equal_rule()
    {
        LcomMetricRule rule = Rule(new Metrics(Graph(Self("a.cs"))).Lcom().Lcom96a().ShouldBeBelowOrEqual(0.5));

        Assert.Equal(MetricComparison.BelowOrEqual, rule.Comparison);
        Assert.Equal(0.5, rule.Threshold);
    }

    [Fact]
    public void ShouldBeAboveOrEqual_builds_an_above_or_equal_rule()
    {
        LcomMetricRule rule = Rule(new Metrics(Graph(Self("a.cs"))).Lcom().Lcom5().ShouldBeAboveOrEqual(0.1));

        Assert.Equal(MetricComparison.AboveOrEqual, rule.Comparison);
        Assert.Equal(0.1, rule.Threshold);
    }

    [Fact]
    public void ShouldSatisfy_builds_a_predicate_rule()
    {
        LcomMetricRule rule = Rule(new Metrics(Graph(Self("a.cs")))
            .Lcom()
            .Lcom96b()
            .ShouldSatisfy(static value => value < 0.8, "cohesive classes"));

        Assert.NotNull(rule.Predicate);
        Assert.Equal("cohesive classes", rule.Message);
        Assert.Null(rule.Comparison);
        Assert.Null(rule.Threshold);
    }

    [Fact]
    public void A_threshold_method_leaves_the_scope_unchanged()
    {
        var scope = new Metrics(Graph(Self("a.cs"))).InFolder("src");

        Rule(scope.Lcom().Lcom4().ShouldBe(1));

        Assert.Equal("project metrics in folder 'src'", scope.DescribeScope());
    }

    [Fact]
    public void ShouldSatisfy_rejects_a_null_predicate()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new Metrics(Graph(Self("a.cs"))).Lcom().Lcom4().ShouldSatisfy(null!, "message"));
    }

    [Fact]
    public void ShouldSatisfy_rejects_a_null_message()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new Metrics(Graph(Self("a.cs"))).Lcom().Lcom4().ShouldSatisfy(static _ => true, null!));
    }

    [Fact]
    public void ShouldSatisfy_rejects_an_empty_message()
    {
        Assert.Throws<ArgumentException>(() =>
            new Metrics(Graph(Self("a.cs"))).Lcom().Lcom4().ShouldSatisfy(static _ => true, string.Empty));
    }

    private static LcomMetricRule Rule(ICheckable checkable) => Assert.IsType<LcomMetricRule>(checkable);

    private static Graph Graph(params Edge[] edges) => new(edges);

    private static Edge Self(string file) => new(file, file, external: false, ImportKind.None);
}
