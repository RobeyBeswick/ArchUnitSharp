using ArchUnitSharp.Common.Extraction;

namespace ArchUnitSharp.Metrics.Tests;

public class MetricsTests
{
    [Fact]
    public void SelectFiles_without_selectors_returns_every_file()
    {
        var metrics = new Metrics(Graph(
            Self("src/App/Program.cs"),
            Self("src/Models/Car.cs")));

        Assert.Equal(new[] { "src/App/Program.cs", "src/Models/Car.cs" }, metrics.SelectFiles());
    }

    [Fact]
    public void WithName_selects_by_the_file_name_not_the_path()
    {
        var metrics = new Metrics(Graph(
            Self("src/Models/Car.cs"),
            Self("src/App/Car.cs"),
            Self("src/App/Program.cs"))).WithName("Car.cs");

        Assert.Equal(new[] { "src/App/Car.cs", "src/Models/Car.cs" }, metrics.SelectFiles());
    }

    [Fact]
    public void InFolder_selects_by_the_folder_not_the_file_name()
    {
        var metrics = new Metrics(Graph(
            Self("src/Models/Car.cs"),
            Self("src/Models/Truck.cs"),
            Self("src/App/Car.cs"))).InFolder("src/Models");

        Assert.Equal(new[] { "src/Models/Car.cs", "src/Models/Truck.cs" }, metrics.SelectFiles());
    }

    [Fact]
    public void InPath_selects_by_the_whole_path()
    {
        var metrics = new Metrics(Graph(
            Self("src/Models/Car.cs"),
            Self("src/Models/Truck.cs"))).InPath("src/Models/Car.cs");

        Assert.Equal(new[] { "src/Models/Car.cs" }, metrics.SelectFiles());
    }

    [Fact]
    public void Selectors_combine_with_and()
    {
        var metrics = new Metrics(Graph(
            Self("src/Models/Car.cs"),
            Self("src/Models/Truck.cs"),
            Self("src/App/Car.cs")))
            .WithName("Car.cs")
            .InFolder("src/Models");

        Assert.Equal(new[] { "src/Models/Car.cs" }, metrics.SelectFiles());
    }

    [Fact]
    public void A_selector_leaves_the_parent_scope_unchanged()
    {
        var graph = Graph(
            Self("src/Models/Car.cs"),
            Self("src/App/Program.cs"));
        var parent = new Metrics(graph);

        var named = parent.WithName("Car.cs");

        Assert.Equal(new[] { "src/App/Program.cs", "src/Models/Car.cs" }, parent.SelectFiles());
        Assert.Equal(new[] { "src/Models/Car.cs" }, named.SelectFiles());
    }

    [Fact]
    public void Two_branches_off_one_parent_do_not_see_each_others_selectors()
    {
        var graph = Graph(
            Self("src/Models/Car.cs"),
            Self("src/Models/Truck.cs"),
            Self("src/App/Program.cs"));
        var parent = new Metrics(graph);

        var cars = parent.WithName("Car.cs");
        var inApp = parent.InFolder("src/App");

        Assert.Equal(new[] { "src/Models/Car.cs" }, cars.SelectFiles());
        Assert.Equal(new[] { "src/App/Program.cs" }, inApp.SelectFiles());
        Assert.Equal(
            new[] { "src/App/Program.cs", "src/Models/Car.cs", "src/Models/Truck.cs" },
            parent.SelectFiles());
    }

    [Fact]
    public void A_branch_extends_its_own_chain_without_touching_the_other_branch()
    {
        var graph = Graph(
            Self("src/Models/Car.cs"),
            Self("src/Models/Truck.cs"),
            Self("src/App/Car.cs"));
        var parent = new Metrics(graph);

        var carsInApp = parent.WithName("Car.cs").InFolder("src/App");
        var carsInModels = parent.WithName("Car.cs").InFolder("src/Models");

        Assert.Equal(new[] { "src/App/Car.cs" }, carsInApp.SelectFiles());
        Assert.Equal(new[] { "src/Models/Car.cs" }, carsInModels.SelectFiles());
    }

    [Fact]
    public void A_class_selector_does_not_change_the_file_selection()
    {
        var metrics = new Metrics(Graph(
            Self("src/App/Program.cs"),
            Self("src/Models/Car.cs"))).ForClassesMatching("*.Controller");

        Assert.Equal(new[] { "src/App/Program.cs", "src/Models/Car.cs" }, metrics.SelectFiles());
    }

    [Fact]
    public void SelectFiles_returns_a_fresh_list_each_call()
    {
        var metrics = new Metrics(Graph(Self("a.cs"), Self("b.cs")));

        IReadOnlyList<string> first = metrics.SelectFiles();
        IReadOnlyList<string> second = metrics.SelectFiles();

        Assert.NotSame(first, second);
        Assert.Equal(second, first);
    }

    [Fact]
    public void The_constructor_rejects_a_null_graph()
    {
        Assert.Throws<ArgumentNullException>(() => new Metrics(null!));
    }

    [Fact]
    public void WithName_rejects_a_null_glob()
    {
        Assert.Throws<ArgumentNullException>(() => new Metrics(Graph(Self("a.cs"))).WithName(null!));
    }

    [Fact]
    public void WithName_rejects_an_empty_glob()
    {
        Assert.Throws<ArgumentException>(() => new Metrics(Graph(Self("a.cs"))).WithName(string.Empty));
    }

    [Fact]
    public void InFolder_rejects_a_null_glob()
    {
        Assert.Throws<ArgumentNullException>(() => new Metrics(Graph(Self("a.cs"))).InFolder(null!));
    }

    [Fact]
    public void InPath_rejects_a_null_glob()
    {
        Assert.Throws<ArgumentNullException>(() => new Metrics(Graph(Self("a.cs"))).InPath(null!));
    }

    [Fact]
    public void ForClassesMatching_rejects_a_null_glob()
    {
        Assert.Throws<ArgumentNullException>(() => new Metrics(Graph(Self("a.cs"))).ForClassesMatching(null!));
    }

    [Fact]
    public void ForClassesMatching_rejects_an_empty_glob()
    {
        Assert.Throws<ArgumentException>(() => new Metrics(Graph(Self("a.cs"))).ForClassesMatching(string.Empty));
    }

    [Fact]
    public void Count_returns_a_count_section_over_this_scope()
    {
        var metrics = new Metrics(Graph(Self("a.cs"))).InFolder("src");

        CountMetrics count = metrics.Count();

        Assert.Same(metrics, count.Metrics);
    }

    [Fact]
    public void Lcom_returns_a_lcom_section_over_this_scope()
    {
        var metrics = new Metrics(Graph(Self("a.cs"))).InFolder("src");

        LcomMetrics lcom = metrics.Lcom();

        Assert.Same(metrics, lcom.Metrics);
    }

    [Fact]
    public void A_selector_leaves_the_class_selectors_unchanged()
    {
        var parent = new Metrics(Graph(Self("a.cs"))).ForClassesMatching("*.Controller");

        var named = parent.WithName("Car.cs");

        Assert.Equal(
            "project metrics for classes matching '*.Controller'",
            parent.DescribeScope());
        Assert.Equal(
            "project metrics with name 'Car.cs' for classes matching '*.Controller'",
            named.DescribeScope());
    }

    [Fact]
    public void Two_class_selector_branches_do_not_see_each_other()
    {
        var parent = new Metrics(Graph(Self("a.cs")));

        var controllers = parent.ForClassesMatching("*.Controller");
        var models = parent.ForClassesMatching("*.Model");

        Assert.Equal(
            "project metrics for classes matching '*.Controller'",
            controllers.DescribeScope());
        Assert.Equal(
            "project metrics for classes matching '*.Model'",
            models.DescribeScope());
        Assert.Equal("project metrics", parent.DescribeScope());
    }

    [Fact]
    public void DescribeScope_names_every_selector_in_its_own_words()
    {
        var metrics = new Metrics(Graph(Self("a.cs")))
            .WithName("Car.cs")
            .InFolder("src/Models")
            .InPath("src/Models/Car.cs")
            .ForClassesMatching("*.Controller");

        Assert.Equal(
            "project metrics with name 'Car.cs' in folder 'src/Models' in path 'src/Models/Car.cs' for classes matching '*.Controller'",
            metrics.DescribeScope());
    }

    [Fact]
    public void The_internal_constructor_rejects_a_null_provider()
    {
        Assert.Throws<ArgumentNullException>(() => new Metrics(Graph(Self("a.cs")), null!));
    }

    [Fact]
    public void A_branch_keeps_the_source_provider_of_its_parent()
    {
        var parent = new Metrics(Graph(Self("src/Models/Car.cs"), Self("src/App/Program.cs")), _ => "source");

        var named = parent.WithName("Car.cs");

        Assert.Equal("source", named.SourceText("src/Models/Car.cs"));
        Assert.Equal("source", parent.SourceText("src/Models/Car.cs"));
    }

    private static Graph Graph(params Edge[] edges) => new(edges);

    private static Edge Self(string file) => new(file, file, external: false, ImportKind.None);
}
