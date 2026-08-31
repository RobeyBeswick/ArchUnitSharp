namespace ArchUnitSharp.Metrics.Tests;

public class LcomMetricViolationTests
{
    [Fact]
    public void The_threshold_shape_carries_the_rules_data()
    {
        var violation = new LcomMetricViolation(
            "src/Classes.cs",
            "App.Big",
            LcomMetricKind.Lcom4,
            value: 2.0,
            MetricComparison.Below,
            threshold: 1.0);

        Assert.Equal("src/Classes.cs", violation.File);
        Assert.Equal("App.Big", violation.Class);
        Assert.Equal(LcomMetricKind.Lcom4, violation.MetricKind);
        Assert.Equal(2.0, violation.Value);
        Assert.Equal(MetricComparison.Below, violation.Comparison);
        Assert.Equal(1.0, violation.Threshold);
        Assert.Null(violation.Message);
    }

    [Fact]
    public void The_satisfy_shape_carries_the_rules_message()
    {
        var violation = new LcomMetricViolation(
            "src/A.cs",
            "App.Car",
            LcomMetricKind.Lcom96b,
            value: 0.5,
            "every class is cohesive");

        Assert.Equal("src/A.cs", violation.File);
        Assert.Equal("App.Car", violation.Class);
        Assert.Equal(LcomMetricKind.Lcom96b, violation.MetricKind);
        Assert.Equal(0.5, violation.Value);
        Assert.Equal("every class is cohesive", violation.Message);
        Assert.Null(violation.Comparison);
        Assert.Null(violation.Threshold);
    }

    [Fact]
    public void Two_violations_with_the_same_data_are_equal()
    {
        var first = new LcomMetricViolation("src/A.cs", "App.Car", LcomMetricKind.Lcom96a, 0.5, MetricComparison.Equal, 1.0);
        var second = new LcomMetricViolation("src/A.cs", "App.Car", LcomMetricKind.Lcom96a, 0.5, MetricComparison.Equal, 1.0);

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void The_threshold_shape_rejects_a_null_file()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new LcomMetricViolation(null!, "App.Car", LcomMetricKind.Lcom1, 1.0, MetricComparison.Equal, 1.0));
    }

    [Fact]
    public void The_threshold_shape_rejects_an_empty_file()
    {
        Assert.Throws<ArgumentException>(() =>
            new LcomMetricViolation(string.Empty, "App.Car", LcomMetricKind.Lcom1, 1.0, MetricComparison.Equal, 1.0));
    }

    [Fact]
    public void The_satisfy_shape_rejects_a_null_file()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new LcomMetricViolation(null!, "App.Car", LcomMetricKind.Lcom1, 1.0, "message"));
    }

    [Fact]
    public void The_satisfy_shape_rejects_a_null_message()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new LcomMetricViolation("src/A.cs", "App.Car", LcomMetricKind.Lcom1, 1.0, null!));
    }

    [Fact]
    public void The_satisfy_shape_rejects_an_empty_message()
    {
        Assert.Throws<ArgumentException>(() =>
            new LcomMetricViolation("src/A.cs", "App.Car", LcomMetricKind.Lcom1, 1.0, string.Empty));
    }
}
