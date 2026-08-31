namespace ArchUnitSharp.Metrics.Tests;

public class CustomMetricViolationTests
{
    [Fact]
    public void The_threshold_shape_carries_the_rules_data()
    {
        var violation = new CustomMetricViolation(
            "src/Classes.cs",
            "App.Big",
            "member count",
            "classes stay focused",
            value: 3,
            MetricComparison.Below,
            threshold: 1);

        Assert.Equal("src/Classes.cs", violation.File);
        Assert.Equal("App.Big", violation.Class);
        Assert.Equal("member count", violation.MetricName);
        Assert.Equal("classes stay focused", violation.Description);
        Assert.Equal(3, violation.Value);
        Assert.Equal(MetricComparison.Below, violation.Comparison);
        Assert.Equal(1, violation.Threshold);
        Assert.Null(violation.Message);
    }

    [Fact]
    public void The_satisfy_shape_carries_the_rules_message()
    {
        var violation = new CustomMetricViolation(
            "src/A.cs",
            "App.Car",
            "member count",
            "classes stay focused",
            value: 500,
            "focused classes");

        Assert.Equal("src/A.cs", violation.File);
        Assert.Equal("App.Car", violation.Class);
        Assert.Equal("member count", violation.MetricName);
        Assert.Equal("classes stay focused", violation.Description);
        Assert.Equal(500, violation.Value);
        Assert.Equal("focused classes", violation.Message);
        Assert.Null(violation.Comparison);
        Assert.Null(violation.Threshold);
    }

    [Fact]
    public void Two_violations_with_the_same_data_are_equal()
    {
        var first = new CustomMetricViolation(
            "src/A.cs",
            "App.Car",
            "member count",
            "classes stay focused",
            1,
            MetricComparison.Equal,
            2);
        var second = new CustomMetricViolation(
            "src/A.cs",
            "App.Car",
            "member count",
            "classes stay focused",
            1,
            MetricComparison.Equal,
            2);

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void The_threshold_shape_rejects_a_null_file()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new CustomMetricViolation(null!, "App.Car", "member count", "d", 1, MetricComparison.Equal, 2));
    }

    [Fact]
    public void The_threshold_shape_rejects_an_empty_file()
    {
        Assert.Throws<ArgumentException>(() =>
            new CustomMetricViolation(string.Empty, "App.Car", "member count", "d", 1, MetricComparison.Equal, 2));
    }

    [Fact]
    public void The_threshold_shape_rejects_a_null_class()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new CustomMetricViolation("src/A.cs", null!, "member count", "d", 1, MetricComparison.Equal, 2));
    }

    [Fact]
    public void The_threshold_shape_rejects_a_null_metric_name()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new CustomMetricViolation("src/A.cs", "App.Car", null!, "d", 1, MetricComparison.Equal, 2));
    }

    [Fact]
    public void The_threshold_shape_rejects_a_null_description()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new CustomMetricViolation("src/A.cs", "App.Car", "member count", null!, 1, MetricComparison.Equal, 2));
    }

    [Fact]
    public void The_satisfy_shape_rejects_a_null_message()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new CustomMetricViolation("src/A.cs", "App.Car", "member count", "d", 1, null!));
    }

    [Fact]
    public void The_satisfy_shape_rejects_an_empty_message()
    {
        Assert.Throws<ArgumentException>(() =>
            new CustomMetricViolation("src/A.cs", "App.Car", "member count", "d", 1, string.Empty));
    }
}
