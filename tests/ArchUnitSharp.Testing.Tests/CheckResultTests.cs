namespace ArchUnitSharp.Testing.Tests;

public class CheckResultTests
{
    [Fact]
    public void Two_results_with_the_same_verdict_and_message_are_equal()
    {
        var first = new CheckResult(Passed: true, Message: "The rule passed.");
        var second = new CheckResult(Passed: true, Message: "The rule passed.");

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Results_with_different_verdicts_are_not_equal()
    {
        var passed = new CheckResult(Passed: true, Message: "The rule passed.");
        var failed = new CheckResult(Passed: false, Message: "The rule passed.");

        Assert.NotEqual(passed, failed);
    }

    [Fact]
    public void Results_with_different_messages_are_not_equal()
    {
        var first = new CheckResult(Passed: false, Message: "File 'a.cs' violates the rule.");
        var second = new CheckResult(Passed: false, Message: "File 'b.cs' violates the rule.");

        Assert.NotEqual(first, second);
    }
}
