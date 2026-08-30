using ArchUnitSharp.Common.Extraction;
using ArchUnitSharp.Layers.Assertion;

namespace ArchUnitSharp.Layers.Tests;

public class LayersAssertionTests
{
    [Fact]
    public void A_blocklist_reports_a_dependency_onto_a_named_layer()
    {
        var policy = Layers(
            Declared("App", "src/App"),
            Declared("Models", "src/Models"),
            Declared("Infra", "src/Infra"))
            .WhereLayer("App")
            .MayNotDependOnLayers("Infra");

        IReadOnlyList<Violation> violations = policy.Check();

        Assert.Equal(
            new Violation[]
            {
                new LayerViolation("App", "Infra", "src/App/Program.cs", "src/Infra/Db.cs"),
            },
            violations);
    }

    [Fact]
    public void A_blocklist_passes_when_the_subject_depends_on_no_named_layer()
    {
        var graph = Graph(
            Self("src/App/Program.cs"),
            Self("src/Models/Car.cs"),
            Self("src/Infra/Db.cs"),
            Using("src/App/Program.cs", "src/Models/Car.cs"));

        IReadOnlyList<Violation> violations = Layers(
                graph,
                Declared("App", "src/App"),
                Declared("Models", "src/Models"),
                Declared("Infra", "src/Infra"))
            .WhereLayer("App")
            .MayNotDependOnLayers("Infra")
            .Check();

        Assert.Empty(violations);
    }

    [Fact]
    public void An_allowlist_reports_a_dependency_outside_the_named_layers()
    {
        var policy = Layers(
            Declared("App", "src/App"),
            Declared("Models", "src/Models"),
            Declared("Infra", "src/Infra"))
            .WhereLayer("App")
            .MayOnlyDependOnLayers("Models");

        IReadOnlyList<Violation> violations = policy.Check();

        Assert.Equal(
            new Violation[]
            {
                new LayerViolation("App", "Infra", "src/App/Program.cs", "src/Infra/Db.cs"),
            },
            violations);
    }

    [Fact]
    public void An_allowlist_passes_when_every_dependency_is_named()
    {
        var policy = Layers(
            Declared("App", "src/App"),
            Declared("Models", "src/Models"),
            Declared("Infra", "src/Infra"))
            .WhereLayer("App")
            .MayOnlyDependOnLayers("Models", "Infra");

        IReadOnlyList<Violation> violations = policy.Check();

        Assert.Empty(violations);
    }

    [Fact]
    public void A_sealed_layer_reports_every_cross_layer_dependency()
    {
        var policy = Layers(
            Declared("App", "src/App"),
            Declared("Models", "src/Models"),
            Declared("Infra", "src/Infra"))
            .WhereLayer("App")
            .MayOnlyDependOnLayers();

        IReadOnlyList<Violation> violations = policy.Check();

        Assert.Equal(
            new Violation[]
            {
                new LayerViolation("App", "Infra", "src/App/Program.cs", "src/Infra/Db.cs"),
                new LayerViolation("App", "Models", "src/App/Program.cs", "src/Models/Car.cs"),
            },
            violations);
    }

    [Fact]
    public void A_sealed_layer_passes_when_the_subject_has_only_intra_layer_dependencies()
    {
        var graph = Graph(
            Self("src/App/Program.cs"),
            Self("src/App/Other.cs"),
            Self("src/Models/Car.cs"),
            Using("src/App/Program.cs", "src/App/Other.cs"));

        IReadOnlyList<Violation> violations = Layers(
                graph,
                Declared("App", "src/App"),
                Declared("Models", "src/Models"))
            .WhereLayer("App")
            .MayOnlyDependOnLayers()
            .Check();

        Assert.Empty(violations);
    }

    [Fact]
    public void A_constraint_whose_subject_layer_selects_no_files_is_an_empty_test()
    {
        var policy = Layers(
            Declared("App", "src/App"),
            Declared("Models", "src/Models"))
            .WhereLayer("Ghost")
            .MayNotDependOnLayers("Models");

        IReadOnlyList<Violation> violations = policy.Check();

        Assert.Equal(
            new Violation[]
            {
                new EmptyTestViolation(
                    "project layers where layer 'Ghost' may not depend on layers 'Models'"),
            },
            violations);
    }

    [Fact]
    public void A_blocklist_whose_target_layer_selects_no_files_is_an_empty_test_not_a_pass()
    {
        var policy = Layers(
            Declared("App", "src/App"),
            Declared("Models", "src/NoSuchFolder"))
            .WhereLayer("App")
            .MayNotDependOnLayers("Models");

        IReadOnlyList<Violation> violations = policy.Check();

        Assert.Equal(
            new Violation[]
            {
                new EmptyTestViolation(
                    "project layers where layer 'App' may not depend on layers 'Models'"),
            },
            violations);
    }

    [Fact]
    public void An_allowlist_whose_target_layers_all_select_no_files_is_an_empty_test()
    {
        var policy = Layers(
            Declared("App", "src/App"),
            Declared("Models", "src/NoSuchFolder"))
            .WhereLayer("App")
            .MayOnlyDependOnLayers("Models");

        IReadOnlyList<Violation> violations = policy.Check();

        Assert.Equal(
            new Violation[]
            {
                new EmptyTestViolation(
                    "project layers where layer 'App' may only depend on layers 'Models'"),
            },
            violations);
    }

    [Fact]
    public void An_undeclared_target_layer_is_an_empty_test()
    {
        var policy = Layers(
            Declared("App", "src/App"),
            Declared("Models", "src/Models"))
            .WhereLayer("App")
            .MayOnlyDependOnLayers("Services");

        IReadOnlyList<Violation> violations = policy.Check();

        Assert.Equal(
            new Violation[]
            {
                new EmptyTestViolation(
                    "project layers where layer 'App' may only depend on layers 'Services'"),
            },
            violations);
    }

    [Fact]
    public void A_named_target_layer_with_some_empty_some_populated_layers_is_not_an_empty_test()
    {
        var policy = Layers(
            Declared("App", "src/App"),
            Declared("Models", "src/Models"),
            Declared("Infra", "src/Infra"),
            Declared("Empty", "src/NoSuchFolder"))
            .WhereLayer("App")
            .MayOnlyDependOnLayers("Models", "Empty");

        IReadOnlyList<Violation> violations = policy.Check();

        Assert.Equal(
            new Violation[]
            {
                new LayerViolation("App", "Infra", "src/App/Program.cs", "src/Infra/Db.cs"),
            },
            violations);
    }

    [Fact]
    public void A_sealed_layer_with_a_non_empty_subject_is_not_an_empty_test()
    {
        var policy = Layers(
            Declared("App", "src/App"),
            Declared("Models", "src/Models"))
            .WhereLayer("App")
            .MayOnlyDependOnLayers();

        IReadOnlyList<Violation> violations = policy.Check();

        Assert.Equal(
            new Violation[]
            {
                new LayerViolation("App", "Models", "src/App/Program.cs", "src/Models/Car.cs"),
            },
            violations);
    }

    [Fact]
    public void A_sealed_layer_with_an_empty_subject_is_an_empty_test()
    {
        var policy = Layers(
            Declared("App", "src/App"),
            Declared("Models", "src/Models"))
            .WhereLayer("Ghost")
            .MayOnlyDependOnLayers();

        IReadOnlyList<Violation> violations = policy.Check();

        Assert.Equal(
            new Violation[]
            {
                new EmptyTestViolation("project layers where layer 'Ghost' may only depend on layers"),
            },
            violations);
    }

    [Fact]
    public void An_empty_test_honours_allow_empty_tests()
    {
        var policy = Layers(
            Declared("App", "src/App"),
            Declared("Models", "src/NoSuchFolder"))
            .WhereLayer("App")
            .MayNotDependOnLayers("Models");

        IReadOnlyList<Violation> violations = policy.Check(new CheckOptions { AllowEmptyTests = true });

        Assert.Empty(violations);
    }

    [Fact]
    public void A_dependency_blocked_and_not_allowed_on_the_same_subject_is_reported_once()
    {
        var policy = Layers(
            Declared("App", "src/App"),
            Declared("Models", "src/Models"),
            Declared("Infra", "src/Infra"))
            .WhereLayer("App")
            .MayNotDependOnLayers("Infra")
            .WhereLayer("App")
            .MayOnlyDependOnLayers("Models");

        IReadOnlyList<Violation> violations = policy.Check();

        Assert.Equal(
            new Violation[]
            {
                new LayerViolation("App", "Infra", "src/App/Program.cs", "src/Infra/Db.cs"),
            },
            violations);
    }

    [Fact]
    public void Blocklist_violations_are_reported_before_allowlist_violations()
    {
        var policy = Layers(
            Declared("App", "src/App"),
            Declared("Models", "src/Models"),
            Declared("Infra", "src/Infra"),
            Declared("Services", "src/Services"))
            .WhereLayer("Services")
            .MayOnlyDependOnLayers("Infra")
            .WhereLayer("App")
            .MayNotDependOnLayers("Infra");

        var graph = Graph(
            Self("src/App/Program.cs"),
            Self("src/Models/Car.cs"),
            Self("src/Infra/Db.cs"),
            Self("src/Services/Handler.cs"),
            Using("src/App/Program.cs", "src/Infra/Db.cs"),
            Using("src/Services/Handler.cs", "src/Models/Car.cs"));

        IReadOnlyList<Violation> violations = Layers(
                graph,
                Declared("App", "src/App"),
                Declared("Models", "src/Models"),
                Declared("Infra", "src/Infra"),
                Declared("Services", "src/Services"))
            .WhereLayer("Services")
            .MayOnlyDependOnLayers("Infra")
            .WhereLayer("App")
            .MayNotDependOnLayers("Infra")
            .Check();

        Assert.Equal(
            new Violation[]
            {
                new LayerViolation("App", "Infra", "src/App/Program.cs", "src/Infra/Db.cs"),
                new LayerViolation("Services", "Models", "src/Services/Handler.cs", "src/Models/Car.cs"),
            },
            violations);
    }

    [Fact]
    public void A_policy_with_no_constraints_passes()
    {
        var policy = Layers(Declared("App", "src/App"));

        Assert.Empty(policy.Check());
    }

    [Fact]
    public void CheckConstraint_rejects_a_null_layers()
    {
        var constraint = new LayerConstraint("App", new[] { "Models" }, negate: false);

        Assert.Throws<ArgumentNullException>(() => LayersAssertion.CheckConstraint(null!, constraint, null));
    }

    [Fact]
    public void CheckConstraint_rejects_a_null_constraint()
    {
        var layers = new Layers(Graph(Self("a.cs")));

        Assert.Throws<ArgumentNullException>(() => LayersAssertion.CheckConstraint(layers, null!, null));
    }

    private static Layers Layers(params LayerDeclaration[] declarations) =>
        Layers(Graph(
            Self("src/App/Program.cs"),
            Self("src/Models/Car.cs"),
            Self("src/Infra/Db.cs"),
            Self("src/Services/Handler.cs"),
            Using("src/App/Program.cs", "src/Models/Car.cs"),
            Using("src/App/Program.cs", "src/Infra/Db.cs"),
            Using("src/Services/Handler.cs", "src/Infra/Db.cs")), declarations);

    private static Layers Layers(Graph graph, params LayerDeclaration[] declarations)
    {
        Layers layers = new(graph);
        foreach (LayerDeclaration declaration in declarations)
        {
            layers = layers.AddDeclaration(declaration);
        }

        return layers;
    }

    private static LayerDeclaration Declared(string name, string folder) =>
        new(name, new Filter(new Pattern(folder), MatchTarget.PathWithoutFilename));

    private static Graph Graph(params Edge[] edges) => new(edges);

    private static Edge Self(string file) => new(file, file, external: false, ImportKind.None);

    private static Edge Using(string source, string target) =>
        new(source, target, external: false, ImportKind.Using);
}
