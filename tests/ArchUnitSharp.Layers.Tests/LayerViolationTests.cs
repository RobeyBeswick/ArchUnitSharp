using ArchUnitSharp.Common.Extraction;

namespace ArchUnitSharp.Layers.Tests;

public class LayerViolationTests
{
    [Fact]
    public void Carries_the_four_data_values()
    {
        var violation = new LayerViolation(
            "Services",
            "src/Services/CarService.cs",
            "src/Models/Car.cs",
            "Models");

        Assert.Equal("Services", violation.SubjectLayer);
        Assert.Equal("src/Services/CarService.cs", violation.Source);
        Assert.Equal("src/Models/Car.cs", violation.Target);
        Assert.Equal("Models", violation.TargetLayer);
        Assert.Equal(ViolationKind.Rule, violation.Kind);
    }

    [Fact]
    public void Two_violations_with_the_same_values_are_equal()
    {
        var first = new LayerViolation("Services", "src/Services/A.cs", "src/Models/Car.cs", "Models");
        var second = new LayerViolation("Services", "src/Services/A.cs", "src/Models/Car.cs", "Models");

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Theory]
    [InlineData(null, "src/Services/A.cs", "src/Models/Car.cs", "Models")]
    [InlineData("Services", null, "src/Models/Car.cs", "Models")]
    [InlineData("Services", "src/Services/A.cs", null, "Models")]
    [InlineData("Services", "src/Services/A.cs", "src/Models/Car.cs", null)]
    public void Rejects_a_null_argument(
        string? subjectLayer,
        string? source,
        string? target,
        string? targetLayer)
    {
        Assert.Throws<ArgumentNullException>(() =>
            new LayerViolation(subjectLayer!, source!, target!, targetLayer!));
    }

    [Theory]
    [InlineData("", "src/Services/A.cs", "src/Models/Car.cs", "Models")]
    [InlineData("Services", "", "src/Models/Car.cs", "Models")]
    [InlineData("Services", "src/Services/A.cs", "", "Models")]
    [InlineData("Services", "src/Services/A.cs", "src/Models/Car.cs", "")]
    public void Rejects_an_empty_argument(
        string subjectLayer,
        string source,
        string target,
        string targetLayer)
    {
        Assert.Throws<ArgumentException>(() =>
            new LayerViolation(subjectLayer, source, target, targetLayer));
    }
}
