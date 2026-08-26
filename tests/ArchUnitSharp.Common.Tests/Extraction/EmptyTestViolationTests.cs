using ArchUnitSharp.Common.Extraction;

namespace ArchUnitSharp.Common.Tests.Extraction;

public class EmptyTestViolationTests
{
    private sealed record FakeViolation : Violation
    {
        public FakeViolation(ViolationKind kind) : base(kind) { }
    }

    [Fact]
    public void A_non_zero_kind_passes_through_the_base_constructor()
    {
        var violation = new FakeViolation(ViolationKind.Rule);

        Assert.Equal(ViolationKind.Rule, violation.Kind);
    }

    [Fact]
    public void Is_a_violation_of_the_empty_test_kind()
    {
        var violation = new EmptyTestViolation("project files should not depend on themselves");

        Assert.IsAssignableFrom<Violation>(violation);
        Assert.Equal(ViolationKind.EmptyTest, violation.Kind);
    }

    [Fact]
    public void Carries_the_rule_that_was_empty()
    {
        var violation = new EmptyTestViolation("layers should be acyclic");

        Assert.Equal("layers should be acyclic", violation.RuleDescription);
    }

    [Fact]
    public void Null_rule_description_is_rejected()
    {
        Assert.Throws<ArgumentNullException>(() => new EmptyTestViolation(null!));
    }

    [Fact]
    public void Empty_rule_description_is_rejected()
    {
        Assert.Throws<ArgumentException>(() => new EmptyTestViolation(string.Empty));
    }

    [Fact]
    public void Whitespace_rule_description_is_allowed()
    {
        var violation = new EmptyTestViolation(" ");

        Assert.Equal(" ", violation.RuleDescription);
    }

    [Fact]
    public void Two_violations_with_the_same_rule_are_equal()
    {
        var first = new EmptyTestViolation("slices should not form cycles");
        var second = new EmptyTestViolation("slices should not form cycles");

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void A_with_expression_can_replace_the_rule_through_the_same_validation()
    {
        var violation = new EmptyTestViolation("original rule");

        var rewritten = violation with { RuleDescription = "rewritten rule" };
        Assert.Equal("rewritten rule", rewritten.RuleDescription);
        Assert.Equal("original rule", violation.RuleDescription);
    }

    [Fact]
    public void A_with_expression_cannot_introduce_an_empty_rule()
    {
        var violation = new EmptyTestViolation("original rule");

        Assert.Throws<ArgumentException>(() => violation with { RuleDescription = string.Empty });
    }
}
