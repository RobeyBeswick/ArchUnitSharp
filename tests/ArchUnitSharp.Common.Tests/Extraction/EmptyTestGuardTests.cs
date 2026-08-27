using ArchUnitSharp.Common.Extraction;

namespace ArchUnitSharp.Common.Tests.Extraction;

public class EmptyTestGuardTests
{
    [Fact]
    public void Guards_a_rule_that_matched_nothing_with_an_empty_test_violation()
    {
        IReadOnlyList<Violation> violations = EmptyTestGuard.Guard("project files should exist", null);

        Assert.Equal(
            new Violation[] { new EmptyTestViolation("project files should exist") },
            violations);
    }

    [Fact]
    public void The_violation_carries_the_rule_that_was_empty()
    {
        IReadOnlyList<Violation> violations = EmptyTestGuard.Guard("layers should be acyclic", null);

        var violation = Assert.IsType<EmptyTestViolation>(Assert.Single(violations));
        Assert.Equal(ViolationKind.EmptyTest, violation.Kind);
        Assert.Equal("layers should be acyclic", violation.RuleDescription);
    }

    [Fact]
    public void Null_options_mean_the_defaults_so_the_guard_is_on()
    {
        IReadOnlyList<Violation> violations = EmptyTestGuard.Guard("slices should not form cycles", null);

        Assert.Equal(
            new Violation[] { new EmptyTestViolation("slices should not form cycles") },
            violations);
    }

    [Fact]
    public void Guards_by_default_when_options_do_not_allow_empty_tests()
    {
        IReadOnlyList<Violation> violations = EmptyTestGuard.Guard("slices should not form cycles", new CheckOptions());

        Assert.Equal(
            new Violation[] { new EmptyTestViolation("slices should not form cycles") },
            violations);
    }

    [Fact]
    public void Allow_empty_tests_is_the_only_option_that_matters()
    {
        IReadOnlyList<Violation> violations = EmptyTestGuard.Guard(
            "project files should exist",
            new CheckOptions { ClearCache = true, IgnoreTestCode = true, IgnoreGeneratedCode = true });

        Assert.Equal(
            new Violation[] { new EmptyTestViolation("project files should exist") },
            violations);
    }

    [Fact]
    public void Passes_when_allow_empty_tests_is_set()
    {
        IReadOnlyList<Violation> violations = EmptyTestGuard.Guard(
            "project files should exist",
            new CheckOptions { AllowEmptyTests = true });

        Assert.Empty(violations);
    }

    [Fact]
    public void Allow_empty_tests_means_the_rule_description_is_never_touched()
    {
        IReadOnlyList<Violation> violations = EmptyTestGuard.Guard(null!, new CheckOptions { AllowEmptyTests = true });

        Assert.Empty(violations);
    }

    [Fact]
    public void The_returned_list_is_a_fresh_copy_on_every_call()
    {
        IReadOnlyList<Violation> first = EmptyTestGuard.Guard("project files should exist", null);
        IReadOnlyList<Violation> second = EmptyTestGuard.Guard("project files should exist", null);

        Assert.Equal(first, second);
        Assert.NotSame(first, second);
    }

    [Fact]
    public void A_null_rule_description_is_rejected()
    {
        Assert.Throws<ArgumentNullException>(() => EmptyTestGuard.Guard(null!, null));
    }

    [Fact]
    public void An_empty_rule_description_is_rejected()
    {
        Assert.Throws<ArgumentException>(() => EmptyTestGuard.Guard(string.Empty, null));
    }
}
