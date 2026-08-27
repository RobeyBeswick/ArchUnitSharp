using ArchUnitSharp.Common.Extraction;
using Xunit.Sdk;

namespace ArchUnitSharp.Testing.Xunit.Tests;

public class RuleExtensionsIntegrationTests
{
    [Fact]
    public void AssertPasses_reads_as_a_native_assertion_on_a_passing_chain()
    {
        new ArchUnitSharp.Files.Files(Graph(Self("a.cs"), Self("b.cs")))
            .Should()
            .HaveNoCycles()
            .AssertPasses();
    }

    [Fact]
    public void AssertPasses_asserts_a_negated_rule_through_its_should_not_mood()
    {
        new ArchUnitSharp.Files.Files(Graph(
            Self("src/App/Program.cs"),
            Self("src/Models/Car.cs"),
            Self("src/Utils/Helper.cs"),
            Using("src/App/Program.cs", "src/Utils/Helper.cs")))
            .InFolder("src/App")
            .ShouldNot()
            .DependOn()
            .InFolder("src/Models")
            .AssertPasses();
    }

    [Fact]
    public void AssertPasses_throws_the_native_true_exception_with_the_report_message()
    {
        TrueException failure = Assert.Throws<TrueException>(() =>
            new ArchUnitSharp.Files.Files(Graph(
                Self("a.cs"),
                Self("b.cs"),
                Using("a.cs", "b.cs"),
                Using("b.cs", "a.cs")))
                .Should()
                .HaveNoCycles()
                .AssertPasses());

        Assert.Equal("Cycle: a.cs → b.cs → a.cs", failure.Message);
    }

    [Fact]
    public void AssertFails_asserts_a_rule_that_does_not_hold()
    {
        new ArchUnitSharp.Files.Files(Graph(
            Self("a.cs"),
            Self("b.cs"),
            Using("a.cs", "b.cs"),
            Using("b.cs", "a.cs")))
            .Should()
            .HaveNoCycles()
            .AssertFails();
    }

    [Fact]
    public void AssertFails_throws_the_native_false_exception_when_the_rule_passes()
    {
        FalseException failure = Assert.Throws<FalseException>(() =>
            new ArchUnitSharp.Files.Files(Graph(Self("a.cs"), Self("b.cs")))
                .Should()
                .HaveNoCycles()
                .AssertFails());

        Assert.Equal(ResultFactory.PassLine, failure.Message);
    }

    [Fact]
    public void AssertPasses_accepts_options_through_the_fluent_chain()
    {
        new ArchUnitSharp.Files.Files(Graph(Self("src/Models/Car.cs")))
            .InPath("src/NoSuchFile.cs")
            .Should()
            .Exist()
            .AssertPasses(new CheckOptions { AllowEmptyTests = true });
    }

    [Fact]
    public void AssertPasses_propagates_a_user_error_from_the_check_unchanged()
    {
        Assert.Throws<UserError>(() =>
            new ArchUnitSharp.Files.Files(Graph(Self("src/App/Program.cs")))
                .Should()
                .AdhereTo(static _ => true, "the rule's message")
                .AssertPasses());
    }

    private static Graph Graph(params Edge[] edges) => new(edges);

    private static Edge Self(string file) => new(file, file, external: false, ImportKind.None);

    private static Edge Using(string source, string target) =>
        new(source, target, external: false, ImportKind.Using);
}
