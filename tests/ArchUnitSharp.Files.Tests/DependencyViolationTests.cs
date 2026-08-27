using ArchUnitSharp.Common.Extraction;

namespace ArchUnitSharp.Files.Tests;

public class DependencyViolationTests
{
    [Fact]
    public void Is_a_violation_of_the_rule_kind()
    {
        var violation = new DependencyViolation("src/App/Program.cs", "src/Models/Car.cs");

        Assert.IsAssignableFrom<Violation>(violation);
        Assert.Equal(ViolationKind.Rule, violation.Kind);
    }

    [Fact]
    public void Carries_the_offending_dependency()
    {
        var violation = new DependencyViolation("src/App/Program.cs", "src/Models/Car.cs");

        Assert.Equal("src/App/Program.cs", violation.Source);
        Assert.Equal("src/Models/Car.cs", violation.Target);
    }

    [Fact]
    public void Null_source_is_rejected()
    {
        Assert.Throws<ArgumentNullException>(() => new DependencyViolation(null!, "src/Models/Car.cs"));
    }

    [Fact]
    public void Null_target_is_rejected()
    {
        Assert.Throws<ArgumentNullException>(() => new DependencyViolation("src/App/Program.cs", null!));
    }

    [Fact]
    public void Empty_source_is_rejected()
    {
        Assert.Throws<ArgumentException>(() => new DependencyViolation(string.Empty, "src/Models/Car.cs"));
    }

    [Fact]
    public void Empty_target_is_rejected()
    {
        Assert.Throws<ArgumentException>(() => new DependencyViolation("src/App/Program.cs", string.Empty));
    }

    [Fact]
    public void Two_violations_with_the_same_dependency_are_equal()
    {
        var first = new DependencyViolation("src/App/Program.cs", "src/Models/Car.cs");
        var second = new DependencyViolation("src/App/Program.cs", "src/Models/Car.cs");

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void A_with_expression_can_replace_the_dependency_through_the_same_validation()
    {
        var violation = new DependencyViolation("src/App/Program.cs", "src/Models/Car.cs");

        var rewritten = violation with { Target = "src/Models/Truck.cs" };
        Assert.Equal("src/Models/Truck.cs", rewritten.Target);
        Assert.Equal("src/Models/Car.cs", violation.Target);
    }

    [Fact]
    public void A_with_expression_cannot_introduce_an_empty_source()
    {
        var violation = new DependencyViolation("src/App/Program.cs", "src/Models/Car.cs");

        Assert.Throws<ArgumentException>(() => violation with { Source = string.Empty });
    }
}
