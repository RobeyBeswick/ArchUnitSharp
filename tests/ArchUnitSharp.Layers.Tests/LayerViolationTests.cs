using ArchUnitSharp.Common.Extraction;

namespace ArchUnitSharp.Layers.Tests;

public class LayerViolationTests
{
    [Fact]
    public void Is_a_violation_of_the_rule_kind()
    {
        var violation = new LayerViolation("App", "Models", "src/App/Program.cs", "src/Models/Car.cs");

        Assert.IsAssignableFrom<Violation>(violation);
        Assert.Equal(ViolationKind.Rule, violation.Kind);
    }

    [Fact]
    public void Carries_the_two_layers_and_the_two_files()
    {
        var violation = new LayerViolation("App", "Models", "src/App/Program.cs", "src/Models/Car.cs");

        Assert.Equal("App", violation.SourceLayer);
        Assert.Equal("Models", violation.TargetLayer);
        Assert.Equal("src/App/Program.cs", violation.Source);
        Assert.Equal("src/Models/Car.cs", violation.Target);
    }

    [Fact]
    public void Null_source_layer_is_rejected()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new LayerViolation(null!, "Models", "src/App/Program.cs", "src/Models/Car.cs"));
    }

    [Fact]
    public void Empty_source_layer_is_rejected()
    {
        Assert.Throws<ArgumentException>(() =>
            new LayerViolation(string.Empty, "Models", "src/App/Program.cs", "src/Models/Car.cs"));
    }

    [Fact]
    public void Null_target_layer_is_rejected()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new LayerViolation("App", null!, "src/App/Program.cs", "src/Models/Car.cs"));
    }

    [Fact]
    public void Empty_target_layer_is_rejected()
    {
        Assert.Throws<ArgumentException>(() =>
            new LayerViolation("App", string.Empty, "src/App/Program.cs", "src/Models/Car.cs"));
    }

    [Fact]
    public void Null_source_file_is_rejected()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new LayerViolation("App", "Models", null!, "src/Models/Car.cs"));
    }

    [Fact]
    public void Empty_source_file_is_rejected()
    {
        Assert.Throws<ArgumentException>(() =>
            new LayerViolation("App", "Models", string.Empty, "src/Models/Car.cs"));
    }

    [Fact]
    public void Null_target_file_is_rejected()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new LayerViolation("App", "Models", "src/App/Program.cs", null!));
    }

    [Fact]
    public void Empty_target_file_is_rejected()
    {
        Assert.Throws<ArgumentException>(() =>
            new LayerViolation("App", "Models", "src/App/Program.cs", string.Empty));
    }

    [Fact]
    public void Two_violations_with_the_same_values_are_equal()
    {
        var first = new LayerViolation("App", "Models", "src/App/Program.cs", "src/Models/Car.cs");
        var second = new LayerViolation("App", "Models", "src/App/Program.cs", "src/Models/Car.cs");

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Violations_with_different_values_are_not_equal()
    {
        var first = new LayerViolation("App", "Models", "src/App/Program.cs", "src/Models/Car.cs");
        var second = new LayerViolation("App", "Infra", "src/App/Program.cs", "src/Models/Car.cs");

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void A_with_expression_can_replace_a_value_through_the_same_validation()
    {
        var violation = new LayerViolation("App", "Models", "src/App/Program.cs", "src/Models/Car.cs");

        var rewritten = violation with { TargetLayer = "Services" };
        Assert.Equal("Services", rewritten.TargetLayer);
        Assert.Equal("Models", violation.TargetLayer);
    }

    [Fact]
    public void A_with_expression_cannot_introduce_an_empty_source_layer()
    {
        var violation = new LayerViolation("App", "Models", "src/App/Program.cs", "src/Models/Car.cs");

        Assert.Throws<ArgumentException>(() => violation with { SourceLayer = string.Empty });
    }
}
