using ArchUnitSharp.Common.Extraction;

namespace ArchUnitSharp.Files.Tests;

public class AdhereToViolationTests
{
    [Fact]
    public void Is_a_violation_of_the_rule_kind()
    {
        var violation = new AdhereToViolation("src/Models/Car.cs", "every file has at most one class");

        Assert.IsAssignableFrom<Violation>(violation);
        Assert.Equal(ViolationKind.Rule, violation.Kind);
    }

    [Fact]
    public void Carries_the_offending_file_and_the_message()
    {
        var violation = new AdhereToViolation("src/Models/Car.cs", "every file has at most one class");

        Assert.Equal("src/Models/Car.cs", violation.File);
        Assert.Equal("every file has at most one class", violation.Message);
    }

    [Fact]
    public void Null_file_is_rejected()
    {
        Assert.Throws<ArgumentNullException>(() => new AdhereToViolation(null!, "message"));
    }

    [Fact]
    public void Empty_file_is_rejected()
    {
        Assert.Throws<ArgumentException>(() => new AdhereToViolation(string.Empty, "message"));
    }

    [Fact]
    public void Null_message_is_rejected()
    {
        Assert.Throws<ArgumentNullException>(() => new AdhereToViolation("src/Models/Car.cs", null!));
    }

    [Fact]
    public void Empty_message_is_rejected()
    {
        Assert.Throws<ArgumentException>(() => new AdhereToViolation("src/Models/Car.cs", string.Empty));
    }

    [Fact]
    public void Two_violations_with_the_same_file_and_message_are_equal()
    {
        var first = new AdhereToViolation("src/Models/Car.cs", "message");
        var second = new AdhereToViolation("src/Models/Car.cs", "message");

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void A_with_expression_can_replace_the_file_through_the_same_validation()
    {
        var violation = new AdhereToViolation("src/Models/Car.cs", "message");

        var rewritten = violation with { File = "src/App/Program.cs" };
        Assert.Equal("src/App/Program.cs", rewritten.File);
        Assert.Equal("src/Models/Car.cs", violation.File);

        Assert.Throws<ArgumentException>(() => violation with { Message = string.Empty });
    }
}
