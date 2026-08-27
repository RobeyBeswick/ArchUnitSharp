using ArchUnitSharp.Common.Extraction;

namespace ArchUnitSharp.Files.Tests;

public class FilesTests
{
    [Fact]
    public void Select_without_selectors_returns_every_file()
    {
        var files = new Files(Graph(
            Self("src/App/Program.cs"),
            Self("src/Models/Car.cs")));

        Assert.Equal(new[] { "src/App/Program.cs", "src/Models/Car.cs" }, files.Select());
    }

    [Fact]
    public void WithName_selects_by_the_file_name_not_the_path()
    {
        var files = new Files(Graph(
            Self("src/Models/Car.cs"),
            Self("src/App/Car.cs"),
            Self("src/App/Program.cs"))).WithName("Car.cs");

        Assert.Equal(new[] { "src/App/Car.cs", "src/Models/Car.cs" }, files.Select());
    }

    [Fact]
    public void InFolder_selects_by_the_folder_not_the_file_name()
    {
        var files = new Files(Graph(
            Self("src/Models/Car.cs"),
            Self("src/Models/Truck.cs"),
            Self("src/App/Car.cs"))).InFolder("src/Models");

        Assert.Equal(new[] { "src/Models/Car.cs", "src/Models/Truck.cs" }, files.Select());
    }

    [Fact]
    public void InPath_selects_by_the_whole_path()
    {
        var files = new Files(Graph(
            Self("src/Models/Car.cs"),
            Self("src/Models/Truck.cs"))).InPath("src/Models/Car.cs");

        Assert.Equal(new[] { "src/Models/Car.cs" }, files.Select());
    }

    [Fact]
    public void InFile_selects_by_the_class_style_name()
    {
        var files = new Files(Graph(
            Self("src/Models/Car.cs"),
            Self("src/App/Program.cs"))).InFile("src.Models.Car");

        Assert.Equal(new[] { "src/Models/Car.cs" }, files.Select());
    }

    [Fact]
    public void Selectors_combine_with_and()
    {
        var files = new Files(Graph(
            Self("src/Models/Car.cs"),
            Self("src/Models/Truck.cs"),
            Self("src/App/Car.cs")))
            .WithName("Car.cs")
            .InFolder("src/Models");

        Assert.Equal(new[] { "src/Models/Car.cs" }, files.Select());
    }

    [Fact]
    public void A_selector_leaves_the_parent_selection_unchanged()
    {
        var graph = Graph(
            Self("src/Models/Car.cs"),
            Self("src/App/Program.cs"));
        var parent = new Files(graph);

        var named = parent.WithName("Car.cs");

        Assert.Equal(new[] { "src/App/Program.cs", "src/Models/Car.cs" }, parent.Select());
        Assert.Equal(new[] { "src/Models/Car.cs" }, named.Select());
    }

    [Fact]
    public void Two_branches_off_one_parent_do_not_see_each_others_selectors()
    {
        var graph = Graph(
            Self("src/Models/Car.cs"),
            Self("src/Models/Truck.cs"),
            Self("src/App/Program.cs"));
        var parent = new Files(graph);

        var cars = parent.WithName("Car.cs");
        var inApp = parent.InFolder("src/App");

        Assert.Equal(new[] { "src/Models/Car.cs" }, cars.Select());
        Assert.Equal(new[] { "src/App/Program.cs" }, inApp.Select());
        Assert.Equal(
            new[] { "src/App/Program.cs", "src/Models/Car.cs", "src/Models/Truck.cs" },
            parent.Select());
    }

    [Fact]
    public void A_branch_extends_its_own_chain_without_touching_the_other_branch()
    {
        var graph = Graph(
            Self("src/Models/Car.cs"),
            Self("src/Models/Truck.cs"),
            Self("src/App/Car.cs"));
        var parent = new Files(graph);

        var carsInApp = parent.WithName("Car.cs").InFolder("src/App");
        var carsInModels = parent.WithName("Car.cs").InFolder("src/Models");

        Assert.Equal(new[] { "src/App/Car.cs" }, carsInApp.Select());
        Assert.Equal(new[] { "src/Models/Car.cs" }, carsInModels.Select());
    }

    [Fact]
    public void Select_returns_a_fresh_list_each_call()
    {
        var files = new Files(Graph(Self("a.cs"), Self("b.cs")));

        IReadOnlyList<string> first = files.Select();
        IReadOnlyList<string> second = files.Select();

        Assert.NotSame(first, second);
        Assert.Equal(second, first);
    }

    [Fact]
    public void The_constructor_rejects_a_null_graph()
    {
        Assert.Throws<ArgumentNullException>(() => new Files(null!));
    }

    [Fact]
    public void WithName_rejects_a_null_glob()
    {
        Assert.Throws<ArgumentNullException>(() => new Files(Graph(Self("a.cs"))).WithName(null!));
    }

    [Fact]
    public void WithName_rejects_an_empty_glob()
    {
        Assert.Throws<ArgumentException>(() => new Files(Graph(Self("a.cs"))).WithName(string.Empty));
    }

    [Fact]
    public void InFolder_rejects_a_null_glob()
    {
        Assert.Throws<ArgumentNullException>(() => new Files(Graph(Self("a.cs"))).InFolder(null!));
    }

    [Fact]
    public void InPath_rejects_a_null_glob()
    {
        Assert.Throws<ArgumentNullException>(() => new Files(Graph(Self("a.cs"))).InPath(null!));
    }

    [Fact]
    public void InFile_rejects_a_null_glob()
    {
        Assert.Throws<ArgumentNullException>(() => new Files(Graph(Self("a.cs"))).InFile(null!));
    }

    private static Graph Graph(params Edge[] edges) => new(edges);

    private static Edge Self(string file) => new(file, file, external: false, ImportKind.None);
}
