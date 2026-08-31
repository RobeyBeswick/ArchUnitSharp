using ArchUnitSharp.Common.Extraction;

namespace ArchUnitSharp.Metrics.Tests;

public class DistanceZoneViolationTests
{
    [Fact]
    public void The_violation_carries_the_subjects_data()
    {
        var violation = new DistanceZoneViolation(
            "src/Utils/Helpers.cs",
            DistanceZone.Pain,
            abstractness: 0.0,
            instability: 0.0);

        Assert.Equal(ViolationKind.Rule, violation.Kind);
        Assert.Equal("src/Utils/Helpers.cs", violation.File);
        Assert.Equal(DistanceZone.Pain, violation.Zone);
        Assert.Equal(0.0, violation.Abstractness);
        Assert.Equal(0.0, violation.Instability);
    }

    [Fact]
    public void Two_violations_with_the_same_data_are_equal()
    {
        var first = new DistanceZoneViolation("src/A.cs", DistanceZone.Uselessness, 1.0, 1.0);
        var second = first with { };

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Two_violations_that_differ_in_the_zone_are_not_equal()
    {
        var pain = new DistanceZoneViolation("src/A.cs", DistanceZone.Pain, 0.0, 0.0);
        var uselessness = new DistanceZoneViolation("src/A.cs", DistanceZone.Uselessness, 0.0, 0.0);

        Assert.NotEqual(pain, uselessness);
        Assert.NotEqual(uselessness, pain);
    }

    [Fact]
    public void The_constructor_rejects_a_null_file()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new DistanceZoneViolation(null!, DistanceZone.Pain, 0.0, 0.0));
    }

    [Fact]
    public void The_constructor_rejects_an_empty_file()
    {
        Assert.Throws<ArgumentException>(() =>
            new DistanceZoneViolation(string.Empty, DistanceZone.Pain, 0.0, 0.0));
    }

    [Fact]
    public void A_with_expression_cannot_introduce_an_empty_file()
    {
        var violation = new DistanceZoneViolation("src/A.cs", DistanceZone.Pain, 0.0, 0.0);

        Assert.Throws<ArgumentException>(() => violation with { File = string.Empty });
    }
}
