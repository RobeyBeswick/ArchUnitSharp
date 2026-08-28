using ArchUnitSharp.Common.Extraction;
using ArchUnitSharp.Layers.Projection;

namespace ArchUnitSharp.Layers.Tests;

public class LayersProjectionTests
{
    [Fact]
    public void FilesOf_returns_every_file_matching_the_filter_sorted()
    {
        var graph = Graph(
            Self("Z/z.cs"),
            Self("src/Models/Car.cs"),
            Self("src/Models/Truck.cs"));

        IReadOnlyList<string> files = LayersProjection.FilesOf(graph, Folder("src/Models"));

        Assert.Equal(new[] { "src/Models/Car.cs", "src/Models/Truck.cs" }, files);
    }

    [Fact]
    public void FilesOf_returns_only_distinct_sources_so_external_targets_are_not_files()
    {
        var graph = Graph(
            Self("src/App/Program.cs"),
            External("src/App/Program.cs", "System.Linq"));

        IReadOnlyList<string> files = LayersProjection.FilesOf(graph, Path("**/*"));

        Assert.Equal(new[] { "src/App/Program.cs" }, files);
    }

    [Fact]
    public void FilesOf_rejects_a_null_graph()
    {
        Assert.Throws<ArgumentNullException>(() => LayersProjection.FilesOf(null!, Folder("src")));
    }

    [Fact]
    public void FilesOf_rejects_a_null_filter()
    {
        Assert.Throws<ArgumentNullException>(() => LayersProjection.FilesOf(Graph(Self("a.cs")), null!));
    }

    [Fact]
    public void CrossLayerDependencies_returns_each_edge_from_the_subject_to_another_layer()
    {
        var graph = Graph(
            Self("src/Models/Car.cs"),
            Self("src/Services/CarService.cs"),
            Using("src/Services/CarService.cs", "src/Models/Car.cs"));

        IReadOnlyList<CrossLayerDependency> dependencies = LayersProjection.CrossLayerDependencies(
            graph, Services, DeclaredLayers);

        var dependency = Assert.Single(dependencies);
        Assert.Equal("src/Services/CarService.cs", dependency.Source);
        Assert.Equal("src/Models/Car.cs", dependency.Target);
        Assert.Equal(new[] { "Models" }, dependency.TargetLayers);
    }

    [Fact]
    public void CrossLayerDependencies_ignores_intra_layer_dependencies()
    {
        var graph = Graph(
            Self("src/Services/A.cs"),
            Self("src/Services/B.cs"),
            Using("src/Services/A.cs", "src/Services/B.cs"));

        Assert.Empty(LayersProjection.CrossLayerDependencies(graph, Services, DeclaredLayers));
    }

    [Fact]
    public void CrossLayerDependencies_ignores_targets_in_no_declared_layer()
    {
        var graph = Graph(
            Self("src/Services/A.cs"),
            Self("src/Unlayered/X.cs"),
            Using("src/Services/A.cs", "src/Unlayered/X.cs"));

        Assert.Empty(LayersProjection.CrossLayerDependencies(graph, Services, DeclaredLayers));
    }

    [Fact]
    public void CrossLayerDependencies_ignores_edges_from_files_outside_the_subject_layer()
    {
        var graph = Graph(
            Self("src/Models/Car.cs"),
            Self("src/Services/CarService.cs"),
            Self("src/App/Program.cs"),
            Using("src/App/Program.cs", "src/Models/Car.cs"));

        Assert.Empty(LayersProjection.CrossLayerDependencies(graph, Services, DeclaredLayers));
    }

    [Fact]
    public void CrossLayerDependencies_ignores_external_edges()
    {
        var graph = Graph(
            Self("src/Services/CarService.cs"),
            Self("System.Linq"),
            External("src/Services/CarService.cs", "System.Linq"));

        var layers = new[]
        {
            new Layer("System", Path("System.*")),
            new Layer("Services", Folder("src/Services")),
        };

        Assert.Empty(LayersProjection.CrossLayerDependencies(graph, Services, layers));
    }

    [Fact]
    public void CrossLayerDependencies_ignores_self_edges()
    {
        var graph = Graph(
            Self("src/Services/CarService.cs"),
            Self("src/Models/Car.cs"),
            Using("src/Services/CarService.cs", "src/Services/CarService.cs"));

        var layers = new[]
        {
            new Layer("Src", Path("src/**")),
            new Layer("Models", Folder("src/Models")),
            new Layer("Services", Folder("src/Services")),
        };

        Assert.Empty(LayersProjection.CrossLayerDependencies(graph, Services, layers));
    }

    [Fact]
    public void CrossLayerDependencies_lists_every_declared_layer_that_contains_the_target()
    {
        var graph = Graph(
            Self("src/Util/Helper.cs"),
            Self("src/Services/CarService.cs"),
            Using("src/Services/CarService.cs", "src/Util/Helper.cs"));

        var layers = new[]
        {
            new Layer("Util", Folder("src/Util")),
            new Layer("Models", Path("src/Util/**")),
            new Layer("Services", Folder("src/Services")),
        };
        var dependency = Assert.Single(LayersProjection.CrossLayerDependencies(graph, Services, layers));
        Assert.Equal(new[] { "Util", "Models" }, dependency.TargetLayers);
    }

    [Fact]
    public void CrossLayerDependencies_never_lists_the_subject_layer_as_a_target_layer()
    {
        var graph = Graph(
            Self("src/Models/Car.cs"),
            Self("src/Services/Other.cs"),
            Self("src/Services/CarService.cs"),
            Using("src/Services/CarService.cs", "src/Models/Car.cs"),
            Using("src/Services/CarService.cs", "src/Services/Other.cs"));

        IReadOnlyList<CrossLayerDependency> dependencies =
            LayersProjection.CrossLayerDependencies(graph, Services, DeclaredLayers);

        Assert.Equal("src/Models/Car.cs", Assert.Single(dependencies).Target);
    }

    [Fact]
    public void CrossLayerDependencies_result_is_sorted_by_source_then_target()
    {
        var graph = Graph(
            Self("src/Models/Car.cs"),
            Self("src/Services/Zeta.cs"),
            Self("src/Services/Alpha.cs"),
            Using("src/Services/Zeta.cs", "src/Models/Car.cs"),
            Using("src/Services/Alpha.cs", "src/Models/Car.cs"));

        IReadOnlyList<CrossLayerDependency> dependencies =
            LayersProjection.CrossLayerDependencies(graph, Services, DeclaredLayers);

        Assert.Equal(
            new[] { "src/Services/Alpha.cs", "src/Services/Zeta.cs" },
            dependencies.Select(static dependency => dependency.Source));
    }

    [Fact]
    public void CrossLayerDependencies_rejects_a_null_graph()
    {
        Assert.Throws<ArgumentNullException>(() =>
            LayersProjection.CrossLayerDependencies(null!, Services, DeclaredLayers));
    }

    [Fact]
    public void CrossLayerDependencies_rejects_a_null_subject()
    {
        Assert.Throws<ArgumentNullException>(() =>
            LayersProjection.CrossLayerDependencies(Graph(Self("a.cs")), null!, DeclaredLayers));
    }

    [Fact]
    public void CrossLayerDependencies_rejects_null_layers()
    {
        Assert.Throws<ArgumentNullException>(() =>
            LayersProjection.CrossLayerDependencies(Graph(Self("a.cs")), Services, null!));
    }

    private static Graph Graph(params Edge[] edges) => new(edges);

    private static Edge Self(string file) => new(file, file, external: false, ImportKind.None);

    private static Edge Using(string source, string target) =>
        new(source, target, external: false, ImportKind.Using);

    private static Edge External(string source, string module) =>
        new(source, module, external: true, ImportKind.Using);

    private static IReadOnlyList<Layer> DeclaredLayers => new[]
    {
        new Layer("Models", Folder("src/Models")),
        new Layer("Services", Folder("src/Services")),
    };

    private static Layer Services => new("Services", Folder("src/Services"));

    private static Filter Path(string glob) => new(new Pattern(glob), MatchTarget.Path);

    private static Filter Folder(string glob) => new(new Pattern(glob), MatchTarget.PathWithoutFilename);
}
