namespace ArchUnitSharp.Testing.Tests;

public class AssertionFailedExceptionTests
{
    [Fact]
    public void The_message_is_the_shaped_results_message()
    {
        var result = new CheckResult(Passed: false, Message: "File 'src/App/Program.cs' violates the rule.");

        var failure = new AssertionFailedException(result);

        Assert.Equal("File 'src/App/Program.cs' violates the rule.", failure.Message);
    }

    [Fact]
    public void The_exception_carries_the_shaped_result()
    {
        var result = new CheckResult(Passed: false, Message: "Cycle: src/A.cs → src/B.cs → src/A.cs");

        var failure = new AssertionFailedException(result);

        Assert.Same(result, failure.Result);
        Assert.False(failure.Result.Passed);
    }

    [Fact]
    public void A_null_result_is_rejected()
    {
        Assert.Throws<ArgumentNullException>(() => new AssertionFailedException(null!));
    }

    [Fact]
    public void A_passed_result_is_rejected()
    {
        var result = new CheckResult(Passed: true, Message: "The rule passed.");

        Assert.Throws<ArgumentException>(() => new AssertionFailedException(result));
    }
}
