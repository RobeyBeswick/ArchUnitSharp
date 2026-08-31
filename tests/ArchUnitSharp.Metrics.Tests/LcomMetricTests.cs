namespace ArchUnitSharp.Metrics.Tests;

public class LcomMetricTests
{
    [Fact]
    public void The_metric_carries_its_kind_and_subject()
    {
        var metric = new LcomMetric(LcomMetricKind.Lcom4, MetricSubject.Class);

        Assert.Equal(LcomMetricKind.Lcom4, metric.Kind);
        Assert.Equal(MetricSubject.Class, metric.Subject);
    }

    [Fact]
    public void Two_metrics_with_the_same_kind_and_subject_are_equal()
    {
        var first = new LcomMetric(LcomMetricKind.Lcom96b, MetricSubject.Class);
        var second = new LcomMetric(LcomMetricKind.Lcom96b, MetricSubject.Class);

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }
}
