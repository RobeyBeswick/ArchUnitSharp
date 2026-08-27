using ArchUnitSharp.Common.Extraction;

namespace ArchUnitSharp.Testing.Tests;

public class ResultFactoryIntegrationTests
{
    [Fact]
    public void A_passing_rule_through_the_fluent_surface_shapes_as_a_pass()
    {
        IReadOnlyList<Violation> violations =
            new ArchUnitSharp.Files.Files(Graph(Self("a.cs"), Self("b.cs"))).Should().Exist().Check();

        CheckResult result = ResultFactory.Create(violations);

        Assert.True(result.Passed);
        Assert.Equal(ResultFactory.PassLine, result.Message);
    }

    [Fact]
    public void A_failing_rule_through_the_fluent_surface_shapes_its_cycle_message()
    {
        IReadOnlyList<Violation> violations = new ArchUnitSharp.Files.Files(Graph(
            Using("src/A.cs", "src/B.cs"),
            Using("src/B.cs", "src/A.cs"))).Should().HaveNoCycles().Check();

        CheckResult result = ResultFactory.Create(violations);

        Assert.False(result.Passed);
        Assert.Equal("Cycle: src/A.cs → src/B.cs → src/A.cs", result.Message);
    }

    [Fact]
    public void An_empty_rule_through_the_fluent_surface_shapes_the_empty_test_message()
    {
        IReadOnlyList<Violation> violations = new ArchUnitSharp.Files.Files(Graph(Self("src/Models/Car.cs")))
            .InPath("src/NoSuchFile.cs")
            .Should()
            .Exist()
            .Check();

        CheckResult result = ResultFactory.Create(violations);

        Assert.False(result.Passed);
        Assert.Equal(
            "The rule matched nothing: project files in path 'src/NoSuchFile.cs' should exist.",
            result.Message);
    }

    private static Graph Graph(params Edge[] edges) => new(edges);

    private static Edge Self(string file) => new(file, file, external: false, ImportKind.None);

    private static Edge Using(string source, string target) =>
        new(source, target, external: false, ImportKind.Using);
}
