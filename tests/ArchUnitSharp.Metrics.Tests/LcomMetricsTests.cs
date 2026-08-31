using ArchUnitSharp.Common.Extraction;

namespace ArchUnitSharp.Metrics.Tests;

public class LcomMetricsTests
{
    [Fact]
    public void Lcom96a_returns_a_class_level_metric_selection()
    {
        LcomMetricSelection selection = new Metrics(Graph(Self("a.cs"))).Lcom().Lcom96a();

        Assert.Equal(LcomMetricKind.Lcom96a, selection.Metric.Kind);
        Assert.Equal(MetricSubject.Class, selection.Metric.Subject);
    }

    [Fact]
    public void Lcom96b_returns_a_class_level_metric_selection()
    {
        LcomMetricSelection selection = new Metrics(Graph(Self("a.cs"))).Lcom().Lcom96b();

        Assert.Equal(LcomMetricKind.Lcom96b, selection.Metric.Kind);
        Assert.Equal(MetricSubject.Class, selection.Metric.Subject);
    }

    [Fact]
    public void Lcom1_returns_a_class_level_metric_selection()
    {
        LcomMetricSelection selection = new Metrics(Graph(Self("a.cs"))).Lcom().Lcom1();

        Assert.Equal(LcomMetricKind.Lcom1, selection.Metric.Kind);
        Assert.Equal(MetricSubject.Class, selection.Metric.Subject);
    }

    [Fact]
    public void Lcom2_returns_a_class_level_metric_selection()
    {
        LcomMetricSelection selection = new Metrics(Graph(Self("a.cs"))).Lcom().Lcom2();

        Assert.Equal(LcomMetricKind.Lcom2, selection.Metric.Kind);
        Assert.Equal(MetricSubject.Class, selection.Metric.Subject);
    }

    [Fact]
    public void Lcom3_returns_a_class_level_metric_selection()
    {
        LcomMetricSelection selection = new Metrics(Graph(Self("a.cs"))).Lcom().Lcom3();

        Assert.Equal(LcomMetricKind.Lcom3, selection.Metric.Kind);
        Assert.Equal(MetricSubject.Class, selection.Metric.Subject);
    }

    [Fact]
    public void Lcom4_returns_a_class_level_metric_selection()
    {
        LcomMetricSelection selection = new Metrics(Graph(Self("a.cs"))).Lcom().Lcom4();

        Assert.Equal(LcomMetricKind.Lcom4, selection.Metric.Kind);
        Assert.Equal(MetricSubject.Class, selection.Metric.Subject);
    }

    [Fact]
    public void Lcom5_returns_a_class_level_metric_selection()
    {
        LcomMetricSelection selection = new Metrics(Graph(Self("a.cs"))).Lcom().Lcom5();

        Assert.Equal(LcomMetricKind.Lcom5, selection.Metric.Kind);
        Assert.Equal(MetricSubject.Class, selection.Metric.Subject);
    }

    [Fact]
    public void LcomStar_returns_a_class_level_metric_selection()
    {
        LcomMetricSelection selection = new Metrics(Graph(Self("a.cs"))).Lcom().LcomStar();

        Assert.Equal(LcomMetricKind.LcomStar, selection.Metric.Kind);
        Assert.Equal(MetricSubject.Class, selection.Metric.Subject);
    }

    [Fact]
    public void A_metric_method_leaves_the_scope_unchanged()
    {
        var scope = new Metrics(Graph(Self("a.cs"))).InFolder("src");

        LcomMetricSelection selection = scope.Lcom().Lcom4();

        Assert.Same(scope, selection.Metrics);
        Assert.Equal("project metrics in folder 'src'", scope.DescribeScope());
    }

    [Fact]
    public void ExportAsHtml_writes_the_lcom_reports_html_and_returns_the_path()
    {
        using var dir = new TempDir();
        const string split =
            "namespace App;\n" +
            "public class Split\n" +
            "{\n" +
            "    private int _a;\n" +
            "    private int _b;\n" +
            "    public void A() { _a = 1; }\n" +
            "    public void B() { _b = 2; }\n" +
            "}\n";
        string path = dir.File("lcom.html");
        var builder = new Metrics(Graph(Self("src/Split.cs")), _ => split).Lcom();

        string written = builder.ExportAsHtml(
            path,
            new MetricsExportOptions { IncludeTimestamp = false, Title = "Cohesion" });

        Assert.Equal(path, written);
        string html = File.ReadAllText(path);
        Assert.StartsWith("<!DOCTYPE html>", html);
        Assert.Contains("<title>Cohesion</title>", html);
        Assert.Contains("lcom96a [src/Split.cs:App.Split]", html);
        Assert.Contains("lcom4 [src/Split.cs:App.Split]", html);
        Assert.DoesNotContain("Generated:", html);
    }

    private static Graph Graph(params Edge[] edges) => new(edges);

    private static Edge Self(string file) => new(file, file, external: false, ImportKind.None);
}
