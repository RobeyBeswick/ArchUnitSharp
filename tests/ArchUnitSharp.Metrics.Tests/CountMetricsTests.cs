using ArchUnitSharp.Common.Extraction;

namespace ArchUnitSharp.Metrics.Tests;

public class CountMetricsTests
{
    [Fact]
    public void MethodCount_returns_a_class_level_metric_selection()
    {
        MetricSelection selection = new Metrics(Graph(Self("a.cs"))).Count().MethodCount();

        Assert.Equal(CountMetricKind.MethodCount, selection.Metric.Kind);
        Assert.Equal(MetricSubject.Class, selection.Metric.Subject);
    }

    [Fact]
    public void FieldCount_returns_a_class_level_metric_selection()
    {
        MetricSelection selection = new Metrics(Graph(Self("a.cs"))).Count().FieldCount();

        Assert.Equal(CountMetricKind.FieldCount, selection.Metric.Kind);
        Assert.Equal(MetricSubject.Class, selection.Metric.Subject);
    }

    [Fact]
    public void LinesOfCode_returns_a_file_level_metric_selection()
    {
        MetricSelection selection = new Metrics(Graph(Self("a.cs"))).Count().LinesOfCode();

        Assert.Equal(CountMetricKind.LinesOfCode, selection.Metric.Kind);
        Assert.Equal(MetricSubject.File, selection.Metric.Subject);
    }

    [Fact]
    public void Statements_returns_a_file_level_metric_selection()
    {
        MetricSelection selection = new Metrics(Graph(Self("a.cs"))).Count().Statements();

        Assert.Equal(CountMetricKind.Statements, selection.Metric.Kind);
        Assert.Equal(MetricSubject.File, selection.Metric.Subject);
    }

    [Fact]
    public void Imports_returns_a_file_level_metric_selection()
    {
        MetricSelection selection = new Metrics(Graph(Self("a.cs"))).Count().Imports();

        Assert.Equal(CountMetricKind.Imports, selection.Metric.Kind);
        Assert.Equal(MetricSubject.File, selection.Metric.Subject);
    }

    [Fact]
    public void Classes_returns_a_file_level_metric_selection()
    {
        MetricSelection selection = new Metrics(Graph(Self("a.cs"))).Count().Classes();

        Assert.Equal(CountMetricKind.Classes, selection.Metric.Kind);
        Assert.Equal(MetricSubject.File, selection.Metric.Subject);
    }

    [Fact]
    public void Interfaces_returns_a_file_level_metric_selection()
    {
        MetricSelection selection = new Metrics(Graph(Self("a.cs"))).Count().Interfaces();

        Assert.Equal(CountMetricKind.Interfaces, selection.Metric.Kind);
        Assert.Equal(MetricSubject.File, selection.Metric.Subject);
    }

    [Fact]
    public void A_metric_method_leaves_the_scope_unchanged()
    {
        var scope = new Metrics(Graph(Self("a.cs"))).InFolder("src");

        MetricSelection selection = scope.Count().LinesOfCode();

        Assert.Same(scope, selection.Metrics);
        Assert.Equal("project metrics in folder 'src'", scope.DescribeScope());
    }

    private static Graph Graph(params Edge[] edges) => new(edges);

    private static Edge Self(string file) => new(file, file, external: false, ImportKind.None);
}
