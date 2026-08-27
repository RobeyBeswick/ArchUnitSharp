using ArchUnitSharp.Common.Extraction;

namespace ArchUnitSharp.Files.Tests;

public class DependOnTests
{
    [Fact]
    public void DependOn_passes_through_the_fluent_chain()
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
    public void DependOn_reports_each_offending_dependency_through_the_fluent_chain()
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
    public void DependOn_passes_through_InPath()
    {
        var rule = new Files(Graph(
            Self("src/App/Program.cs"),
            Self("src/Models/Car.cs"),
            Using("src/App/Program.cs", "src/Models/Car.cs")))
            .InFolder("src/App")
            .Should()
            .DependOn()
            .InPath("src/Models/Car.cs");

        Assert.Empty(rule.Check());
    }

    [Fact]
    public void DependOn_passes_through_InFile()
    {
        var rule = new Files(Graph(
            Self("src/App/Program.cs"),
            Self("src/Models/Car.cs"),
            Using("src/App/Program.cs", "src/Models/Car.cs")))
            .InFolder("src/App")
            .Should()
            .DependOn()
            .InFile("src.Models.Car");

        Assert.Empty(rule.Check());
    }

    [Fact]
    public void DependOn_reports_an_offending_dependency_through_InPath()
    {
        var rule = new Files(Graph(
            Self("src/App/Program.cs"),
            Self("src/Models/Car.cs"),
            Using("src/App/Program.cs", "src/Models/Car.cs")))
            .InFolder("src/App")
            .ShouldNot()
            .DependOn()
            .InPath("src/Models/Car.cs");

        Assert.Equal(
            new Violation[] { new DependencyViolation("src/App/Program.cs", "src/Models/Car.cs") },
            rule.Check());
    }

    [Fact]
    public void DependOn_reports_an_offending_dependency_through_InFile()
    {
        var rule = new Files(Graph(
            Self("src/App/Program.cs"),
            Self("src/Models/Car.cs"),
            Using("src/App/Program.cs", "src/Models/Car.cs")))
            .InFolder("src/App")
            .ShouldNot()
            .DependOn()
            .InFile("src.Models.Car");

        Assert.Equal(
            new Violation[] { new DependencyViolation("src/App/Program.cs", "src/Models/Car.cs") },
            rule.Check());
    }

    [Fact]
    public void Object_selectors_combine_with_and()
    {
        var rule = new Files(Graph(
            Self("src/App/Program.cs"),
            Self("src/App/Other.cs"),
            Self("src/Models/Car.cs"),
            Self("src/Models/Truck.cs"),
            Using("src/App/Program.cs", "src/Models/Car.cs"),
            Using("src/App/Other.cs", "src/Models/Truck.cs")))
            .InFolder("src/App")
            .Should()
            .DependOn()
            .InFolder("src/Models")
            .WithName("Car.cs");

        IReadOnlyList<Violation> violations = rule.Check();

        Assert.Equal(new[] { new FileViolation("src/App/Other.cs") }, violations);
    }

    [Fact]
    public void A_selector_leaves_the_parent_object_unchanged()
    {
        var files = new Files(Graph(
            Self("src/App/Program.cs"),
            Self("src/Models/Car.cs"),
            Self("src/Models/Truck.cs"),
            Using("src/App/Program.cs", "src/Models/Car.cs"),
            Using("src/App/Program.cs", "src/Models/Truck.cs")))
            .InFolder("src/App");

        var parent = files.ShouldNot().DependOn().InFolder("src/Models");
        var narrowed = parent.WithName("Car.cs");

        Assert.Equal(
            new Violation[]
            {
                new DependencyViolation("src/App/Program.cs", "src/Models/Car.cs"),
                new DependencyViolation("src/App/Program.cs", "src/Models/Truck.cs"),
            },
            parent.Check());
        Assert.Equal(
            new Violation[] { new DependencyViolation("src/App/Program.cs", "src/Models/Car.cs") },
            narrowed.Check());
    }

    [Fact]
    public void Two_branches_off_one_parent_do_not_see_each_others_selectors()
    {
        var files = new Files(Graph(
            Self("src/App/Program.cs"),
            Self("src/Models/Car.cs"),
            Self("src/Util/Helper.cs"),
            Using("src/App/Program.cs", "src/Models/Car.cs"),
            Using("src/App/Program.cs", "src/Util/Helper.cs")))
            .InFolder("src/App");

        var parent = files.ShouldNot().DependOn();
        var cars = parent.WithName("Car.cs");
        var helpers = parent.WithName("Helper.cs");

        Assert.Equal(
            new Violation[]
            {
                new DependencyViolation("src/App/Program.cs", "src/Models/Car.cs"),
                new DependencyViolation("src/App/Program.cs", "src/Util/Helper.cs"),
            },
            parent.Check());
        Assert.Equal(
            new Violation[] { new DependencyViolation("src/App/Program.cs", "src/Models/Car.cs") },
            cars.Check());
        Assert.Equal(
            new Violation[] { new DependencyViolation("src/App/Program.cs", "src/Util/Helper.cs") },
            helpers.Check());
    }

    [Fact]
    public void Two_depend_on_rules_off_one_selection_do_not_see_each_other()
    {
        var files = new Files(Graph(
            Self("src/App/Program.cs"),
            Self("src/Models/Car.cs"),
            Self("src/Util/Helper.cs"),
            Using("src/App/Program.cs", "src/Models/Car.cs"),
            Using("src/App/Program.cs", "src/Util/Helper.cs")))
            .InFolder("src/App");

        var should = files.Should().DependOn().WithName("Car.cs");
        var shouldNot = files.ShouldNot().DependOn().WithName("Helper.cs");

        Assert.Empty(should.Check());
        Assert.Equal(
            new Violation[] { new DependencyViolation("src/App/Program.cs", "src/Util/Helper.cs") },
            shouldNot.Check());
        Assert.Equal(new[] { "src/App/Program.cs" }, files.Select());
    }

    [Fact]
    public void A_rule_can_be_checked_twice_and_reports_the_same_result()
    {
        var rule = new Files(Graph(
            Self("src/App/Program.cs"),
            Self("src/Models/Car.cs"),
            Using("src/App/Program.cs", "src/Models/Car.cs")))
            .InFolder("src/App")
            .ShouldNot()
            .DependOn()
            .InFolder("src/Models");

        IReadOnlyList<Violation> first = rule.Check();
        IReadOnlyList<Violation> second = rule.Check();

        Assert.Equal(first, second);
        Assert.NotSame(first, second);
    }

    [Fact]
    public void DependOn_guards_a_selection_that_matches_nothing()
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
    public void DependOn_honours_allow_empty_tests()
    {
        var rule = new Files(Graph(Self("a.cs"))).WithName("Car.cs").Should().DependOn().WithName("Truck.cs");

        IReadOnlyList<Violation> violations = rule.Check(new CheckOptions { AllowEmptyTests = true });

        Assert.Empty(violations);
    }

    [Fact]
    public void WithName_rejects_a_null_glob()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new Files(Graph(Self("a.cs"))).Should().DependOn().WithName(null!));
    }

    [Fact]
    public void WithName_rejects_an_empty_glob()
    {
        Assert.Throws<ArgumentException>(() =>
            new Files(Graph(Self("a.cs"))).Should().DependOn().WithName(string.Empty));
    }

    [Fact]
    public void InFolder_rejects_a_null_glob()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new Files(Graph(Self("a.cs"))).Should().DependOn().InFolder(null!));
    }

    [Fact]
    public void InFolder_rejects_an_empty_glob()
    {
        Assert.Throws<ArgumentException>(() =>
            new Files(Graph(Self("a.cs"))).Should().DependOn().InFolder(string.Empty));
    }

    [Fact]
    public void InPath_rejects_a_null_glob()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new Files(Graph(Self("a.cs"))).Should().DependOn().InPath(null!));
    }

    [Fact]
    public void InPath_rejects_an_empty_glob()
    {
        Assert.Throws<ArgumentException>(() =>
            new Files(Graph(Self("a.cs"))).Should().DependOn().InPath(string.Empty));
    }

    [Fact]
    public void InFile_rejects_a_null_glob()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new Files(Graph(Self("a.cs"))).Should().DependOn().InFile(null!));
    }

    [Fact]
    public void InFile_rejects_an_empty_glob()
    {
        Assert.Throws<ArgumentException>(() =>
            new Files(Graph(Self("a.cs"))).Should().DependOn().InFile(string.Empty));
    }

    private static Graph Graph(params Edge[] edges) => new(edges);

    private static Edge Self(string file) => new(file, file, external: false, ImportKind.None);

    private static Edge Using(string source, string target) =>
        new(source, target, external: false, ImportKind.Using);
}
