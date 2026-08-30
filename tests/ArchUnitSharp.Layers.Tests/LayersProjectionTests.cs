using ArchUnitSharp.Common.Extraction;
using ArchUnitSharp.Layers.Projection;

namespace ArchUnitSharp.Layers.Tests;

public class LayersProjectionTests
{
    [Fact]
    public void FilesOfLayer_returns_every_file_matching_a_folder_declaration()
    {
        var graph = Graph(
            Self("src/Models/Car.cs"),
            Self("src/Models/Truck.cs"),
            Self("src/App/Program.cs"));

        IReadOnlyList<string> files = LayersProjection.FilesOfLayer(
            graph,
            new[] { Declared("Models", "src/Models", MatchTarget.PathWithoutFilename) },
            "Models");

        Assert.Equal(new[] { "src/Models/Car.cs", "src/Models/Truck.cs" }, files);
    }

    [Fact]
    public void FilesOfLayer_returns_every_file_matching_a_path_declaration()
    {
        var graph = Graph(
            Self("src/Models/Car.cs"),
            Self("src/App/Program.cs"));

        IReadOnlyList<string> files = LayersProjection.FilesOfLayer(
            graph,
            new[] { Declared("Car", "src/Models/Car.cs", MatchTarget.Path) },
            "Car");

        Assert.Equal(new[] { "src/Models/Car.cs" }, files);
    }

    [Fact]
    public void FilesOfLayer_unions_declarations_of_the_same_layer()
    {
        var graph = Graph(
            Self("src/Models/Car.cs"),
            Self("src/Shared/Truck.cs"),
            Self("src/App/Program.cs"));

        IReadOnlyList<string> files = LayersProjection.FilesOfLayer(
            graph,
            new[]
            {
                Declared("Models", "src/Models", MatchTarget.PathWithoutFilename),
                Declared("Models", "src/Shared", MatchTarget.PathWithoutFilename),
            },
            "Models");

        Assert.Equal(new[] { "src/Models/Car.cs", "src/Shared/Truck.cs" }, files);
    }

    [Fact]
    public void FilesOfLayer_returns_nothing_for_a_layer_with_no_matching_files()
    {
        var graph = Graph(Self("src/App/Program.cs"));

        IReadOnlyList<string> files = LayersProjection.FilesOfLayer(
            graph,
            new[] { Declared("Models", "src/Models", MatchTarget.PathWithoutFilename) },
            "Models");

        Assert.Empty(files);
    }

    [Fact]
    public void FilesOfLayer_returns_nothing_for_an_undeclared_layer()
    {
        var graph = Graph(Self("src/App/Program.cs"));

        IReadOnlyList<string> files = LayersProjection.FilesOfLayer(
            graph,
            Array.Empty<LayerDeclaration>(),
            "Models");

        Assert.Empty(files);
    }

    [Fact]
    public void FilesOfLayer_result_is_sorted_ordinally()
    {
        var graph = Graph(Self("Z/z.cs"), Self("A/a.cs"), Self("M/m.cs"));

        IReadOnlyList<string> files = LayersProjection.FilesOfLayer(
            graph,
            new[] { Declared("All", "*", MatchTarget.Filename) },
            "All");

        Assert.Equal(new[] { "A/a.cs", "M/m.cs", "Z/z.cs" }, files);
    }

    [Fact]
    public void FilesOfLayer_rejects_a_null_graph()
    {
        Assert.Throws<ArgumentNullException>(() =>
            LayersProjection.FilesOfLayer(null!, Array.Empty<LayerDeclaration>(), "Models"));
    }

    [Fact]
    public void FilesOfLayer_rejects_null_declarations()
    {
        Assert.Throws<ArgumentNullException>(() =>
            LayersProjection.FilesOfLayer(Graph(Self("a.cs")), null!, "Models"));
    }

    [Fact]
    public void FilesOfLayer_rejects_a_null_layer_name()
    {
        Assert.Throws<ArgumentNullException>(() =>
            LayersProjection.FilesOfLayer(Graph(Self("a.cs")), Array.Empty<LayerDeclaration>(), null!));
    }

    [Fact]
    public void FilesOfLayer_rejects_an_empty_layer_name()
    {
        Assert.Throws<ArgumentException>(() =>
            LayersProjection.FilesOfLayer(Graph(Self("a.cs")), Array.Empty<LayerDeclaration>(), string.Empty));
    }

    [Fact]
    public void CrossLayerDependencies_reports_a_dependency_between_two_layers()
    {
        var graph = Graph(
            Self("src/App/Program.cs"),
            Self("src/Models/Car.cs"),
            Using("src/App/Program.cs", "src/Models/Car.cs"));

        IReadOnlyList<CrossLayerDependency> dependencies = LayersProjection.CrossLayerDependencies(
            graph,
            new[]
            {
                Declared("App", "src/App", MatchTarget.PathWithoutFilename),
                Declared("Models", "src/Models", MatchTarget.PathWithoutFilename),
            });

        Assert.Equal(
            new[]
            {
                new CrossLayerDependency("App", "Models", "src/App/Program.cs", "src/Models/Car.cs"),
            },
            dependencies);
    }

    [Fact]
    public void CrossLayerDependencies_ignores_intra_layer_edges()
    {
        var graph = Graph(
            Self("src/App/Program.cs"),
            Self("src/App/Other.cs"),
            Using("src/App/Program.cs", "src/App/Other.cs"));

        IReadOnlyList<CrossLayerDependency> dependencies = LayersProjection.CrossLayerDependencies(
            graph,
            new[] { Declared("App", "src/App", MatchTarget.PathWithoutFilename) });

        Assert.Empty(dependencies);
    }

    [Fact]
    public void CrossLayerDependencies_ignores_edges_whose_endpoint_belongs_to_no_layer()
    {
        var graph = Graph(
            Self("src/App/Program.cs"),
            Self("src/Unlayered/Thing.cs"),
            Using("src/App/Program.cs", "src/Unlayered/Thing.cs"));

        IReadOnlyList<CrossLayerDependency> dependencies = LayersProjection.CrossLayerDependencies(
            graph,
            new[] { Declared("App", "src/App", MatchTarget.PathWithoutFilename) });

        Assert.Empty(dependencies);
    }

    [Fact]
    public void CrossLayerDependencies_ignores_self_edges()
    {
        var graph = Graph(Self("src/App/Program.cs"));

        IReadOnlyList<CrossLayerDependency> dependencies = LayersProjection.CrossLayerDependencies(
            graph,
            new[]
            {
                Declared("App", "src/App", MatchTarget.PathWithoutFilename),
                Declared("Frontend", "src/App", MatchTarget.PathWithoutFilename),
            });

        Assert.Empty(dependencies);
    }

    [Fact]
    public void CrossLayerDependencies_ignores_external_edges()
    {
        var graph = Graph(
            Self("src/App/Program.cs"),
            Self("src/Models/Car.cs"),
            External("src/App/Program.cs", "src/Models/Car.cs"));

        IReadOnlyList<CrossLayerDependency> dependencies = LayersProjection.CrossLayerDependencies(
            graph,
            new[]
            {
                Declared("App", "src/App", MatchTarget.PathWithoutFilename),
                Declared("Models", "src/Models", MatchTarget.PathWithoutFilename),
            });

        Assert.Empty(dependencies);
    }

    [Fact]
    public void CrossLayerDependencies_reports_one_dependency_per_source_layer()
    {
        var graph = Graph(
            Self("src/App/Program.cs"),
            Self("src/Shared/Car.cs"),
            Using("src/App/Program.cs", "src/Shared/Car.cs"));

        IReadOnlyList<CrossLayerDependency> dependencies = LayersProjection.CrossLayerDependencies(
            graph,
            new[]
            {
                Declared("App", "src/App", MatchTarget.PathWithoutFilename),
                Declared("Frontend", "src/App", MatchTarget.PathWithoutFilename),
                Declared("Shared", "src/Shared", MatchTarget.PathWithoutFilename),
            });

        Assert.Equal(
            new[]
            {
                new CrossLayerDependency("App", "Shared", "src/App/Program.cs", "src/Shared/Car.cs"),
                new CrossLayerDependency("Frontend", "Shared", "src/App/Program.cs", "src/Shared/Car.cs"),
            },
            dependencies);
    }

    [Fact]
    public void CrossLayerDependencies_reports_each_dependency_edge_separately()
    {
        var graph = Graph(
            Self("src/App/Program.cs"),
            Self("src/Models/Car.cs"),
            Self("src/Models/Truck.cs"),
            Using("src/App/Program.cs", "src/Models/Car.cs"),
            Using("src/App/Program.cs", "src/Models/Truck.cs"));

        IReadOnlyList<CrossLayerDependency> dependencies = LayersProjection.CrossLayerDependencies(
            graph,
            new[]
            {
                Declared("App", "src/App", MatchTarget.PathWithoutFilename),
                Declared("Models", "src/Models", MatchTarget.PathWithoutFilename),
            });

        Assert.Equal(
            new[]
            {
                new CrossLayerDependency("App", "Models", "src/App/Program.cs", "src/Models/Car.cs"),
                new CrossLayerDependency("App", "Models", "src/App/Program.cs", "src/Models/Truck.cs"),
            },
            dependencies);
    }

    [Fact]
    public void CrossLayerDependencies_result_is_sorted()
    {
        var graph = Graph(
            Self("B/b.cs"),
            Self("A/a.cs"),
            Self("D/d.cs"),
            Self("C/c.cs"),
            Using("A/a.cs", "C/c.cs"),
            Using("B/b.cs", "D/d.cs"),
            Using("A/a.cs", "D/d.cs"));

        IReadOnlyList<CrossLayerDependency> dependencies = LayersProjection.CrossLayerDependencies(
            graph,
            new[]
            {
                Declared("X", "A", MatchTarget.PathWithoutFilename),
                Declared("Y", "B", MatchTarget.PathWithoutFilename),
                Declared("Z", "C", MatchTarget.PathWithoutFilename),
                Declared("W", "D", MatchTarget.PathWithoutFilename),
            });

        Assert.Equal(
            new[]
            {
                new CrossLayerDependency("X", "W", "A/a.cs", "D/d.cs"),
                new CrossLayerDependency("X", "Z", "A/a.cs", "C/c.cs"),
                new CrossLayerDependency("Y", "W", "B/b.cs", "D/d.cs"),
            },
            dependencies);
    }

    [Fact]
    public void CrossLayerDependencies_rejects_a_null_graph()
    {
        Assert.Throws<ArgumentNullException>(() =>
            LayersProjection.CrossLayerDependencies(null!, Array.Empty<LayerDeclaration>()));
    }

    [Fact]
    public void CrossLayerDependencies_rejects_null_declarations()
    {
        Assert.Throws<ArgumentNullException>(() =>
            LayersProjection.CrossLayerDependencies(Graph(Self("a.cs")), null!));
    }

    private static Graph Graph(params Edge[] edges) => new(edges);

    private static Edge Self(string file) => new(file, file, external: false, ImportKind.None);

    private static Edge Using(string source, string target) =>
        new(source, target, external: false, ImportKind.Using);

    private static Edge External(string source, string module) =>
        new(source, module, external: true, ImportKind.Using);

    private static LayerDeclaration Declared(string name, string glob, MatchTarget target) =>
        new(name, new Filter(new Pattern(glob), target));
}
