using ArchUnitSharp.Common.Extraction;

namespace ArchUnitSharp.Files.Tests;

public class CycleViolationTests
{
    private static CycleViolation CreateViolation() =>
        new(new[] { "src/A.cs", "src/B.cs", "src/A.cs" });

    [Fact]
    public void Is_a_violation_of_the_rule_kind()
    {
        var violation = CreateViolation();

        Assert.IsAssignableFrom<Violation>(violation);
        Assert.Equal(ViolationKind.Rule, violation.Kind);
    }

    [Fact]
    public void Carries_the_ordered_files_of_the_cycle()
    {
        var violation = CreateViolation();

        Assert.Equal(new[] { "src/A.cs", "src/B.cs", "src/A.cs" }, violation.Files);
    }

    [Fact]
    public void Renders_the_cycle_as_a_readable_path()
    {
        var violation = CreateViolation();

        Assert.Equal("src/A.cs → src/B.cs → src/A.cs", violation.Path);
    }

    [Fact]
    public void Every_read_of_the_files_list_returns_a_fresh_copy()
    {
        var violation = CreateViolation();

        Assert.NotSame(violation.Files, violation.Files);
    }

    [Fact]
    public void Mutating_a_returned_list_does_not_corrupt_the_violation()
    {
        var violation = CreateViolation();

        var returned = (string[])violation.Files;
        returned[1] = "src/App/X.cs";

        Assert.Equal("src/A.cs → src/B.cs → src/A.cs", violation.Path);
    }

    [Fact]
    public void Two_violations_with_the_same_cycle_are_equal()
    {
        var first = CreateViolation();
        var second = new CycleViolation(new[] { "src/A.cs", "src/B.cs", "src/A.cs" });

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Violations_with_different_cycles_are_not_equal()
    {
        var first = CreateViolation();
        var second = new CycleViolation(new[] { "src/B.cs", "src/A.cs", "src/B.cs" });

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void Null_files_are_rejected()
    {
        Assert.Throws<ArgumentNullException>(() => new CycleViolation(null!));
    }

    [Fact]
    public void Fewer_than_three_files_are_rejected()
    {
        Assert.Throws<ArgumentException>(() => new CycleViolation(new[] { "src/A.cs", "src/B.cs" }));
    }

    [Fact]
    public void An_unclosed_cycle_is_rejected()
    {
        Assert.Throws<ArgumentException>(() =>
            new CycleViolation(new[] { "src/A.cs", "src/B.cs", "src/C.cs" }));
    }

    [Fact]
    public void A_null_or_empty_file_is_rejected()
    {
        Assert.Throws<ArgumentException>(() =>
            new CycleViolation(new[] { "src/A.cs", string.Empty, "src/A.cs" }));
        Assert.Throws<ArgumentException>(() =>
            new CycleViolation(new[] { "src/A.cs", null!, "src/A.cs" }));
    }

    [Fact]
    public void A_with_expression_can_replace_the_cycle_through_the_same_validation()
    {
        var violation = CreateViolation();

        var rewritten = violation with { Files = new[] { "src/X.cs", "src/Y.cs", "src/X.cs" } };

        Assert.Equal("src/X.cs → src/Y.cs → src/X.cs", rewritten.Path);
        Assert.Equal("src/A.cs → src/B.cs → src/A.cs", violation.Path);
    }

    [Fact]
    public void A_with_expression_cannot_introduce_an_unclosed_cycle()
    {
        var violation = CreateViolation();

        Assert.Throws<ArgumentException>(() =>
            violation with { Files = new[] { "src/A.cs", "src/B.cs", "src/C.cs" } });
    }
}
