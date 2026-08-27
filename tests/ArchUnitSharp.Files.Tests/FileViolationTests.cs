using ArchUnitSharp.Common.Extraction;

namespace ArchUnitSharp.Files.Tests;

public class FileViolationTests
{
    [Fact]
    public void Is_a_violation_of_the_rule_kind()
    {
        var violation = new FileViolation("src/Models/Car.cs");

        Assert.IsAssignableFrom<Violation>(violation);
        Assert.Equal(ViolationKind.Rule, violation.Kind);
    }

    [Fact]
    public void Carries_the_offending_file()
    {
        var violation = new FileViolation("src/Models/Car.cs");

        Assert.Equal("src/Models/Car.cs", violation.File);
    }

    [Fact]
    public void Null_file_is_rejected()
    {
        Assert.Throws<ArgumentNullException>(() => new FileViolation(null!));
    }

    [Fact]
    public void Empty_file_is_rejected()
    {
        Assert.Throws<ArgumentException>(() => new FileViolation(string.Empty));
    }

    [Fact]
    public void Two_violations_with_the_same_file_are_equal()
    {
        var first = new FileViolation("src/Models/Car.cs");
        var second = new FileViolation("src/Models/Car.cs");

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void A_with_expression_can_replace_the_file_through_the_same_validation()
    {
        var violation = new FileViolation("src/Models/Car.cs");

        var rewritten = violation with { File = "src/App/Program.cs" };
        Assert.Equal("src/App/Program.cs", rewritten.File);
        Assert.Equal("src/Models/Car.cs", violation.File);
    }

    [Fact]
    public void A_with_expression_cannot_introduce_an_empty_file()
    {
        var violation = new FileViolation("src/Models/Car.cs");

        Assert.Throws<ArgumentException>(() => violation with { File = string.Empty });
    }
}
