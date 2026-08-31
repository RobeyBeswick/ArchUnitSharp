using ArchUnitSharp.Common.Extraction;

namespace ArchUnitSharp.Slices.Tests;

public class ForbiddenDependencyViolationTests
{
    [Fact]
    public void Is_a_violation_of_the_rule_kind()
    {
        var violation = new ForbiddenDependencyViolation(
            "billing",
            "src/features/billing/order.cs",
            "src/legacy/Old.cs");

        Assert.IsAssignableFrom<Violation>(violation);
        Assert.Equal(ViolationKind.Rule, violation.Kind);
    }

    [Fact]
    public void Carries_the_slice_and_the_two_files()
    {
        var violation = new ForbiddenDependencyViolation(
            "billing",
            "src/features/billing/order.cs",
            "src/legacy/Old.cs");

        Assert.Equal("billing", violation.Slice);
        Assert.Equal("src/features/billing/order.cs", violation.Source);
        Assert.Equal("src/legacy/Old.cs", violation.Target);
    }

    [Fact]
    public void Null_slice_is_rejected()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ForbiddenDependencyViolation(null!, "src/features/billing/order.cs", "src/legacy/Old.cs"));
    }

    [Fact]
    public void Empty_slice_is_rejected()
    {
        Assert.Throws<ArgumentException>(() =>
            new ForbiddenDependencyViolation(string.Empty, "src/features/billing/order.cs", "src/legacy/Old.cs"));
    }

    [Fact]
    public void Null_source_is_rejected()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ForbiddenDependencyViolation("billing", null!, "src/legacy/Old.cs"));
    }

    [Fact]
    public void Empty_source_is_rejected()
    {
        Assert.Throws<ArgumentException>(() =>
            new ForbiddenDependencyViolation("billing", string.Empty, "src/legacy/Old.cs"));
    }

    [Fact]
    public void Null_target_is_rejected()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ForbiddenDependencyViolation("billing", "src/features/billing/order.cs", null!));
    }

    [Fact]
    public void Empty_target_is_rejected()
    {
        Assert.Throws<ArgumentException>(() =>
            new ForbiddenDependencyViolation("billing", "src/features/billing/order.cs", string.Empty));
    }

    [Fact]
    public void Two_violations_with_the_same_values_are_equal()
    {
        var first = new ForbiddenDependencyViolation(
            "billing",
            "src/features/billing/order.cs",
            "src/legacy/Old.cs");
        var second = new ForbiddenDependencyViolation(
            "billing",
            "src/features/billing/order.cs",
            "src/legacy/Old.cs");

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Violations_with_different_values_are_not_equal()
    {
        var first = new ForbiddenDependencyViolation(
            "billing",
            "src/features/billing/order.cs",
            "src/legacy/Old.cs");
        var second = new ForbiddenDependencyViolation(
            "auth",
            "src/features/billing/order.cs",
            "src/legacy/Old.cs");

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void A_with_expression_can_replace_a_value_through_the_same_validation()
    {
        var violation = new ForbiddenDependencyViolation(
            "billing",
            "src/features/billing/order.cs",
            "src/legacy/Old.cs");

        var rewritten = violation with { Slice = "auth" };
        Assert.Equal("auth", rewritten.Slice);
        Assert.Equal("billing", violation.Slice);
    }

    [Fact]
    public void A_with_expression_cannot_introduce_an_empty_slice()
    {
        var violation = new ForbiddenDependencyViolation(
            "billing",
            "src/features/billing/order.cs",
            "src/legacy/Old.cs");

        Assert.Throws<ArgumentException>(() => violation with { Slice = string.Empty });
    }
}
