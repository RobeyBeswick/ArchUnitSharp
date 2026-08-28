using ArchUnitSharp.Common.Extraction;

namespace ArchUnitSharp.Layers.Tests;

public class LayerRuleTests
{
    [Fact]
    public void MayOnlyDependOnLayers_passes_when_every_dependency_is_to_an_allowed_layer()
    {
        var rule = Policy(Graph(
            Self("src/Models/Car.cs"),
            Self("src/Services/CarService.cs"),
            Using("src/Services/CarService.cs", "src/Models/Car.cs")))
            .WhereLayer("Services")
            .MayOnlyDependOnLayers("Models");

        Assert.Empty(rule.Check());
    }

    [Fact]
    public void MayOnlyDependOnLayers_flags_every_dependency_to_a_layer_outside_the_allowlist()
    {
        var rule = Policy(Graph(
            Self("src/Models/Car.cs"),
            Self("src/Util/Helper.cs"),
            Self("src/Services/CarService.cs"),
            Using("src/Services/CarService.cs", "src/Util/Helper.cs")))
            .WhereLayer("Services")
            .MayOnlyDependOnLayers("Models");

        IReadOnlyList<Violation> violations = rule.Check();

        Assert.Equal(
            new Violation[]
            {
                new LayerViolation("Services", "src/Services/CarService.cs", "src/Util/Helper.cs", "Util"),
            },
            violations);
    }

    [Fact]
    public void MayNotDependOnLayers_flags_each_forbidden_dependency()
    {
        var rule = Policy(Graph(
            Self("src/Models/Car.cs"),
            Self("src/Services/CarService.cs"),
            Using("src/Services/CarService.cs", "src/Models/Car.cs")))
            .WhereLayer("Services")
            .MayNotDependOnLayers("Models");

        IReadOnlyList<Violation> violations = rule.Check();

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
    public void MayNotDependOnLayers_passes_when_no_dependency_reaches_a_forbidden_layer()
    {
        var rule = Policy(Graph(
            Self("src/Models/Car.cs"),
            Self("src/Util/Helper.cs"),
            Self("src/Services/CarService.cs"),
            Using("src/Services/CarService.cs", "src/Models/Car.cs")))
            .WhereLayer("Services")
            .MayNotDependOnLayers("Util");

        Assert.Empty(rule.Check());
    }

    [Fact]
    public void A_blocklist_and_an_allowlist_combine_with_the_blocklist_first()
    {
        var rule = Policy(Graph(
            Self("src/Models/Car.cs"),
            Self("src/Services/CarService.cs"),
            Using("src/Services/CarService.cs", "src/Models/Car.cs")))
            .WhereLayer("Services")
            .MayNotDependOnLayers("Models")
            .MayOnlyDependOnLayers("Models");

        Assert.Equal(
            new Violation[]
            {
                new LayerViolation("Services", "src/Services/CarService.cs", "src/Models/Car.cs", "Models"),
            },
            rule.Check());
    }

    [Fact]
    public void A_constraint_leaves_the_parent_rule_unchanged()
    {
        var subject = Policy(Graph(Self("src/Services/A.cs"))).WhereLayer("Services");

        var only = subject.MayOnlyDependOnLayers("Models");
        var forbid = subject.MayNotDependOnLayers("Models");

        Assert.Empty(subject.Constraints);
        Assert.Single(only.Constraints);
        Assert.Single(forbid.Constraints);
    }

    [Fact]
    public void Two_rules_off_one_subject_do_not_see_each_others_constraints()
    {
        var subject = Policy(Graph(
            Self("src/Models/Car.cs"),
            Self("src/Services/CarService.cs"),
            Using("src/Services/CarService.cs", "src/Models/Car.cs"))).WhereLayer("Services");

        var only = subject.MayOnlyDependOnLayers("Models");
        var forbid = subject.MayNotDependOnLayers("Models");

        Assert.Empty(only.Check());
        Assert.Equal(
            new Violation[]
            {
                new LayerViolation("Services", "src/Services/CarService.cs", "src/Models/Car.cs", "Models"),
            },
            forbid.Check());
    }

    [Fact]
    public void A_rule_can_be_checked_twice_and_reports_the_same_result()
    {
        var rule = Policy(Graph(
            Self("src/Models/Car.cs"),
            Self("src/Services/CarService.cs"),
            Using("src/Services/CarService.cs", "src/Models/Car.cs")))
            .WhereLayer("Services")
            .MayNotDependOnLayers("Models");

        IReadOnlyList<Violation> first = rule.Check();
        IReadOnlyList<Violation> second = rule.Check();

        Assert.Equal(first, second);
        Assert.NotSame(first, second);
    }

    [Fact]
    public void A_rule_with_no_constraints_raises_a_user_error_when_checked()
    {
        var rule = Policy(Graph(Self("src/Services/CarService.cs"))).WhereLayer("Services");

        Assert.Throws<UserError>(() => rule.Check());
    }

    [Fact]
    public void MayOnlyDependOnLayers_guards_a_subject_that_matches_no_files()
    {
        var rule = Policy(Graph(Self("src/App/Program.cs")))
            .WhereLayer("Services")
            .MayOnlyDependOnLayers("Models");

        IReadOnlyList<Violation> violations = rule.Check();

        Assert.Equal(
            new Violation[]
            {
                new EmptyTestViolation(
                    "layer 'Services' defined by folder 'src/Services' may only depend on layers 'Models'"),
            },
            violations);
    }

    [Fact]
    public void MayOnlyDependOnLayers_honours_allow_empty_tests()
    {
        var rule = Policy(Graph(Self("src/App/Program.cs")))
            .WhereLayer("Services")
            .MayOnlyDependOnLayers("Models");

        IReadOnlyList<Violation> violations = rule.Check(new CheckOptions { AllowEmptyTests = true });

        Assert.Empty(violations);
    }

    [Fact]
    public void MayOnlyDependOnLayers_rejects_a_null_layer_name()
    {
        Assert.Throws<ArgumentNullException>(() =>
            Policy(Graph(Self("a.cs"))).WhereLayer("Services").MayOnlyDependOnLayers(null!));
    }

    [Fact]
    public void MayOnlyDependOnLayers_rejects_an_empty_layer_name()
    {
        Assert.Throws<ArgumentException>(() =>
            Policy(Graph(Self("a.cs"))).WhereLayer("Services").MayOnlyDependOnLayers(string.Empty));
    }

    [Fact]
    public void MayOnlyDependOnLayers_rejects_an_undeclared_layer()
    {
        Assert.Throws<UserError>(() =>
            Policy(Graph(Self("a.cs"))).WhereLayer("Services").MayOnlyDependOnLayers("Repository"));
    }

    [Fact]
    public void MayNotDependOnLayers_rejects_the_subject_layer_itself()
    {
        Assert.Throws<UserError>(() =>
            Policy(Graph(Self("a.cs"))).WhereLayer("Services").MayNotDependOnLayers("Services"));
    }

    [Fact]
    public void MayOnlyDependOnLayers_accepts_the_subject_layer_as_redundant()
    {
        var rule = Policy(Graph(
            Self("src/Services/A.cs"),
            Self("src/Services/B.cs"),
            Using("src/Services/A.cs", "src/Services/B.cs")))
            .WhereLayer("Services")
            .MayOnlyDependOnLayers("Services");

        Assert.Empty(rule.Check());
    }

    [Fact]
    public void MayOnlyDependOnLayers_with_no_arguments_is_a_sealed_layer()
    {
        var rule = Policy(Graph(
            Self("src/Models/Car.cs"),
            Self("src/Services/CarService.cs"),
            Using("src/Services/CarService.cs", "src/Models/Car.cs")))
            .WhereLayer("Services")
            .MayOnlyDependOnLayers();

        Assert.Equal(
            new Violation[]
            {
                new LayerViolation("Services", "src/Services/CarService.cs", "src/Models/Car.cs", "Models"),
            },
            rule.Check());
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
