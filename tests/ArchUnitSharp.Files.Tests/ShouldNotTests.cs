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

    [Fact]
    public void DependOn_returns_the_depend_on_predicate_for_the_negated_mood()
    {
        var rule = new Files(Graph(
            Self("src/App/Program.cs"),
            Self("src/Models/Car.cs"),
            Using("src/App/Program.cs", "src/Models/Car.cs")))
            .InFolder("src/App")
            .ShouldNot()
            .DependOn()
            .InFolder("src/Models");

        IReadOnlyList<Violation> violations = rule.Check();

        Assert.Equal(
            new Violation[] { new DependencyViolation("src/App/Program.cs", "src/Models/Car.cs") },
            violations);
    }

    [Fact]
    public void DependOn_guards_an_object_that_matches_nothing_through_the_mood()
    {
        var rule = new Files(Graph(Self("a.cs"), Self("b.cs")))
            .ShouldNot()
            .DependOn()
            .WithName("Car.cs");

        IReadOnlyList<Violation> violations = rule.Check();

        Assert.Equal(
            new Violation[]
            {
                new EmptyTestViolation("project files should not depend on files with name 'Car.cs'"),
            },
            violations);
    }

    [Fact]
    public void HaveName_flags_every_file_that_matches()
    {
        var rule = new Files(Graph(Self("Car.cs"), Self("Truck.cs"))).ShouldNot().HaveName("Car.cs");

        IReadOnlyList<Violation> violations = rule.Check();

        Assert.Equal(new[] { new FileViolation("Car.cs") }, violations);
    }

    [Fact]
    public void BeInFolder_flags_every_file_in_the_folder()
    {
        var rule = new Files(Graph(Self("src/Models/Car.cs"), Self("src/App/Program.cs")))
            .ShouldNot().BeInFolder("src/Models");

        IReadOnlyList<Violation> violations = rule.Check();

        Assert.Equal(new[] { new FileViolation("src/Models/Car.cs") }, violations);
    }

    [Fact]
    public void BeInPath_flags_every_file_at_the_path()
    {
        var rule = new Files(Graph(Self("src/Models/Car.cs"), Self("src/App/Program.cs")))
            .ShouldNot().BeInPath("src/Models/Car.cs");

        IReadOnlyList<Violation> violations = rule.Check();

        Assert.Equal(new[] { new FileViolation("src/Models/Car.cs") }, violations);
    }

    [Fact]
    public void HaveName_after_selectors_flags_only_the_matching_files()
    {
        var rule = new Files(Graph(
            Self("src/Models/Car.cs"),
            Self("src/Models/Truck.cs"),
            Self("src/App/Car.cs")))
            .InFolder("src/Models")
            .ShouldNot()
            .HaveName("Car.cs");

        IReadOnlyList<Violation> violations = rule.Check();

        Assert.Equal(new[] { new FileViolation("src/Models/Car.cs") }, violations);
    }

    [Fact]
    public void HaveName_guards_a_selection_that_matches_nothing()
    {
        var rule = new Files(Graph(Self("a.cs"))).WithName("Car.cs").ShouldNot().HaveName("Truck.cs");

        IReadOnlyList<Violation> violations = rule.Check();

        Assert.Equal(
            new Violation[] { new EmptyTestViolation("project files with name 'Car.cs' should not have name 'Truck.cs'") },
            violations);
    }

    [Fact]
    public void HaveName_honours_allow_empty_tests()
    {
        var rule = new Files(Graph(Self("a.cs"))).WithName("Car.cs").ShouldNot().HaveName("Truck.cs");

        IReadOnlyList<Violation> violations = rule.Check(new CheckOptions { AllowEmptyTests = true });

        Assert.Empty(violations);
    }

    [Fact]
    public void HaveName_rejects_a_null_glob()
    {
        Assert.Throws<ArgumentNullException>(() => new Files(Graph(Self("a.cs"))).ShouldNot().HaveName(null!));
    }

    [Fact]
    public void HaveName_rejects_an_empty_glob()
    {
        Assert.Throws<ArgumentException>(() => new Files(Graph(Self("a.cs"))).ShouldNot().HaveName(string.Empty));
    }

    [Fact]
    public void BeInFolder_rejects_a_null_glob()
    {
        Assert.Throws<ArgumentNullException>(() => new Files(Graph(Self("a.cs"))).ShouldNot().BeInFolder(null!));
    }

    [Fact]
    public void BeInFolder_rejects_an_empty_glob()
    {
        Assert.Throws<ArgumentException>(() => new Files(Graph(Self("a.cs"))).ShouldNot().BeInFolder(string.Empty));
    }

    [Fact]
    public void BeInPath_rejects_a_null_glob()
    {
        Assert.Throws<ArgumentNullException>(() => new Files(Graph(Self("a.cs"))).ShouldNot().BeInPath(null!));
    }

    [Fact]
    public void BeInPath_rejects_an_empty_glob()
    {
        Assert.Throws<ArgumentException>(() => new Files(Graph(Self("a.cs"))).ShouldNot().BeInPath(string.Empty));
    }

    [Fact]
    public void Two_moods_off_one_selection_do_not_see_each_other()
    {
        var files = new Files(Graph(Self("Car.cs"), Self("Truck.cs")));

        var should = files.Should();
        var shouldNot = files.ShouldNot();

        Assert.Empty(should.HaveName("*.cs").Check());
        Assert.Equal(2, shouldNot.HaveName("*.cs").Check().Count);
        Assert.Equal(new[] { "Car.cs", "Truck.cs" }, files.Select());
    }

    [Fact]
    public void A_have_name_rule_can_be_checked_twice_and_reports_the_same_result()
    {
        var rule = new Files(Graph(Self("Car.cs"), Self("Truck.cs"))).ShouldNot().HaveName("Car.cs");

        IReadOnlyList<Violation> first = rule.Check();
        IReadOnlyList<Violation> second = rule.Check();

        Assert.Equal(first, second);
        Assert.NotSame(first, second);
    }

    private static Graph Graph(params Edge[] edges) => new(edges);

    private static Edge Self(string file) => new(file, file, external: false, ImportKind.None);

    private static Edge Using(string source, string target) =>
        new(source, target, external: false, ImportKind.Using);
}
