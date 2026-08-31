namespace ArchUnitSharp.Metrics.Tests;

public class MetricViolationTests
{
    [Fact]
    public void The_threshold_shape_carries_the_rules_data()
    {
        var violation = new MetricViolation(
            "src/Classes.cs",
            "App.Big",
            CountMetricKind.MethodCount,
            value: 3,
            MetricComparison.Below,
            threshold: 1);

        Assert.Equal("src/Classes.cs", violation.File);
        Assert.Equal("App.Big", violation.Class);
        Assert.Equal(CountMetricKind.MethodCount, violation.MetricKind);
        Assert.Equal(3, violation.Value);
        Assert.Equal(MetricComparison.Below, violation.Comparison);
        Assert.Equal(1, violation.Threshold);
        Assert.Null(violation.Message);
    }

    [Fact]
    public void The_satisfy_shape_carries_the_rules_message()
    {
        var violation = new MetricViolation(
            "src/A.cs",
            null,
            CountMetricKind.LinesOfCode,
            value: 500,
            "every file is short");

        Assert.Equal("src/A.cs", violation.File);
        Assert.Null(violation.Class);
        Assert.Equal(CountMetricKind.LinesOfCode, violation.MetricKind);
        Assert.Equal(500, violation.Value);
        Assert.Equal("every file is short", violation.Message);
        Assert.Null(violation.Comparison);
        Assert.Null(violation.Threshold);
    }

    [Fact]
    public void A_file_level_metric_violation_carries_no_class()
    {
        var violation = new MetricViolation(
            "src/A.cs",
            null,
            CountMetricKind.Classes,
            value: 3,
            MetricComparison.AboveOrEqual,
            threshold: 5);

        Assert.Null(violation.Class);
    }

    [Fact]
    public void Two_violations_with_the_same_data_are_equal()
    {
        var first = new MetricViolation("src/A.cs", null, CountMetricKind.Classes, 1, MetricComparison.Equal, 2);
        var second = new MetricViolation("src/A.cs", null, CountMetricKind.Classes, 1, MetricComparison.Equal, 2);

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void The_threshold_shape_rejects_a_null_file()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new MetricViolation(null!, null, CountMetricKind.Classes, 1, MetricComparison.Equal, 2));
    }

    [Fact]
    public void The_threshold_shape_rejects_an_empty_file()
    {
        Assert.Throws<ArgumentException>(() =>
            new MetricViolation(string.Empty, null, CountMetricKind.Classes, 1, MetricComparison.Equal, 2));
    }

    [Fact]
    public void The_satisfy_shape_rejects_a_null_file()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new MetricViolation(null!, null, CountMetricKind.Classes, 1, "message"));
    }

    [Fact]
    public void The_satisfy_shape_rejects_a_null_message()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new MetricViolation("src/A.cs", null, CountMetricKind.Classes, 1, null!));
    }

    [Fact]
    public void The_satisfy_shape_rejects_an_empty_message()
    {
        Assert.Throws<ArgumentException>(() =>
            new MetricViolation("src/A.cs", null, CountMetricKind.Classes, 1, string.Empty));
    }
}
