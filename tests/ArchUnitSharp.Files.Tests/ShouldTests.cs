using ArchUnitSharp.Common.Extraction;

namespace ArchUnitSharp.Files.Tests;

public class ShouldTests
{
    [Fact]
    public void Exist_returns_no_violations_for_a_non_empty_selection()
    {
        var rule = new Files(Graph(Self("a.cs"), Self("b.cs"))).Should().Exist();

        Assert.Empty(rule.Check());
    }

    [Fact]
    public void Exist_passes_through_the_fluent_chain()
    {
        var files = new Files(Graph(Self("a.cs"), Self("b.cs")));

        Assert.Empty(files.Should().Exist().Check());
    }

    [Fact]
    public void Exist_after_selectors_checks_the_narrowed_selection()
    {
        var files = new Files(Graph(Self("src/Models/Car.cs"), Self("src/App/Program.cs")))
            .InFolder("src/Models");

        Assert.Equal(new[] { "src/Models/Car.cs" }, files.Select());
        Assert.Empty(files.Should().Exist().Check());
    }

    [Fact]
    public void Exist_guards_a_selection_that_matches_nothing()
    {
        var rule = new Files(Graph(Self("a.cs"))).WithName("Car.cs").Should().Exist();

        IReadOnlyList<Violation> violations = rule.Check();

        Assert.Equal(
            new Violation[] { new EmptyTestViolation("project files with name 'Car.cs' should exist") },
            violations);
    }

    [Fact]
    public void Exist_honours_allow_empty_tests()
    {
        var rule = new Files(Graph(Self("a.cs"))).WithName("Car.cs").Should().Exist();

        IReadOnlyList<Violation> violations = rule.Check(new CheckOptions { AllowEmptyTests = true });

        Assert.Empty(violations);
    }

    [Fact]
    public void A_rule_can_be_checked_twice_and_reports_the_same_result()
    {
        var rule = new Files(Graph(Self("a.cs"), Self("b.cs"))).Should().Exist();

        IReadOnlyList<Violation> first = rule.Check();
        IReadOnlyList<Violation> second = rule.Check();

        Assert.Equal(first, second);
        Assert.NotSame(first, second);
    }

    [Fact]
    public void Two_moods_off_one_selection_do_not_see_each_other()
    {
        var files = new Files(Graph(Self("a.cs"), Self("b.cs")));

        var should = files.Should();
        var shouldNot = files.ShouldNot();

        Assert.Empty(should.Exist().Check());
        Assert.Equal(2, shouldNot.Exist().Check().Count);
        Assert.Equal(new[] { "a.cs", "b.cs" }, files.Select());
    }

    private static Graph Graph(params Edge[] edges) => new(edges);

    private static Edge Self(string file) => new(file, file, external: false, ImportKind.None);
}
