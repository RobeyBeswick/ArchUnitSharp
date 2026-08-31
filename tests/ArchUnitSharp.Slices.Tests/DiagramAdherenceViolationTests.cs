using ArchUnitSharp.Common.Extraction;

namespace ArchUnitSharp.Slices.Tests;

public class DiagramAdherenceViolationTests
{
    [Fact]
    public void Is_a_violation_of_the_rule_kind()
    {
        var violation = new DiagramAdherenceViolation("api", "services");

        Assert.IsAssignableFrom<Violation>(violation);
        Assert.Equal(ViolationKind.Rule, violation.Kind);
    }

    [Fact]
    public void Carries_the_two_slices_of_the_dependency()
    {
        var violation = new DiagramAdherenceViolation("api", "services");

        Assert.Equal("api", violation.SourceSlice);
        Assert.Equal("services", violation.TargetSlice);
    }

    [Fact]
    public void Null_source_slice_is_rejected()
    {
        Assert.Throws<ArgumentNullException>(() => new DiagramAdherenceViolation(null!, "services"));
    }

    [Fact]
    public void Empty_source_slice_is_rejected()
    {
        Assert.Throws<ArgumentException>(() => new DiagramAdherenceViolation(string.Empty, "services"));
    }

    [Fact]
    public void Null_target_slice_is_rejected()
    {
        Assert.Throws<ArgumentNullException>(() => new DiagramAdherenceViolation("api", null!));
    }

    [Fact]
    public void Empty_target_slice_is_rejected()
    {
        Assert.Throws<ArgumentException>(() => new DiagramAdherenceViolation("api", string.Empty));
    }

    [Fact]
    public void Two_violations_with_the_same_slices_are_equal()
    {
        var first = new DiagramAdherenceViolation("api", "services");
        var second = new DiagramAdherenceViolation("api", "services");

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Violations_with_different_slices_are_not_equal()
    {
        var first = new DiagramAdherenceViolation("api", "services");
        var second = new DiagramAdherenceViolation("api", "database");

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void A_with_expression_can_replace_a_value_through_the_same_validation()
    {
        var violation = new DiagramAdherenceViolation("api", "services");

        var rewritten = violation with { TargetSlice = "database" };
        Assert.Equal("database", rewritten.TargetSlice);
        Assert.Equal("services", violation.TargetSlice);
    }

    [Fact]
    public void A_with_expression_cannot_introduce_an_empty_target()
    {
        var violation = new DiagramAdherenceViolation("api", "services");

        Assert.Throws<ArgumentException>(() => violation with { TargetSlice = string.Empty });
    }
}
