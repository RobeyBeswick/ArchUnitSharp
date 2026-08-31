using ArchUnitSharp.Common.Extraction;

namespace ArchUnitSharp.Metrics.Tests;

public class MetricSelectionTests
{
    [Fact]
    public void ShouldBeBelow_builds_a_below_rule()
    {
        MetricRule rule = Rule(new Metrics(Graph(Self("a.cs"))).Count().MethodCount().ShouldBeBelow(20));

        Assert.Equal(MetricComparison.Below, rule.Comparison);
        Assert.Equal(20, rule.Threshold);
        Assert.Null(rule.Predicate);
        Assert.Equal(MetricSubject.Class, rule.Metric.Subject);
    }

    [Fact]
    public void ShouldBeAbove_builds_an_above_rule()
    {
        MetricRule rule = Rule(new Metrics(Graph(Self("a.cs"))).Count().LinesOfCode().ShouldBeAbove(100));

        Assert.Equal(MetricComparison.Above, rule.Comparison);
        Assert.Equal(100, rule.Threshold);
        Assert.Equal(MetricSubject.File, rule.Metric.Subject);
    }

    [Fact]
    public void ShouldBe_builds_an_equal_rule()
    {
        MetricRule rule = Rule(new Metrics(Graph(Self("a.cs"))).Count().Imports().ShouldBe(3));

        Assert.Equal(MetricComparison.Equal, rule.Comparison);
        Assert.Equal(3, rule.Threshold);
    }

    [Fact]
    public void ShouldBeBelowOrEqual_builds_a_below_or_equal_rule()
    {
        MetricRule rule = Rule(new Metrics(Graph(Self("a.cs"))).Count().Statements().ShouldBeBelowOrEqual(50));

        Assert.Equal(MetricComparison.BelowOrEqual, rule.Comparison);
        Assert.Equal(50, rule.Threshold);
    }

    [Fact]
    public void ShouldBeAboveOrEqual_builds_an_above_or_equal_rule()
    {
        MetricRule rule = Rule(new Metrics(Graph(Self("a.cs"))).Count().Classes().ShouldBeAboveOrEqual(1));

        Assert.Equal(MetricComparison.AboveOrEqual, rule.Comparison);
        Assert.Equal(1, rule.Threshold);
    }

    [Fact]
    public void ShouldSatisfy_builds_a_predicate_rule()
    {
        MetricRule rule = Rule(new Metrics(Graph(Self("a.cs")))
            .Count()
            .MethodCount()
            .ShouldSatisfy(static value => value < 20, "few methods"));

        Assert.NotNull(rule.Predicate);
        Assert.Equal("few methods", rule.Message);
        Assert.Null(rule.Comparison);
        Assert.Null(rule.Threshold);
    }

    [Fact]
    public void A_threshold_method_leaves_the_scope_unchanged()
    {
        var scope = new Metrics(Graph(Self("a.cs"))).InFolder("src");

        Rule(scope.Count().LinesOfCode().ShouldBeBelow(100));

        Assert.Equal("project metrics in folder 'src'", scope.DescribeScope());
    }

    [Fact]
    public void ShouldSatisfy_rejects_a_null_predicate()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new Metrics(Graph(Self("a.cs"))).Count().MethodCount().ShouldSatisfy(null!, "message"));
    }

    [Fact]
    public void ShouldSatisfy_rejects_a_null_message()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new Metrics(Graph(Self("a.cs"))).Count().MethodCount().ShouldSatisfy(static _ => true, null!));
    }

    [Fact]
    public void ShouldSatisfy_rejects_an_empty_message()
    {
        Assert.Throws<ArgumentException>(() =>
            new Metrics(Graph(Self("a.cs"))).Count().MethodCount().ShouldSatisfy(static _ => true, string.Empty));
    }

    private static MetricRule Rule(ICheckable checkable) => Assert.IsType<MetricRule>(checkable);

    private static Graph Graph(params Edge[] edges) => new(edges);

    private static Edge Self(string file) => new(file, file, external: false, ImportKind.None);
}
