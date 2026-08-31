namespace ArchUnitSharp.Metrics.Tests;

public class CustomMetricTests
{
    [Fact]
    public void The_metric_carries_its_name_and_description()
    {
        var metric = new CustomMetric("member count", "classes stay focused", static _ => 0);

        Assert.Equal("member count", metric.Name);
        Assert.Equal("classes stay focused", metric.Description);
    }

    [Fact]
    public void Calculate_invokes_the_calculation_with_the_full_class_info()
    {
        var metric = new CustomMetric(
            "member count",
            "classes stay focused",
            static info => info.Methods.Count + info.Fields.Count);

        var info = new ClassInfo(
            "App.Car",
            "src/Car.cs",
            new[] { new MethodInfo("Drive"), new MethodInfo("Stop") },
            new[] { new FieldInfo("_speed") });

        Assert.Equal(3, metric.Calculate(info));
    }

    [Fact]
    public void Two_metrics_that_share_a_calculation_are_equal()
    {
        Func<ClassInfo, int> calculation = static _ => 1;
        var first = new CustomMetric("member count", "classes stay focused", calculation);
        var second = new CustomMetric("member count", "classes stay focused", calculation);

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Two_metrics_that_differ_in_the_name_are_not_equal()
    {
        Func<ClassInfo, int> calculation = static _ => 1;
        var first = new CustomMetric("member count", "classes stay focused", calculation);
        var second = new CustomMetric("method count", "classes stay focused", calculation);

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void The_metric_rejects_a_null_name()
    {
        Assert.Throws<ArgumentNullException>(() => new CustomMetric(null!, "description", static _ => 1));
    }

    [Fact]
    public void The_metric_rejects_an_empty_name()
    {
        Assert.Throws<ArgumentException>(() => new CustomMetric(string.Empty, "description", static _ => 1));
    }

    [Fact]
    public void The_metric_rejects_a_null_description()
    {
        Assert.Throws<ArgumentNullException>(() => new CustomMetric("member count", null!, static _ => 1));
    }

    [Fact]
    public void The_metric_rejects_an_empty_description()
    {
        Assert.Throws<ArgumentException>(() => new CustomMetric("member count", string.Empty, static _ => 1));
    }

    [Fact]
    public void The_metric_rejects_a_null_calculation()
    {
        Assert.Throws<ArgumentNullException>(() => new CustomMetric("member count", "description", null!));
    }

    [Fact]
    public void Calculate_rejects_a_null_class_info()
    {
        var metric = new CustomMetric("member count", "description", static _ => 1);

        Assert.Throws<ArgumentNullException>(() => metric.Calculate(null!));
    }
}
