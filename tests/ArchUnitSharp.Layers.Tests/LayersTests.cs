using ArchUnitSharp.Common.Extraction;
using ArchUnitSharp.Layers.Projection;

namespace ArchUnitSharp.Layers.Tests;

public class LayersTests
{
    [Fact]
    public void A_declared_layer_selects_the_files_matching_its_path_glob()
    {
        var graph = Graph(
            Self("src/Models/Car.cs"),
            Self("src/Models/Truck.cs"),
            Self("src/App/Program.cs"));

        var layers = new Layers(graph).Layer("Models").DefinedBy("src/Models/**");

        var declared = Assert.Single(layers.DeclaredLayers);
        Assert.Equal("Models", declared.Name);
        Assert.Equal(
            new[] { "src/Models/Car.cs", "src/Models/Truck.cs" },
            LayersProjection.FilesOf(graph, declared.Filter));
    }

    [Fact]
    public void A_layer_declared_by_folder_selects_the_files_in_that_folder()
    {
        var graph = Graph(
            Self("src/Models/Car.cs"),
            Self("src/Models/Truck.cs"),
            Self("src/App/Program.cs"));

        var layers = new Layers(graph).Layer("Models").DefinedByFolder("src/Models");

        var declared = Assert.Single(layers.DeclaredLayers);
        Assert.Equal("Models", declared.Name);
        Assert.Equal(
            new[] { "src/Models/Car.cs", "src/Models/Truck.cs" },
            LayersProjection.FilesOf(graph, declared.Filter));
    }

    [Fact]
    public void DefinedBy_binds_the_filter_to_the_whole_path()
    {
        var layers = new Layers(Graph(Self("a.cs"))).Layer("Models").DefinedBy("**");

        Assert.Equal(MatchTarget.Path, Assert.Single(layers.DeclaredLayers).Filter.Target);
    }

    [Fact]
    public void DefinedByFolder_binds_the_filter_to_the_folder()
    {
        var layers = new Layers(Graph(Self("a.cs"))).Layer("Models").DefinedByFolder("**");

        Assert.Equal(MatchTarget.PathWithoutFilename, Assert.Single(layers.DeclaredLayers).Filter.Target);
    }

    [Fact]
    public void Declarations_accumulate_in_order()
    {
        var layers = new Layers(Graph(Self("a.cs")))
            .Layer("Models").DefinedByFolder("src/Models")
            .Layer("Services").DefinedByFolder("src/Services");

        Assert.Equal(new[] { "Models", "Services" }, layers.DeclaredLayers.Select(static layer => layer.Name));
    }

    [Fact]
    public void Layer_rejects_a_null_name()
    {
        Assert.Throws<ArgumentNullException>(() => new Layers(Graph(Self("a.cs"))).Layer(null!));
    }

    [Fact]
    public void Layer_rejects_an_empty_name()
    {
        Assert.Throws<ArgumentException>(() => new Layers(Graph(Self("a.cs"))).Layer(string.Empty));
    }

    [Fact]
    public void A_layer_cannot_be_declared_twice()
    {
        var layers = new Layers(Graph(Self("a.cs"))).Layer("Models").DefinedByFolder("src/Models");

        Assert.Throws<UserError>(() => layers.Layer("Models").DefinedByFolder("src/Other"));
    }

    [Fact]
    public void A_definition_leaves_the_parent_builder_unchanged()
    {
        var parent = new Layers(Graph(Self("a.cs")));

        var withModels = parent.Layer("Models").DefinedByFolder("src/Models");

        Assert.Empty(parent.DeclaredLayers);
        Assert.Equal(new[] { "Models" }, withModels.DeclaredLayers.Select(static layer => layer.Name));
    }

    [Fact]
    public void Two_branches_off_one_builder_do_not_see_each_others_layers()
    {
        var parent = new Layers(Graph(Self("a.cs")));

        var models = parent.Layer("Models").DefinedByFolder("src/Models");
        var services = parent.Layer("Services").DefinedByFolder("src/Services");

        Assert.Equal(new[] { "Models" }, models.DeclaredLayers.Select(static layer => layer.Name));
        Assert.Equal(new[] { "Services" }, services.DeclaredLayers.Select(static layer => layer.Name));
        Assert.Empty(parent.DeclaredLayers);
    }

    [Fact]
    public void WhereLayer_selects_the_declared_layer_as_the_subject()
    {
        var layers = new Layers(Graph(Self("a.cs")))
            .Layer("Models").DefinedByFolder("src/Models")
            .Layer("Services").DefinedByFolder("src/Services");

        var rule = layers.WhereLayer("Services");

        Assert.Equal("Services", rule.Subject.Name);
    }

    [Fact]
    public void WhereLayer_rejects_an_undeclared_layer()
    {
        var layers = new Layers(Graph(Self("a.cs"))).Layer("Models").DefinedByFolder("src/Models");

        Assert.Throws<UserError>(() => layers.WhereLayer("Services"));
    }

    [Fact]
    public void WhereLayer_rejects_a_null_name()
    {
        Assert.Throws<ArgumentNullException>(() => new Layers(Graph(Self("a.cs"))).WhereLayer(null!));
    }

    [Fact]
    public void WhereLayer_rejects_an_empty_name()
    {
        Assert.Throws<ArgumentException>(() => new Layers(Graph(Self("a.cs"))).WhereLayer(string.Empty));
    }

    [Fact]
    public void The_constructor_rejects_a_null_graph()
    {
        Assert.Throws<ArgumentNullException>(() => new Layers(null!));
    }

    private static Graph Graph(params Edge[] edges) => new(edges);

    private static Edge Self(string file) => new(file, file, external: false, ImportKind.None);
}
