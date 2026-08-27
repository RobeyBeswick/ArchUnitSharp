using ArchUnitSharp.Common.Extraction;

namespace ArchUnitSharp.Testing.Tests;

public class RuleAssertTests
{
    [Fact]
    public void A_passing_rule_returns_normally()
    {
        RuleAssert.Passes(new ArchUnitSharp.Files.Files(Graph(Self("src/App/Program.cs"))).Should().Exist());
    }

    [Fact]
    public void A_failing_rule_raises_the_formatted_message()
    {
        var rule = new ArchUnitSharp.Files.Files(Graph(Self("src/App/Program.cs")))
            .Should()
            .HaveName("Car.cs");

        AssertionFailedException failure =
            Assert.Throws<AssertionFailedException>(() => RuleAssert.Passes(rule));

        Assert.Equal("File 'src/App/Program.cs' violates the rule.", failure.Message);
        Assert.False(failure.Result.Passed);
    }

    [Fact]
    public void A_rule_with_several_violations_raises_them_joined_with_a_newline()
    {
        var rule = new ArchUnitSharp.Files.Files(Graph(Self("src/A.cs"), Self("src/B.cs")))
            .Should()
            .HaveName("Car.cs");

        AssertionFailedException failure =
            Assert.Throws<AssertionFailedException>(() => RuleAssert.Passes(rule));

        Assert.Equal(
            "File 'src/A.cs' violates the rule.\nFile 'src/B.cs' violates the rule.",
            failure.Message);
    }

    [Fact]
    public void A_rule_that_matches_nothing_raises_the_empty_test_message_by_default()
    {
        var rule = new ArchUnitSharp.Files.Files(Graph(Self("src/Models/Car.cs")))
            .InPath("src/NoSuchFile.cs")
            .Should()
            .Exist();

        AssertionFailedException failure =
            Assert.Throws<AssertionFailedException>(() => RuleAssert.Passes(rule));

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

        RuleAssert.Passes(rule, new CheckOptions { AllowEmptyTests = true });
    }

    [Fact]
    public void A_user_error_from_the_check_propagates_unchanged()
    {
        var rule = new ArchUnitSharp.Files.Files(Graph(Self("src/App/Program.cs")))
            .Should()
            .AdhereTo(static _ => true, "the rule's message");

        UserError error = Assert.Throws<UserError>(() => RuleAssert.Passes(rule));

        Assert.Equal(
            "Source text is not available for file 'src/App/Program.cs': this selection was built "
            + "from a graph without its source files. Build the selection from "
            + "Project.ProjectFiles(...) to run adhere-to rules.",
            error.Message);
    }

    [Fact]
    public void A_failing_rule_makes_Fails_return_normally()
    {
        var rule = new ArchUnitSharp.Files.Files(Graph(Self("src/App/Program.cs")))
            .Should()
            .HaveName("Car.cs");

        RuleAssert.Fails(rule);
    }

    [Fact]
    public void A_passing_rule_makes_Fails_raise_the_unexpected_pass_message()
    {
        var rule = new ArchUnitSharp.Files.Files(Graph(Self("src/App/Program.cs"))).Should().Exist();

        AssertionFailedException failure =
            Assert.Throws<AssertionFailedException>(() => RuleAssert.Fails(rule));

        Assert.Equal("The rule passed, but the assertion expected it to fail.", failure.Message);
        Assert.False(failure.Result.Passed);
    }

    [Fact]
    public void A_rule_that_matches_nothing_makes_Fails_pass_by_default()
    {
        var rule = new ArchUnitSharp.Files.Files(Graph(Self("src/Models/Car.cs")))
            .InPath("src/NoSuchFile.cs")
            .Should()
            .Exist();

        RuleAssert.Fails(rule);
    }

    [Fact]
    public void AllowEmptyTests_makes_an_empty_rule_pass_so_Fails_raises()
    {
        var rule = new ArchUnitSharp.Files.Files(Graph(Self("src/Models/Car.cs")))
            .InPath("src/NoSuchFile.cs")
            .Should()
            .Exist();

        AssertionFailedException failure = Assert.Throws<AssertionFailedException>(
            () => RuleAssert.Fails(rule, new CheckOptions { AllowEmptyTests = true }));

        Assert.Equal("The rule passed, but the assertion expected it to fail.", failure.Message);
    }

    [Fact]
    public void Fails_propagates_a_user_error_from_the_check_unchanged()
    {
        var rule = new ArchUnitSharp.Files.Files(Graph(Self("src/App/Program.cs")))
            .Should()
            .AdhereTo(static _ => true, "the rule's message");

        Assert.Throws<UserError>(() => RuleAssert.Fails(rule));
    }

    [Fact]
    public void Passes_rejects_a_null_rule()
    {
        Assert.Throws<ArgumentNullException>(() => RuleAssert.Passes(null!));
    }

    [Fact]
    public void Fails_rejects_a_null_rule()
    {
        Assert.Throws<ArgumentNullException>(() => RuleAssert.Fails(null!));
    }

    private static Graph Graph(params Edge[] edges) => new(edges);

    private static Edge Self(string file) => new(file, file, external: false, ImportKind.None);
}
