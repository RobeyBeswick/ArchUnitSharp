using ArchUnitSharp.Common.Extraction;

namespace ArchUnitSharp.Metrics.Tests;

public class DistanceMetricViolationTests
{
    [Fact]
    public void The_violation_carries_the_subjects_data()
    {
        var violation = new DistanceMetricViolation(
            "src/Models/Car.cs",
            DistanceMetricKind.Instability,
            0.8,
            MetricComparison.Below,
            0.5);

        Assert.Equal(ViolationKind.Rule, violation.Kind);
        Assert.Equal("src/Models/Car.cs", violation.File);
        Assert.Equal(DistanceMetricKind.Instability, violation.MetricKind);
        Assert.Equal(0.8, violation.Value);
        Assert.Equal(MetricComparison.Below, violation.Comparison);
        Assert.Equal(0.5, violation.Threshold);
        Assert.Null(violation.Message);
    }

    [Fact]
    public void A_predicate_violation_carries_the_message_instead()
    {
        var violation = new DistanceMetricViolation(
            "src/Models/Car.cs",
            DistanceMetricKind.Abstractness,
            0.2,
            "every file is abstract enough");

        Assert.Equal("every file is abstract enough", violation.Message);
        Assert.Null(violation.Comparison);
        Assert.Null(violation.Threshold);
    }

    [Fact]
    public void Two_violations_with_the_same_data_are_equal()
    {
        var first = new DistanceMetricViolation(
            "src/A.cs",
            DistanceMetricKind.Abstractness,
            0.5,
            MetricComparison.Above,
            0.3);
        var second = first with { };

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void The_constructor_rejects_a_null_file()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new DistanceMetricViolation(
                null!,
                DistanceMetricKind.Abstractness,
                0.5,
                MetricComparison.Above,
                0.3));
    }

    [Fact]
    public void The_constructor_rejects_an_empty_file()
    {
        Assert.Throws<ArgumentException>(() =>
            new DistanceMetricViolation(
                string.Empty,
                DistanceMetricKind.Abstractness,
                0.5,
                MetricComparison.Above,
                0.3));
    }

    [Fact]
    public void The_predicate_constructor_rejects_a_null_message()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new DistanceMetricViolation("src/A.cs", DistanceMetricKind.Abstractness, 0.5, null!));
    }

    [Fact]
    public void The_predicate_constructor_rejects_an_empty_message()
    {
        Assert.Throws<ArgumentException>(() =>
            new DistanceMetricViolation("src/A.cs", DistanceMetricKind.Abstractness, 0.5, string.Empty));
    }

    [Fact]
    public void A_with_expression_cannot_introduce_an_empty_file()
    {
        var violation = new DistanceMetricViolation(
            "src/A.cs",
            DistanceMetricKind.Abstractness,
            0.5,
            MetricComparison.Above,
            0.3);

        Assert.Throws<ArgumentException>(() => violation with { File = string.Empty });
    }
}
