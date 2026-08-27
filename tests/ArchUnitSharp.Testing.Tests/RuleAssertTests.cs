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
    public void A_null_rule_is_rejected()
    {
        Assert.Throws<ArgumentNullException>(() => RuleAssert.Passes(null!));
    }

    private static Graph Graph(params Edge[] edges) => new(edges);

    private static Edge Self(string file) => new(file, file, external: false, ImportKind.None);
}
