using ArchUnitSharp.Common.Extraction;

namespace ArchUnitSharp.Layers.Tests;

public class LayersTests
{
    [Fact]
    public void A_full_policy_passes_through_the_fluent_chain()
    {
        var policy = new Layers(Graph(
                Self("src/App/Program.cs"),
                Self("src/Models/Car.cs"),
                Using("src/App/Program.cs", "src/Models/Car.cs")))
            .Layer("App").DefinedByFolder("src/App")
            .Layer("Models").DefinedByFolder("src/Models")
            .WhereLayer("App").MayOnlyDependOnLayers("Models");

        Assert.Empty(policy.Check());
    }

    [Fact]
    public void A_full_policy_reports_a_forbidden_dependency_through_the_fluent_chain()
    {
        var policy = new Layers(Graph(
                Self("src/App/Program.cs"),
                Self("src/Models/Car.cs"),
                Self("src/Infra/Db.cs"),
                Using("src/App/Program.cs", "src/Infra/Db.cs")))
            .Layer("App").DefinedByFolder("src/App")
            .Layer("Models").DefinedByFolder("src/Models")
            .Layer("Infra").DefinedByFolder("src/Infra")
            .WhereLayer("App").MayOnlyDependOnLayers("Models");

        IReadOnlyList<Violation> violations = policy.Check();

        Assert.Equal(
            new Violation[]
            {
                new LayerViolation("App", "Infra", "src/App/Program.cs", "src/Infra/Db.cs"),
            },
            violations);
    }

    [Fact]
    public void DefinedBy_declares_a_layer_by_whole_path()
    {
        var graph = Graph(
            Self("src/App/Program.cs"),
            Self("src/Models/Car.cs"),
            Using("src/Models/Car.cs", "src/App/Program.cs"));

        IReadOnlyList<Violation> violations = new Layers(graph)
            .Layer("Car").DefinedBy("src/Models/Car.cs")
            .Layer("App").DefinedByFolder("src/App")
            .WhereLayer("Car").MayNotDependOnLayers("App")
            .Check();

        Assert.Equal(
            new Violation[]
            {
                new LayerViolation("Car", "App", "src/Models/Car.cs", "src/App/Program.cs"),
            },
            violations);
    }

    [Fact]
    public void A_rule_can_be_checked_twice_and_reports_the_same_result()
    {
        var policy = new Layers(Graph(
                Self("src/App/Program.cs"),
                Self("src/Models/Car.cs"),
                Using("src/App/Program.cs", "src/Models/Car.cs")))
            .Layer("App").DefinedByFolder("src/App")
            .Layer("Models").DefinedByFolder("src/Models")
            .WhereLayer("App").MayNotDependOnLayers("Models");

        IReadOnlyList<Violation> first = policy.Check();
        IReadOnlyList<Violation> second = policy.Check();

        Assert.Equal(first, second);
        Assert.NotSame(first, second);
    }

    [Fact]
    public void Two_branches_off_one_parent_do_not_see_each_others_declarations()
    {
        var parent = new Layers(Graph(Self("src/App/Program.cs")))
            .Layer("App").DefinedByFolder("src/App");

        var withModels = parent.Layer("Models").DefinedByFolder("src/Models");
        var withInfra = parent.Layer("Infra").DefinedByFolder("src/Infra");

        Assert.Equal(new[] { "App" }, LayerNames(parent));
        Assert.Equal(new[] { "App", "Models" }, LayerNames(withModels));
        Assert.Equal(new[] { "App", "Infra" }, LayerNames(withInfra));
    }

    [Fact]
    public void Two_branches_off_one_parent_do_not_see_each_others_constraints()
    {
        var parent = new Layers(Graph(Self("src/App/Program.cs")))
            .Layer("App").DefinedByFolder("src/App")
            .Layer("Models").DefinedByFolder("src/Models");

        var blocked = parent.WhereLayer("App").MayNotDependOnLayers("Models");
        var allowed = parent.WhereLayer("App").MayOnlyDependOnLayers("Models");

        Assert.Single(blocked.Constraints);
        Assert.Single(allowed.Constraints);
        Assert.Empty(parent.Constraints);
    }

    [Fact]
    public void Declarations_return_a_fresh_copy_on_every_read()
    {
        var layers = new Layers(Graph(Self("src/App/Program.cs")))
            .Layer("App").DefinedByFolder("src/App")
            .Layer("Models").DefinedByFolder("src/Models");

        IReadOnlyList<LayerDeclaration> declarations = layers.Declarations;
        var backing = (LayerDeclaration[])declarations;
        backing[0] = new LayerDeclaration("Hacked", new Filter(new Pattern("*"), MatchTarget.Path));

        Assert.Equal("App", layers.Declarations[0].Name);
    }

    [Fact]
    public void Constraints_return_a_fresh_copy_on_every_read()
    {
        var layers = new Layers(Graph(Self("src/App/Program.cs")))
            .WhereLayer("App").MayNotDependOnLayers("Models");

        IReadOnlyList<LayerConstraint> constraints = layers.Constraints;
        ((LayerConstraint[])constraints)[0] = null!;

        Assert.NotNull(layers.Constraints[0]);
    }

    [Fact]
    public void TargetLayers_return_a_fresh_copy_on_every_read()
    {
        var constraint = new LayerConstraint("App", new[] { "Models" }, negate: true);

        IReadOnlyList<string> names = constraint.TargetLayers;
        ((string[])names)[0] = "Hacked";

        Assert.Equal(new[] { "Models" }, constraint.TargetLayers);
    }

    [Fact]
    public void A_constraint_copies_the_callers_target_layer_array()
    {
        string[] targets = { "Models", "Services" };
        var constraint = new LayerConstraint("App", targets, negate: false);
        targets[0] = "Hacked";

        Assert.Equal(new[] { "Models", "Services" }, constraint.TargetLayers);
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
    public void DefinedBy_rejects_a_null_glob()
    {
        Assert.Throws<ArgumentNullException>(() => new Layers(Graph(Self("a.cs"))).Layer("A").DefinedBy(null!));
    }

    [Fact]
    public void DefinedBy_rejects_an_empty_glob()
    {
        Assert.Throws<ArgumentException>(() => new Layers(Graph(Self("a.cs"))).Layer("A").DefinedBy(string.Empty));
    }

    [Fact]
    public void DefinedByFolder_rejects_a_null_glob()
    {
        Assert.Throws<ArgumentNullException>(() => new Layers(Graph(Self("a.cs"))).Layer("A").DefinedByFolder(null!));
    }

    [Fact]
    public void DefinedByFolder_rejects_an_empty_glob()
    {
        Assert.Throws<ArgumentException>(() => new Layers(Graph(Self("a.cs"))).Layer("A").DefinedByFolder(string.Empty));
    }

    [Fact]
    public void MayOnlyDependOnLayers_rejects_a_null_argument()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new Layers(Graph(Self("a.cs"))).WhereLayer("App").MayOnlyDependOnLayers(null!));
    }

    [Fact]
    public void MayOnlyDependOnLayers_rejects_an_empty_name()
    {
        Assert.Throws<ArgumentException>(() =>
            new Layers(Graph(Self("a.cs"))).WhereLayer("App").MayOnlyDependOnLayers(string.Empty));
    }

    [Fact]
    public void MayNotDependOnLayers_rejects_a_null_name()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new Layers(Graph(Self("a.cs"))).WhereLayer("App").MayNotDependOnLayers(null!, "Models"));
    }

    [Fact]
    public void MayNotDependOnLayers_rejects_an_empty_name()
    {
        Assert.Throws<ArgumentException>(() =>
            new Layers(Graph(Self("a.cs"))).WhereLayer("App").MayNotDependOnLayers("Models", string.Empty));
    }

    [Fact]
    public void MayOnlyDependOnLayers_with_no_arguments_is_a_sealed_layer()
    {
        var policy = new Layers(Graph(
                Self("src/App/Program.cs"),
                Self("src/Models/Car.cs"),
                Using("src/App/Program.cs", "src/Models/Car.cs")))
            .Layer("App").DefinedByFolder("src/App")
            .Layer("Models").DefinedByFolder("src/Models")
            .WhereLayer("App").MayOnlyDependOnLayers();

        Assert.Equal(
            new Violation[]
            {
                new LayerViolation("App", "Models", "src/App/Program.cs", "src/Models/Car.cs"),
            },
            policy.Check());
    }

    private static string[] LayerNames(Layers layers) =>
        layers.Declarations.Select(static declaration => declaration.Name).ToArray();

    private static Graph Graph(params Edge[] edges) => new(edges);

    private static Edge Self(string file) => new(file, file, external: false, ImportKind.None);

    private static Edge Using(string source, string target) =>
        new(source, target, external: false, ImportKind.Using);
}
