using ArchUnitSharp.Common.Extraction;
using ArchUnitSharp.Layers.Assertion;

namespace ArchUnitSharp.Layers.Tests;

public class LayersAssertionTests
{
    [Fact]
    public void MayNotDependOn_flags_each_forbidden_dependency()
    {
        var rule = Policy(Graph(
            Self("src/Models/Car.cs"),
            Self("src/Services/CarService.cs"),
            Using("src/Services/CarService.cs", "src/Models/Car.cs")))
            .WhereLayer("Services")
            .MayNotDependOnLayers("Models");

        IReadOnlyList<Violation> violations = LayersAssertion.Check(rule, options: null);

        Assert.Equal(
            new Violation[]
            {
                new LayerViolation(
                    "Services",
                    "src/Services/CarService.cs",
                    "src/Models/Car.cs",
                    "Models"),
            },
            violations);
    }

    [Fact]
    public void MayNotDependOn_passes_when_no_dependency_reaches_a_forbidden_layer()
    {
        var rule = Policy(Graph(
            Self("src/Models/Car.cs"),
            Self("src/Services/CarService.cs"),
            Self("src/Unlayered/X.cs"),
            Using("src/Services/CarService.cs", "src/Unlayered/X.cs")))
            .WhereLayer("Services")
            .MayNotDependOnLayers("Models");

        IReadOnlyList<Violation> violations = LayersAssertion.Check(rule, options: null);

        Assert.Empty(violations);
    }

    [Fact]
    public void MayOnlyDependOn_flags_every_dependency_to_a_layer_outside_the_allowlist()
    {
        var rule = Policy(Graph(
            Self("src/Models/Car.cs"),
            Self("src/Util/Helper.cs"),
            Self("src/Services/CarService.cs"),
            Using("src/Services/CarService.cs", "src/Models/Car.cs"),
            Using("src/Services/CarService.cs", "src/Util/Helper.cs")))
            .WhereLayer("Services")
            .MayOnlyDependOnLayers("Models");

        IReadOnlyList<Violation> violations = LayersAssertion.Check(rule, options: null);

        Assert.Equal(
            new Violation[]
            {
                new LayerViolation(
                    "Services",
                    "src/Services/CarService.cs",
                    "src/Util/Helper.cs",
                    "Util"),
            },
            violations);
    }

    [Fact]
    public void MayOnlyDependOn_passes_when_every_dependency_is_to_an_allowed_layer()
    {
        var rule = Policy(Graph(
            Self("src/Models/Car.cs"),
            Self("src/Services/CarService.cs"),
            Self("src/Unlayered/X.cs"),
            Using("src/Services/CarService.cs", "src/Models/Car.cs"),
            Using("src/Services/CarService.cs", "src/Unlayered/X.cs")))
            .WhereLayer("Services")
            .MayOnlyDependOnLayers("Models");

        IReadOnlyList<Violation> violations = LayersAssertion.Check(rule, options: null);

        Assert.Empty(violations);
    }

    [Fact]
    public void Intra_layer_dependencies_are_always_allowed()
    {
        var rule = Policy(Graph(
            Self("src/Services/A.cs"),
            Self("src/Services/B.cs"),
            Self("src/Models/Car.cs"),
            Using("src/Services/A.cs", "src/Services/B.cs"),
            Using("src/Services/A.cs", "src/Models/Car.cs")))
            .WhereLayer("Services")
            .MayOnlyDependOnLayers();

        IReadOnlyList<Violation> violations = LayersAssertion.Check(rule, options: null);

        Assert.Equal(
            new Violation[]
            {
                new LayerViolation("Services", "src/Services/A.cs", "src/Models/Car.cs", "Models"),
            },
            violations);
    }

    [Fact]
    public void Dependencies_to_files_in_no_declared_layer_are_ignored()
    {
        var rule = Policy(Graph(
            Self("src/Services/A.cs"),
            Self("src/Unlayered/X.cs"),
            Using("src/Services/A.cs", "src/Unlayered/X.cs")))
            .WhereLayer("Services")
            .MayOnlyDependOnLayers();

        IReadOnlyList<Violation> violations = LayersAssertion.Check(rule, options: null);

        Assert.Empty(violations);
    }

    [Fact]
    public void MayOnlyDependOn_with_no_arguments_seals_the_layer()
    {
        var rule = Policy(Graph(
            Self("src/Models/Car.cs"),
            Self("src/Services/CarService.cs"),
            Using("src/Services/CarService.cs", "src/Models/Car.cs")))
            .WhereLayer("Services")
            .MayOnlyDependOnLayers();

        IReadOnlyList<Violation> violations = LayersAssertion.Check(rule, options: null);

        Assert.Equal(
            new Violation[]
            {
                new LayerViolation(
                    "Services",
                    "src/Services/CarService.cs",
                    "src/Models/Car.cs",
                    "Models"),
            },
            violations);
    }

    [Fact]
    public void Blocklist_rules_are_evaluated_before_allowlist_rules()
    {
        var rule = Policy(Graph(
            Self("src/Models/Car.cs"),
            Self("src/Services/CarService.cs"),
            Using("src/Services/CarService.cs", "src/Models/Car.cs")))
            .WhereLayer("Services")
            .MayNotDependOnLayers("Models")
            .MayOnlyDependOnLayers("Models");

        IReadOnlyList<Violation> violations = LayersAssertion.Check(rule, options: null);

        Assert.Equal(
            new Violation[]
            {
                new LayerViolation(
                    "Services",
                    "src/Services/CarService.cs",
                    "src/Models/Car.cs",
                    "Models"),
            },
            violations);
    }

    [Fact]
    public void Blocklist_takes_precedence_when_a_target_belongs_to_a_blocked_and_an_allowed_layer()
    {
        var rule = Policy(Graph(
            Self("src/Util/Helper.cs"),
            Self("src/Services/CarService.cs"),
            Using("src/Services/CarService.cs", "src/Util/Helper.cs")))
            .Layer("All").DefinedBy("src/**")
            .WhereLayer("Services")
            .MayNotDependOnLayers("All")
            .MayOnlyDependOnLayers("All");

        IReadOnlyList<Violation> violations = LayersAssertion.Check(rule, options: null);

        Assert.Equal(
            new Violation[]
            {
                new LayerViolation(
                    "Services",
                    "src/Services/CarService.cs",
                    "src/Util/Helper.cs",
                    "All"),
            },
            violations);
    }

    [Fact]
    public void An_allowlist_still_flags_a_dependency_the_blocklist_does_not_name()
    {
        var rule = Policy(Graph(
            Self("src/Models/Car.cs"),
            Self("src/Util/Helper.cs"),
            Self("src/Services/CarService.cs"),
            Using("src/Services/CarService.cs", "src/Util/Helper.cs")))
            .WhereLayer("Services")
            .MayNotDependOnLayers("Models")
            .MayOnlyDependOnLayers("Models");

        IReadOnlyList<Violation> violations = LayersAssertion.Check(rule, options: null);

        Assert.Equal(
            new Violation[]
            {
                new LayerViolation("Services", "src/Services/CarService.cs", "src/Util/Helper.cs", "Util"),
            },
            violations);
    }

    [Fact]
    public void Violations_are_sorted_by_source_then_target()
    {
        var rule = Policy(Graph(
            Self("src/Models/Car.cs"),
            Self("src/Util/Helper.cs"),
            Self("src/Services/Zeta.cs"),
            Self("src/Services/Alpha.cs"),
            Using("src/Services/Zeta.cs", "src/Util/Helper.cs"),
            Using("src/Services/Alpha.cs", "src/Models/Car.cs")))
            .WhereLayer("Services")
            .MayOnlyDependOnLayers();

        IReadOnlyList<Violation> violations = LayersAssertion.Check(rule, options: null);

        Assert.Equal(
            new[]
            {
                new LayerViolation("Services", "src/Services/Alpha.cs", "src/Models/Car.cs", "Models"),
                new LayerViolation("Services", "src/Services/Zeta.cs", "src/Util/Helper.cs", "Util"),
            },
            violations);
    }

    [Fact]
    public void Guards_a_subject_layer_that_matches_no_files()
    {
        var rule = Policy(Graph(Self("src/App/Program.cs")))
            .WhereLayer("Services")
            .MayOnlyDependOnLayers("Models");

        IReadOnlyList<Violation> violations = LayersAssertion.Check(rule, options: null);

        var empty = Assert.Single(violations);
        Assert.IsType<EmptyTestViolation>(empty);
        Assert.Equal(
            "layer 'Services' defined by folder 'src/Services' may only depend on layers 'Models'",
            Assert.IsType<EmptyTestViolation>(empty).RuleDescription);
    }

    [Fact]
    public void Guards_an_empty_subject_even_when_a_constraint_layer_has_files()
    {
        var rule = Policy(Graph(Self("src/Models/Car.cs")))
            .WhereLayer("Services")
            .MayNotDependOnLayers("Models");

        IReadOnlyList<Violation> violations = LayersAssertion.Check(rule, options: null);

        var empty = Assert.Single(violations);
        Assert.IsType<EmptyTestViolation>(empty);
        Assert.Equal(
            "layer 'Services' defined by folder 'src/Services' may not depend on layers 'Models'",
            Assert.IsType<EmptyTestViolation>(empty).RuleDescription);
    }

    [Fact]
    public void Guards_a_blocklist_whose_layers_all_match_no_files()
    {
        var rule = Policy(Graph(Self("src/Services/CarService.cs")))
            .WhereLayer("Services")
            .MayNotDependOnLayers("Models");

        IReadOnlyList<Violation> violations = LayersAssertion.Check(rule, options: null);

        var empty = Assert.Single(violations);
        Assert.IsType<EmptyTestViolation>(empty);
        Assert.Equal(
            "layer 'Services' defined by folder 'src/Services' may not depend on layers 'Models'",
            Assert.IsType<EmptyTestViolation>(empty).RuleDescription);
    }

    [Fact]
    public void An_allowlist_whose_named_layers_all_match_no_files_still_forbids_other_layers()
    {
        var rule = Policy(Graph(
            Self("src/Util/Helper.cs"),
            Self("src/Services/CarService.cs"),
            Using("src/Services/CarService.cs", "src/Util/Helper.cs")))
            .WhereLayer("Services")
            .MayOnlyDependOnLayers("Models");

        IReadOnlyList<Violation> violations = LayersAssertion.Check(rule, options: null);

        Assert.Equal(
            new Violation[]
            {
                new LayerViolation(
                    "Services",
                    "src/Services/CarService.cs",
                    "src/Util/Helper.cs",
                    "Util"),
            },
            violations);
    }

    [Fact]
    public void Guards_a_blocklist_with_no_names_because_it_forbids_nothing()
    {
        var rule = Policy(Graph(Self("src/Services/CarService.cs")))
            .WhereLayer("Services")
            .MayNotDependOnLayers();

        IReadOnlyList<Violation> violations = LayersAssertion.Check(rule, options: null);

        Assert.IsType<EmptyTestViolation>(Assert.Single(violations));
    }

    [Fact]
    public void Does_not_guard_an_empty_allowlist_because_it_is_the_sealed_layer_idiom()
    {
        var rule = Policy(Graph(
            Self("src/Services/CarService.cs"),
            Self("src/Unlayered/X.cs"),
            Using("src/Services/CarService.cs", "src/Unlayered/X.cs")))
            .WhereLayer("Services")
            .MayOnlyDependOnLayers();

        IReadOnlyList<Violation> violations = LayersAssertion.Check(rule, options: null);

        Assert.Empty(violations);
    }

    [Fact]
    public void Honours_allow_empty_tests()
    {
        var rule = Policy(Graph(Self("src/App/Program.cs")))
            .WhereLayer("Services")
            .MayOnlyDependOnLayers("Models");

        IReadOnlyList<Violation> violations = LayersAssertion.Check(
            rule,
            options: new CheckOptions { AllowEmptyTests = true });

        Assert.Empty(violations);
    }

    [Fact]
    public void Rejects_a_null_rule()
    {
        Assert.Throws<ArgumentNullException>(() => LayersAssertion.Check(null!, options: null));
    }

    [Fact]
    public void Rejects_a_rule_with_no_constraints()
    {
        var rule = Policy(Graph(Self("src/Services/CarService.cs"))).WhereLayer("Services");

        Assert.Throws<UserError>(() => LayersAssertion.Check(rule, options: null));
    }

    private static Graph Graph(params Edge[] edges) => new(edges);

    private static Edge Self(string file) => new(file, file, external: false, ImportKind.None);

    private static Edge Using(string source, string target) =>
        new(source, target, external: false, ImportKind.Using);

    private static Layers Policy(Graph graph) =>
        new Layers(graph)
            .Layer("Models").DefinedByFolder("src/Models")
            .Layer("Services").DefinedByFolder("src/Services")
            .Layer("Util").DefinedByFolder("src/Util");
}
