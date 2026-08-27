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

    [Fact]
    public void HaveNoCycles_returns_no_violations_for_an_acyclic_graph()
    {
        var rule = new Files(Graph(
            Using("a.cs", "b.cs"),
            Using("b.cs", "c.cs"))).Should().HaveNoCycles();

        Assert.Empty(rule.Check());
    }

    [Fact]
    public void HaveNoCycles_reports_each_cycle_of_the_graph()
    {
        var rule = new Files(Graph(
            Using("a.cs", "b.cs"),
            Using("b.cs", "a.cs"))).Should().HaveNoCycles();

        IReadOnlyList<Violation> violations = rule.Check();

        Assert.Equal(
            new Violation[] { new CycleViolation(new[] { "a.cs", "b.cs", "a.cs" }) },
            violations);
    }

    [Fact]
    public void HaveNoCycles_after_selectors_checks_the_narrowed_subgraph()
    {
        var files = new Files(Graph(
            Using("src/Models/A.cs", "src/Models/B.cs"),
            Using("src/Models/B.cs", "src/Models/A.cs"),
            Using("src/App/X.cs", "src/App/Y.cs"),
            Using("src/App/Y.cs", "src/App/X.cs"))).InFolder("src/App");

        IReadOnlyList<Violation> violations = files.Should().HaveNoCycles().Check();

        Assert.Equal(
            new Violation[] { new CycleViolation(new[] { "src/App/X.cs", "src/App/Y.cs", "src/App/X.cs" }) },
            violations);
    }

    [Fact]
    public void HaveNoCycles_guards_a_selection_that_matches_nothing()
    {
        var rule = new Files(Graph(Self("a.cs"))).WithName("Car.cs").Should().HaveNoCycles();

        IReadOnlyList<Violation> violations = rule.Check();

        Assert.Equal(
            new Violation[] { new EmptyTestViolation("project files with name 'Car.cs' should have no cycles") },
            violations);
    }

    [Fact]
    public void HaveNoCycles_honours_allow_empty_tests()
    {
        var rule = new Files(Graph(Self("a.cs"))).WithName("Car.cs").Should().HaveNoCycles();

        IReadOnlyList<Violation> violations = rule.Check(new CheckOptions { AllowEmptyTests = true });

        Assert.Empty(violations);
    }

    [Fact]
    public void Two_rules_off_one_selection_do_not_see_each_other()
    {
        var files = new Files(Graph(
            Using("a.cs", "b.cs"),
            Using("b.cs", "a.cs")));

        var noCycles = files.Should().HaveNoCycles();
        var exist = files.Should().Exist();

        Assert.Equal(new[] { new CycleViolation(new[] { "a.cs", "b.cs", "a.cs" }) }, noCycles.Check());
        Assert.Empty(exist.Check());
        Assert.Equal(new[] { "a.cs", "b.cs" }, files.Select());
    }

    [Fact]
    public void A_cycles_rule_can_be_checked_twice_and_reports_the_same_result()
    {
        var rule = new Files(Graph(
            Using("a.cs", "b.cs"),
            Using("b.cs", "a.cs"))).Should().HaveNoCycles();

        IReadOnlyList<Violation> first = rule.Check();
        IReadOnlyList<Violation> second = rule.Check();

        Assert.Equal(first, second);
        Assert.NotSame(first, second);
    }

    [Fact]
    public void DependOn_returns_the_depend_on_predicate_for_the_positive_mood()
    {
        var rule = new Files(Graph(
            Self("src/App/Program.cs"),
            Self("src/Models/Car.cs"),
            Using("src/App/Program.cs", "src/Models/Car.cs")))
            .InFolder("src/App")
            .Should()
            .DependOn()
            .InFolder("src/Models");

        Assert.Empty(rule.Check());
    }

    [Fact]
    public void DependOn_guards_a_selection_that_matches_nothing_through_the_mood()
    {
        var rule = new Files(Graph(Self("a.cs"))).WithName("Car.cs").Should().DependOn().WithName("Truck.cs");

        IReadOnlyList<Violation> violations = rule.Check();

        Assert.Equal(
            new Violation[]
            {
                new EmptyTestViolation(
                    "project files with name 'Car.cs' should depend on files with name 'Truck.cs'"),
            },
            violations);
    }

    [Fact]
    public void HaveName_passes_when_every_selected_file_matches()
    {
        var rule = new Files(Graph(Self("a.cs"), Self("b.cs"))).Should().HaveName("*.cs");

        Assert.Empty(rule.Check());
    }

    [Fact]
    public void HaveName_flags_every_file_that_does_not_match()
    {
        var rule = new Files(Graph(Self("Car.cs"), Self("Truck.cs"))).Should().HaveName("Car.cs");

        IReadOnlyList<Violation> violations = rule.Check();

        Assert.Equal(new[] { new FileViolation("Truck.cs") }, violations);
    }

    [Fact]
    public void HaveName_after_selectors_checks_only_the_selected_files()
    {
        var files = new Files(Graph(
            Self("src/Models/Car.cs"),
            Self("src/Models/Truck.cs"),
            Self("src/App/Program.cs"))).InFolder("src/Models");

        IReadOnlyList<Violation> violations = files.Should().HaveName("Car.cs").Check();

        Assert.Equal(new[] { new FileViolation("src/Models/Truck.cs") }, violations);
    }

    [Fact]
    public void HaveName_guards_a_selection_that_matches_nothing()
    {
        var rule = new Files(Graph(Self("a.cs"))).WithName("Car.cs").Should().HaveName("Truck.cs");

        IReadOnlyList<Violation> violations = rule.Check();

        Assert.Equal(
            new Violation[] { new EmptyTestViolation("project files with name 'Car.cs' should have name 'Truck.cs'") },
            violations);
    }

    [Fact]
    public void HaveName_honours_allow_empty_tests()
    {
        var rule = new Files(Graph(Self("a.cs"))).WithName("Car.cs").Should().HaveName("Truck.cs");

        IReadOnlyList<Violation> violations = rule.Check(new CheckOptions { AllowEmptyTests = true });

        Assert.Empty(violations);
    }

    [Fact]
    public void HaveName_rejects_a_null_glob()
    {
        Assert.Throws<ArgumentNullException>(() => new Files(Graph(Self("a.cs"))).Should().HaveName(null!));
    }

    [Fact]
    public void HaveName_rejects_an_empty_glob()
    {
        Assert.Throws<ArgumentException>(() => new Files(Graph(Self("a.cs"))).Should().HaveName(string.Empty));
    }

    [Fact]
    public void BeInFolder_passes_when_every_selected_file_is_in_the_folder()
    {
        var rule = new Files(Graph(Self("src/Models/Car.cs"), Self("src/Models/Truck.cs")))
            .Should().BeInFolder("src/Models");

        Assert.Empty(rule.Check());
    }

    [Fact]
    public void BeInFolder_flags_every_file_that_is_not_in_the_folder()
    {
        var rule = new Files(Graph(Self("src/Models/Car.cs"), Self("src/App/Program.cs")))
            .Should().BeInFolder("src/Models");

        IReadOnlyList<Violation> violations = rule.Check();

        Assert.Equal(new[] { new FileViolation("src/App/Program.cs") }, violations);
    }

    [Fact]
    public void BeInFolder_guards_a_selection_that_matches_nothing()
    {
        var rule = new Files(Graph(Self("a.cs"))).InFolder("src/Models").Should().BeInFolder("src/App");

        IReadOnlyList<Violation> violations = rule.Check();

        Assert.Equal(
            new Violation[] { new EmptyTestViolation("project files in folder 'src/Models' should be in folder 'src/App'") },
            violations);
    }

    [Fact]
    public void BeInFolder_honours_allow_empty_tests()
    {
        var rule = new Files(Graph(Self("a.cs"))).InFolder("src/Models").Should().BeInFolder("src/App");

        IReadOnlyList<Violation> violations = rule.Check(new CheckOptions { AllowEmptyTests = true });

        Assert.Empty(violations);
    }

    [Fact]
    public void BeInFolder_rejects_a_null_glob()
    {
        Assert.Throws<ArgumentNullException>(() => new Files(Graph(Self("a.cs"))).Should().BeInFolder(null!));
    }

    [Fact]
    public void BeInFolder_rejects_an_empty_glob()
    {
        Assert.Throws<ArgumentException>(() => new Files(Graph(Self("a.cs"))).Should().BeInFolder(string.Empty));
    }

    [Fact]
    public void BeInPath_passes_when_every_selected_file_is_at_the_path()
    {
        var rule = new Files(Graph(Self("src/Models/Car.cs")))
            .Should().BeInPath("src/Models/Car.cs");

        Assert.Empty(rule.Check());
    }

    [Fact]
    public void BeInPath_flags_every_file_that_is_not_at_the_path()
    {
        var rule = new Files(Graph(Self("src/Models/Car.cs"), Self("src/App/Program.cs")))
            .Should().BeInPath("src/Models/Car.cs");

        IReadOnlyList<Violation> violations = rule.Check();

        Assert.Equal(new[] { new FileViolation("src/App/Program.cs") }, violations);
    }

    [Fact]
    public void BeInPath_guards_a_selection_that_matches_nothing()
    {
        var rule = new Files(Graph(Self("a.cs"))).InPath("src/Models/Car.cs").Should().BeInPath("src/App");

        IReadOnlyList<Violation> violations = rule.Check();

        Assert.Equal(
            new Violation[] { new EmptyTestViolation("project files in path 'src/Models/Car.cs' should be in path 'src/App'") },
            violations);
    }

    [Fact]
    public void BeInPath_honours_allow_empty_tests()
    {
        var rule = new Files(Graph(Self("a.cs"))).InPath("src/Models/Car.cs").Should().BeInPath("src/App");

        IReadOnlyList<Violation> violations = rule.Check(new CheckOptions { AllowEmptyTests = true });

        Assert.Empty(violations);
    }

    [Fact]
    public void BeInPath_rejects_a_null_glob()
    {
        Assert.Throws<ArgumentNullException>(() => new Files(Graph(Self("a.cs"))).Should().BeInPath(null!));
    }

    [Fact]
    public void BeInPath_rejects_an_empty_glob()
    {
        Assert.Throws<ArgumentException>(() => new Files(Graph(Self("a.cs"))).Should().BeInPath(string.Empty));
    }

    [Fact]
    public void Two_name_rules_off_one_selection_do_not_see_each_other()
    {
        var files = new Files(Graph(Self("Car.cs"), Self("Truck.cs")));

        var cars = files.Should().HaveName("Car.cs");
        var trucks = files.Should().HaveName("Truck.cs");

        Assert.Equal(new[] { new FileViolation("Truck.cs") }, cars.Check());
        Assert.Equal(new[] { new FileViolation("Car.cs") }, trucks.Check());
        Assert.Equal(new[] { "Car.cs", "Truck.cs" }, files.Select());
    }

    [Fact]
    public void A_have_name_rule_can_be_checked_twice_and_reports_the_same_result()
    {
        var rule = new Files(Graph(Self("Car.cs"), Self("Truck.cs"))).Should().HaveName("Car.cs");

        IReadOnlyList<Violation> first = rule.Check();
        IReadOnlyList<Violation> second = rule.Check();

        Assert.Equal(first, second);
        Assert.NotSame(first, second);
    }

    [Fact]
    public void AdhereTo_passes_when_every_selected_file_satisfies_the_predicate()
    {
        var rule = new Files(Graph(Self("a.cs"), Self("b.cs")), Reader("namespace App; public class X { }"))
            .Should()
            .AdhereTo(static detail => detail.NonBlankLineCount <= 2, "every file is short");

        Assert.Empty(rule.Check());
    }

    [Fact]
    public void AdhereTo_flags_every_file_the_predicate_rejects()
    {
        var rule = new Files(Graph(Self("Car.cs"), Self("Truck.cs")), Reader("text"))
            .Should()
            .AdhereTo(static detail => detail.NameWithoutExtension == "Car", "is named Car");

        IReadOnlyList<Violation> violations = rule.Check();

        Assert.Equal(
            new Violation[] { new AdhereToViolation("Truck.cs", "is named Car") },
            violations);
    }

    [Fact]
    public void AdhereTo_after_selectors_checks_only_the_selected_files()
    {
        var rule = new Files(Graph(
            Self("src/Models/Car.cs"),
            Self("src/Models/Truck.cs"),
            Self("src/App/Program.cs")), Reader("text"))
            .InFolder("src/Models")
            .Should()
            .AdhereTo(static detail => detail.NameWithoutExtension == "Car", "is named Car");

        IReadOnlyList<Violation> violations = rule.Check();

        Assert.Equal(
            new Violation[] { new AdhereToViolation("src/Models/Truck.cs", "is named Car") },
            violations);
    }

    [Fact]
    public void AdhereTo_guards_a_selection_that_matches_nothing()
    {
        var rule = new Files(Graph(Self("a.cs")), Reader("text"))
            .WithName("Car.cs")
            .Should()
            .AdhereTo(static _ => true, "message");

        IReadOnlyList<Violation> violations = rule.Check();

        Assert.Equal(
            new Violation[] { new EmptyTestViolation("project files with name 'Car.cs' should adhere to 'message'") },
            violations);
    }

    [Fact]
    public void AdhereTo_honours_allow_empty_tests()
    {
        var rule = new Files(Graph(Self("a.cs")), Reader("text"))
            .WithName("Car.cs")
            .Should()
            .AdhereTo(static _ => true, "message");

        IReadOnlyList<Violation> violations = rule.Check(new CheckOptions { AllowEmptyTests = true });

        Assert.Empty(violations);
    }

    [Fact]
    public void AdhereTo_rejects_a_null_predicate()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new Files(Graph(Self("a.cs")), Reader("text")).Should().AdhereTo(null!, "message"));
    }

    [Fact]
    public void AdhereTo_rejects_a_null_message()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new Files(Graph(Self("a.cs")), Reader("text")).Should().AdhereTo(static _ => true, null!));
    }

    [Fact]
    public void AdhereTo_rejects_an_empty_message()
    {
        Assert.Throws<ArgumentException>(() =>
            new Files(Graph(Self("a.cs")), Reader("text")).Should().AdhereTo(static _ => true, string.Empty));
    }

    [Fact]
    public void AdhereTo_without_sources_raises_a_user_error()
    {
        var rule = new Files(Graph(Self("a.cs"))).Should().AdhereTo(static _ => true, "message");

        Assert.Throws<UserError>(() => rule.Check());
    }

    [Fact]
    public void Two_adhere_to_rules_off_one_selection_do_not_see_each_other()
    {
        var files = new Files(Graph(Self("Car.cs"), Self("Truck.cs")), Reader("text"));

        var cars = files.Should().AdhereTo(static detail => detail.NameWithoutExtension == "Car", "is named Car");
        var trucks = files.Should().AdhereTo(static detail => detail.NameWithoutExtension == "Truck", "is named Truck");

        Assert.Equal(new[] { new AdhereToViolation("Truck.cs", "is named Car") }, cars.Check());
        Assert.Equal(new[] { new AdhereToViolation("Car.cs", "is named Truck") }, trucks.Check());
        Assert.Equal(new[] { "Car.cs", "Truck.cs" }, files.Select());
    }

    [Fact]
    public void An_adhere_to_rule_can_be_checked_twice_and_reports_the_same_result()
    {
        var rule = new Files(Graph(Self("Car.cs")), Reader("text"))
            .Should()
            .AdhereTo(static _ => false, "message");

        IReadOnlyList<Violation> first = rule.Check();
        IReadOnlyList<Violation> second = rule.Check();

        Assert.Equal(first, second);
        Assert.NotSame(first, second);
    }

    private static Func<string, string> Reader(string content) => _ => content;

    private static Graph Graph(params Edge[] edges) => new(edges);

    private static Edge Self(string file) => new(file, file, external: false, ImportKind.None);

    private static Edge Using(string source, string target) =>
        new(source, target, external: false, ImportKind.Using);
}
