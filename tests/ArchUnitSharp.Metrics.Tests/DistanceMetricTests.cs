namespace ArchUnitSharp.Metrics.Tests;

public class DistanceMetricTests
{
    [Fact]
    public void The_metric_carries_its_kind_and_subject()
    {
        var metric = new DistanceMetric(DistanceMetricKind.Instability, MetricSubject.File);

        Assert.Equal(DistanceMetricKind.Instability, metric.Kind);
        Assert.Equal(MetricSubject.File, metric.Subject);
    }

    [Fact]
    public void Two_metrics_with_the_same_kind_and_subject_are_equal()
    {
        var first = new DistanceMetric(DistanceMetricKind.Abstractness, MetricSubject.File);
        var second = new DistanceMetric(DistanceMetricKind.Abstractness, MetricSubject.File);

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }
}
