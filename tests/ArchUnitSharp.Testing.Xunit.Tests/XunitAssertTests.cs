using ArchUnitSharp.Common.Extraction;
using Xunit.Sdk;

namespace ArchUnitSharp.Testing.Xunit.Tests;

public class XunitAssertTests
{
    [Fact]
    public void Native_passes_for_a_passing_rule()
    {
        var rule = new ArchUnitSharp.Files.Files(Graph(Self("src/App/Program.cs"))).Should().Exist();

        XunitAssert.PassesCore(rule, null, native: true);
    }

    [Fact]
    public void Native_passes_throws_the_native_true_exception_with_the_report_message()
    {
        var rule = new ArchUnitSharp.Files.Files(Graph(Self("src/App/Program.cs")))
            .Should()
            .HaveName("Car.cs");

        TrueException failure =
            Assert.Throws<TrueException>(() => XunitAssert.PassesCore(rule, null, native: true));

        Assert.Equal("File 'src/App/Program.cs' violates the rule.", failure.Message);
    }

    [Fact]
    public void Native_fails_for_a_failing_rule()
    {
        var rule = new ArchUnitSharp.Files.Files(Graph(Self("src/App/Program.cs")))
            .Should()
            .HaveName("Car.cs");

        XunitAssert.FailsCore(rule, null, native: true);
    }

    [Fact]
    public void Native_fails_throws_the_native_false_exception_when_the_rule_passes()
    {
        var rule = new ArchUnitSharp.Files.Files(Graph(Self("src/App/Program.cs"))).Should().Exist();

        FalseException failure =
            Assert.Throws<FalseException>(() => XunitAssert.FailsCore(rule, null, native: true));

        Assert.Equal(ResultFactory.PassLine, failure.Message);
    }

    [Fact]
    public void Fallback_passes_for_a_passing_rule()
    {
        var rule = new ArchUnitSharp.Files.Files(Graph(Self("src/App/Program.cs"))).Should().Exist();

        XunitAssert.PassesCore(rule, null, native: false);
    }

    [Fact]
    public void Fallback_passes_throws_the_agnostic_failure_for_a_failing_rule()
    {
        var rule = new ArchUnitSharp.Files.Files(Graph(Self("src/App/Program.cs")))
            .Should()
            .HaveName("Car.cs");

        AssertionFailedException failure =
            Assert.Throws<AssertionFailedException>(() => XunitAssert.PassesCore(rule, null, native: false));

        Assert.Equal("File 'src/App/Program.cs' violates the rule.", failure.Message);
    }

    [Fact]
    public void Fallback_fails_for_a_failing_rule()
    {
        var rule = new ArchUnitSharp.Files.Files(Graph(Self("src/App/Program.cs")))
            .Should()
            .HaveName("Car.cs");

        XunitAssert.FailsCore(rule, null, native: false);
    }

    [Fact]
    public void Fallback_fails_throws_the_agnostic_failure_when_the_rule_passes()
    {
        var rule = new ArchUnitSharp.Files.Files(Graph(Self("src/App/Program.cs"))).Should().Exist();

        AssertionFailedException failure =
            Assert.Throws<AssertionFailedException>(() => XunitAssert.FailsCore(rule, null, native: false));

        Assert.Equal("The rule passed, but the assertion expected it to fail.", failure.Message);
    }

    [Fact]
    public void The_empty_test_guard_fails_an_empty_rule_by_default()
    {
        var rule = new ArchUnitSharp.Files.Files(Graph(Self("src/Models/Car.cs")))
            .InPath("src/NoSuchFile.cs")
            .Should()
            .Exist();

        TrueException failure =
            Assert.Throws<TrueException>(() => XunitAssert.PassesCore(rule, null, native: true));

        Assert.Equal(
            "The rule matched nothing: project files in path 'src/NoSuchFile.cs' should exist.",
            failure.Message);
    }

    [Fact]
    public void AllowEmptyTests_makes_an_empty_rule_pass()
    {
        var rule = new ArchUnitSharp.Files.Files(Graph(Self("src/Models/Car.cs")))
            .InPath("src/NoSuchFile.cs")
            .Should()
            .Exist();

        XunitAssert.PassesCore(rule, new CheckOptions { AllowEmptyTests = true }, native: true);
    }

    [Fact]
    public void A_user_error_from_the_check_propagates_unchanged()
    {
        var rule = new ArchUnitSharp.Files.Files(Graph(Self("src/App/Program.cs")))
            .Should()
            .AdhereTo(static _ => true, "the rule's message");

        Assert.Throws<UserError>(() => XunitAssert.PassesCore(rule, null, native: true));
    }

    [Fact]
    public void Passes_rejects_a_null_rule()
    {
        Assert.Throws<ArgumentNullException>(() => XunitAssert.Passes(null!));
    }

    [Fact]
    public void Fails_rejects_a_null_rule()
    {
        Assert.Throws<ArgumentNullException>(() => XunitAssert.Fails(null!));
    }

    [Fact]
    public void The_public_passes_is_native_under_a_real_xunit_run()
    {
        var rule = new ArchUnitSharp.Files.Files(Graph(Self("src/App/Program.cs")))
            .Should()
            .HaveName("Car.cs");

        Assert.Throws<TrueException>(() => XunitAssert.Passes(rule));
    }

    private static Graph Graph(params Edge[] edges) => new(edges);

    private static Edge Self(string file) => new(file, file, external: false, ImportKind.None);
}
