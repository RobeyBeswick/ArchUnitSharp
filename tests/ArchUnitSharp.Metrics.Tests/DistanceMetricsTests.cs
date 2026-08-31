using ArchUnitSharp.Common.Extraction;

namespace ArchUnitSharp.Metrics.Tests;

public class DistanceMetricsTests
{
    [Fact]
    public void Distance_returns_a_distance_section_over_this_scope()
    {
        var metrics = new Metrics(Graph(Self("a.cs"))).InFolder("src");

        DistanceMetrics distance = metrics.Distance();

        Assert.Same(metrics, distance.Metrics);
    }

    [Fact]
    public void Abstractness_returns_a_file_level_metric_selection()
    {
        DistanceMetricSelection selection = new Metrics(Graph(Self("a.cs"))).Distance().Abstractness();

        Assert.Equal(DistanceMetricKind.Abstractness, selection.Metric.Kind);
        Assert.Equal(MetricSubject.File, selection.Metric.Subject);
    }

    [Fact]
    public void Instability_returns_a_file_level_metric_selection()
    {
        DistanceMetricSelection selection = new Metrics(Graph(Self("a.cs"))).Distance().Instability();

        Assert.Equal(DistanceMetricKind.Instability, selection.Metric.Kind);
        Assert.Equal(MetricSubject.File, selection.Metric.Subject);
    }

    [Fact]
    public void DistanceFromMainSequence_returns_a_file_level_metric_selection()
    {
        DistanceMetricSelection selection = new Metrics(Graph(Self("a.cs"))).Distance().DistanceFromMainSequence();

        Assert.Equal(DistanceMetricKind.DistanceFromMainSequence, selection.Metric.Kind);
        Assert.Equal(MetricSubject.File, selection.Metric.Subject);
    }

    [Fact]
    public void CouplingFactor_returns_a_file_level_metric_selection()
    {
        DistanceMetricSelection selection = new Metrics(Graph(Self("a.cs"))).Distance().CouplingFactor();

        Assert.Equal(DistanceMetricKind.CouplingFactor, selection.Metric.Kind);
        Assert.Equal(MetricSubject.File, selection.Metric.Subject);
    }

    [Fact]
    public void NormalisedDistance_returns_a_file_level_metric_selection()
    {
        DistanceMetricSelection selection = new Metrics(Graph(Self("a.cs"))).Distance().NormalisedDistance();

        Assert.Equal(DistanceMetricKind.NormalisedDistance, selection.Metric.Kind);
        Assert.Equal(MetricSubject.File, selection.Metric.Subject);
    }

    [Fact]
    public void NotInZoneOfPain_returns_the_pain_zone_rule()
    {
        var rule = (DistanceZoneRule)new Metrics(Graph(Self("a.cs"))).Distance().NotInZoneOfPain();

        Assert.Equal(DistanceZone.Pain, rule.Zone);
    }

    [Fact]
    public void NotInZoneOfUselessness_returns_the_uselessness_zone_rule()
    {
        var rule = (DistanceZoneRule)new Metrics(Graph(Self("a.cs"))).Distance().NotInZoneOfUselessness();

        Assert.Equal(DistanceZone.Uselessness, rule.Zone);
    }

    [Fact]
    public void A_section_method_leaves_the_scope_unchanged()
    {
        var scope = new Metrics(Graph(Self("a.cs"))).InFolder("src");

        DistanceMetricSelection selection = scope.Distance().Instability();

        Assert.Same(scope, selection.Metrics);
        Assert.Equal("project metrics in folder 'src'", scope.DescribeScope());
    }

    [Fact]
    public void A_zone_method_leaves_the_scope_unchanged()
    {
        var scope = new Metrics(Graph(Self("a.cs"))).InFolder("src");

        var rule = (DistanceZoneRule)scope.Distance().NotInZoneOfPain();

        Assert.Same(scope, rule.Scope);
        Assert.Equal("project metrics in folder 'src'", scope.DescribeScope());
    }

    [Fact]
    public void A_metric_method_leaves_the_parent_scope_unchanged()
    {
        var parent = new Metrics(Graph(Self("a.cs")));

        DistanceMetrics distance = parent.Distance();

        Assert.Equal("project metrics", parent.DescribeScope());
        Assert.Equal("project metrics", distance.Metrics.DescribeScope());
    }

    [Fact]
    public void ExportAsHtml_writes_the_distance_reports_html_and_returns_the_path()
    {
        using var dir = new TempDir();
        var sources = new Dictionary<string, string>
        {
            ["src/Models/IThing.cs"] = "namespace App;\npublic interface IThing { }\n",
            ["src/Services/Car.cs"] = "namespace App;\npublic class Car : IThing { }\n",
        };
        var graph = Graph(
            Self("src/Models/IThing.cs"),
            Self("src/Services/Car.cs"),
            Using("src/Services/Car.cs", "src/Models/IThing.cs"));
        string path = dir.File("distance.html");
        var builder = new Metrics(graph, identifier => sources[identifier]).Distance();

        string written = builder.ExportAsHtml(
            path,
            new MetricsExportOptions { IncludeTimestamp = false, Title = "Distance" });

        Assert.Equal(path, written);
        string html = File.ReadAllText(path);
        Assert.StartsWith("<!DOCTYPE html>", html);
        Assert.Contains("<title>Distance</title>", html);
        Assert.Contains("abstractness [src/Services/Car.cs]", html);
        Assert.Contains("instability [src/Services/Car.cs]", html);
        Assert.DoesNotContain("Generated:", html);
    }

    private static Graph Graph(params Edge[] edges) => new(edges);

    private static Edge Self(string file) => new(file, file, external: false, ImportKind.None);

    private static Edge Using(string source, string target) =>
        new(source, target, external: false, ImportKind.Using);
}
