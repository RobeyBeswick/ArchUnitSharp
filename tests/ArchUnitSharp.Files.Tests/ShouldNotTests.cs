using ArchUnitSharp.Common.Extraction;

namespace ArchUnitSharp.Files.Tests;

public class ShouldNotTests
{
    [Fact]
    public void Exist_flags_every_selected_file()
    {
        var rule = new Files(Graph(Self("a.cs"), Self("b.cs"))).ShouldNot().Exist();

        IReadOnlyList<Violation> violations = rule.Check();

        Assert.Equal(
            new Violation[] { new FileViolation("a.cs"), new FileViolation("b.cs") },
            violations);
    }

    [Fact]
    public void Exist_after_selectors_flags_only_the_matching_files()
    {
        var rule = new Files(Graph(Self("src/Models/Car.cs"), Self("src/App/Program.cs")))
            .InFolder("src/Models")
            .ShouldNot()
            .Exist();

        IReadOnlyList<Violation> violations = rule.Check();

        Assert.Equal(new[] { new FileViolation("src/Models/Car.cs") }, violations);
    }

    [Fact]
    public void Exist_guards_a_selection_that_matches_nothing()
    {
        var rule = new Files(Graph(Self("a.cs"))).WithName("Car.cs").ShouldNot().Exist();

        IReadOnlyList<Violation> violations = rule.Check();

        Assert.Equal(
            new Violation[] { new EmptyTestViolation("project files with name 'Car.cs' should not exist") },
            violations);
    }

    [Fact]
    public void Exist_honours_allow_empty_tests()
    {
        var rule = new Files(Graph(Self("a.cs"))).WithName("Car.cs").ShouldNot().Exist();

        IReadOnlyList<Violation> violations = rule.Check(new CheckOptions { AllowEmptyTests = true });

        Assert.Empty(violations);
    }

    [Fact]
    public void Building_the_mood_leaves_the_selection_unchanged()
    {
        var files = new Files(Graph(Self("a.cs"), Self("b.cs")));

        files.ShouldNot();

        Assert.Equal(new[] { "a.cs", "b.cs" }, files.Select());
    }

    [Fact]
    public void A_rule_can_be_checked_twice_and_reports_the_same_result()
    {
        var rule = new Files(Graph(Self("a.cs"), Self("b.cs"))).ShouldNot().Exist();

        IReadOnlyList<Violation> first = rule.Check();
        IReadOnlyList<Violation> second = rule.Check();

        Assert.Equal(first, second);
        Assert.NotSame(first, second);
    }

    private static Graph Graph(params Edge[] edges) => new(edges);

    private static Edge Self(string file) => new(file, file, external: false, ImportKind.None);
}
