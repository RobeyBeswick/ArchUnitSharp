using ArchUnitSharp.Common.Extraction;

namespace ArchUnitSharp.Slices.Tests;

public class MissingDependencyViolationTests
{
    [Fact]
    public void Is_a_violation_of_the_rule_kind()
    {
        var violation = new MissingDependencyViolation("auth", "src/features/**", "src/shared/**");

        Assert.IsAssignableFrom<Violation>(violation);
        Assert.Equal(ViolationKind.Rule, violation.Kind);
    }

    [Fact]
    public void Carries_the_slice_and_the_two_patterns()
    {
        var violation = new MissingDependencyViolation("auth", "src/features/**", "src/shared/**");

        Assert.Equal("auth", violation.Slice);
        Assert.Equal("src/features/**", violation.From);
        Assert.Equal("src/shared/**", violation.To);
    }

    [Fact]
    public void Null_slice_is_rejected()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new MissingDependencyViolation(null!, "src/features/**", "src/shared/**"));
    }

    [Fact]
    public void Empty_slice_is_rejected()
    {
        Assert.Throws<ArgumentException>(() =>
            new MissingDependencyViolation(string.Empty, "src/features/**", "src/shared/**"));
    }

    [Fact]
    public void Null_from_is_rejected()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new MissingDependencyViolation("auth", null!, "src/shared/**"));
    }

    [Fact]
    public void Empty_from_is_rejected()
    {
        Assert.Throws<ArgumentException>(() =>
            new MissingDependencyViolation("auth", string.Empty, "src/shared/**"));
    }

    [Fact]
    public void Null_to_is_rejected()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new MissingDependencyViolation("auth", "src/features/**", null!));
    }

    [Fact]
    public void Empty_to_is_rejected()
    {
        Assert.Throws<ArgumentException>(() =>
            new MissingDependencyViolation("auth", "src/features/**", string.Empty));
    }

    [Fact]
    public void Two_violations_with_the_same_values_are_equal()
    {
        var first = new MissingDependencyViolation("auth", "src/features/**", "src/shared/**");
        var second = new MissingDependencyViolation("auth", "src/features/**", "src/shared/**");

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Violations_with_different_values_are_not_equal()
    {
        var first = new MissingDependencyViolation("auth", "src/features/**", "src/shared/**");
        var second = new MissingDependencyViolation("billing", "src/features/**", "src/shared/**");

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void A_with_expression_can_replace_a_value_through_the_same_validation()
    {
        var violation = new MissingDependencyViolation("auth", "src/features/**", "src/shared/**");

        var rewritten = violation with { To = "src/legacy/**" };
        Assert.Equal("src/legacy/**", rewritten.To);
        Assert.Equal("src/shared/**", violation.To);
    }

    [Fact]
    public void A_with_expression_cannot_introduce_an_empty_slice()
    {
        var violation = new MissingDependencyViolation("auth", "src/features/**", "src/shared/**");

        Assert.Throws<ArgumentException>(() => violation with { Slice = string.Empty });
    }
}
