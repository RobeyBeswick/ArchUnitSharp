using ArchUnitSharp.Common.Extraction;
using ArchUnitSharp.Metrics.Assertion;

namespace ArchUnitSharp.Metrics.Tests;

public class DistanceMetricsAssertionTests
{
    [Fact]
    public void Abstractness_flags_a_concrete_file()
    {
        IReadOnlyList<Violation> violations = Check(
            Rule(Project(("src/A.cs", Concrete)).Distance().Abstractness().ShouldBeAbove(0.3)));

        Assert.Equal(
            new Violation[]
            {
                new DistanceMetricViolation("src/A.cs", DistanceMetricKind.Abstractness, 0.0, MetricComparison.Above, 0.3),
            },
            violations);
    }

    [Fact]
    public void Abstractness_passes_for_an_abstract_file()
    {
        IReadOnlyList<Violation> violations = Check(
            Rule(Project(("src/A.cs", Abstract)).Distance().Abstractness().ShouldBeAbove(0.3)));

        Assert.Empty(violations);
    }

    [Fact]
    public void Instability_flags_a_file_that_only_depends_outward()
    {
        var project = Project(
            new[]
            {
                ("src/A.cs", Concrete),
                ("src/B.cs", Concrete),
                ("src/C.cs", Concrete),
            },
            ("src/A.cs", "src/B.cs"),
            ("src/A.cs", "src/C.cs"));

        IReadOnlyList<Violation> violations = Check(
            Rule(project.Distance().Instability().ShouldBe(0.0)));

        Assert.Equal(
            new Violation[]
            {
                new DistanceMetricViolation("src/A.cs", DistanceMetricKind.Instability, 1.0, MetricComparison.Equal, 0.0),
            },
            violations);
    }

    [Fact]
    public void Instability_passes_when_every_file_is_stable()
    {
        IReadOnlyList<Violation> violations = Check(
            Rule(Project(("src/A.cs", Concrete)).Distance().Instability().ShouldBe(0.0)));

        Assert.Empty(violations);
    }

    [Fact]
    public void Distance_from_the_main_sequence_flags_a_concrete_uncoupled_file()
    {
        IReadOnlyList<Violation> violations = Check(
            Rule(Project(("src/A.cs", Concrete)).Distance().DistanceFromMainSequence().ShouldBeBelow(0.5)));

        Assert.Equal(
            new Violation[]
            {
                new DistanceMetricViolation("src/A.cs", DistanceMetricKind.DistanceFromMainSequence, 1.0, MetricComparison.Below, 0.5),
            },
            violations);
    }

    [Fact]
    public void Distance_from_the_main_sequence_passes_on_the_line()
    {
        IReadOnlyList<Violation> violations = Check(
            Rule(Project(("src/A.cs", Abstract)).Distance().DistanceFromMainSequence().ShouldBeBelow(0.5)));

        Assert.Empty(violations);
    }

    [Fact]
    public void Coupling_factor_flags_the_most_coupled_file()
    {
        var project = Project(
            new[]
            {
                ("src/A.cs", Concrete),
                ("src/B.cs", Concrete),
                ("src/C.cs", Concrete),
            },
            ("src/A.cs", "src/B.cs"),
            ("src/A.cs", "src/C.cs"),
            ("src/B.cs", "src/A.cs"));

        IReadOnlyList<Violation> violations = Check(
            Rule(project.Distance().CouplingFactor().ShouldBeBelow(0.7)));

        Assert.Equal(
            new Violation[]
            {
                new DistanceMetricViolation("src/A.cs", DistanceMetricKind.CouplingFactor, 0.75, MetricComparison.Below, 0.7),
            },
            violations);
    }

    [Fact]
    public void Normalised_distance_discounts_a_large_concrete_file_to_half()
    {
        string large = string.Join(
            "\n",
            Enumerable.Range(0, 150).Select(static index => $"// line {index}"))
            + "\nnamespace App;\npublic class Car { }\n";

        IReadOnlyList<Violation> violations = Check(
            Rule(Project(("src/A.cs", large)).Distance().NormalisedDistance().ShouldBeBelow(0.4)));

        Assert.Equal(
            new Violation[]
            {
                new DistanceMetricViolation("src/A.cs", DistanceMetricKind.NormalisedDistance, 0.5, MetricComparison.Below, 0.4),
            },
            violations);
    }

    [Fact]
    public void ShouldSatisfy_flags_every_value_the_predicate_rejects()
    {
        IReadOnlyList<Violation> violations = Check(
            Rule(Project(("src/A.cs", Concrete)).Distance().Abstractness().ShouldSatisfy(
                static value => value > 0.3,
                "every file is abstract enough")));

        Assert.Equal(
            new Violation[]
            {
                new DistanceMetricViolation("src/A.cs", DistanceMetricKind.Abstractness, 0.0, "every file is abstract enough"),
            },
            violations);
    }

    [Fact]
    public void ShouldSatisfy_passes_when_every_value_the_predicate_accepts()
    {
        IReadOnlyList<Violation> violations = Check(
            Rule(Project(("src/A.cs", Abstract)).Distance().Abstractness().ShouldSatisfy(
                static value => value > 0.3,
                "every file is abstract enough")));

        Assert.Empty(violations);
    }

    [Fact]
    public void Not_in_zone_of_pain_flags_a_concrete_stable_file()
    {
        IReadOnlyList<Violation> violations = Check(
            ZoneRule(Project(("src/A.cs", Concrete)).Distance().NotInZoneOfPain()));

        Assert.Equal(
            new Violation[]
            {
                new DistanceZoneViolation("src/A.cs", DistanceZone.Pain, 0.0, 0.0),
            },
            violations);
    }

    [Fact]
    public void Not_in_zone_of_pain_passes_for_a_file_off_the_corner()
    {
        IReadOnlyList<Violation> violations = Check(
            ZoneRule(Project(("src/A.cs", HalfAbstract)).Distance().NotInZoneOfPain()));

        Assert.Empty(violations);
    }

    [Fact]
    public void Not_in_zone_of_uselessness_flags_an_abstract_dependent_file()
    {
        var project = Project(
            new[] { ("src/A.cs", Abstract), ("src/B.cs", Concrete) },
            ("src/A.cs", "src/B.cs"));

        IReadOnlyList<Violation> violations = Check(
            ZoneRule(project.Distance().NotInZoneOfUselessness()));

        Assert.Equal(
            new Violation[]
            {
                new DistanceZoneViolation("src/A.cs", DistanceZone.Uselessness, 1.0, 1.0),
            },
            violations);
    }

    [Fact]
    public void Not_in_zone_of_uselessness_passes_for_a_used_abstraction()
    {
        var project = Project(
            new[] { ("src/A.cs", Abstract), ("src/B.cs", Concrete) },
            ("src/B.cs", "src/A.cs"));

        IReadOnlyList<Violation> violations = Check(
            ZoneRule(project.Distance().NotInZoneOfUselessness()));

        Assert.Empty(violations);
    }

    [Fact]
    public void For_classes_matching_narrows_a_distance_rules_file_subjects()
    {
        var project = Project(
            new[]
            {
                ("src/A.cs", "namespace App;\npublic class Car { }\n"),
                ("src/B.cs", "namespace App;\npublic class CarController { }\n"),
            },
            ("src/A.cs", "src/B.cs"),
            ("src/B.cs", "src/A.cs"));

        IReadOnlyList<Violation> violations = Check(
            Rule(project
                .ForClassesMatching("*Controller")
                .Distance()
                .Instability()
                .ShouldBe(0.0)));

        var violation = Assert.Single(violations);
        Assert.Equal("src/B.cs", Assert.IsType<DistanceMetricViolation>(violation).File);
    }

    [Fact]
    public void Violations_are_reported_in_file_subject_order()
    {
        var project = Project(
            new[] { ("src/Z.cs", Concrete), ("src/A.cs", Concrete) },
            ("src/A.cs", "src/Z.cs"),
            ("src/Z.cs", "src/A.cs"));

        IReadOnlyList<Violation> violations = Check(
            Rule(project.Distance().DistanceFromMainSequence().ShouldBeBelow(0.5)));

        Assert.Equal(
            new[] { "src/A.cs", "src/Z.cs" },
            violations.Select(static violation => ((DistanceMetricViolation)violation).File));
    }

    [Fact]
    public void An_empty_file_selection_is_guarded()
    {
        IReadOnlyList<Violation> violations = Check(
            Rule(new Metrics(Graph()).Distance().Instability().ShouldBeBelow(0.8)));

        var empty = Assert.Single(violations);
        Assert.IsType<EmptyTestViolation>(empty);
        Assert.Equal(
            "project metrics instability should be below 0.8",
            Assert.IsType<EmptyTestViolation>(empty).RuleDescription);
    }

    [Fact]
    public void The_guard_names_a_zone_rules_own_words()
    {
        IReadOnlyList<Violation> violations = Check(
            ZoneRule(new Metrics(Graph()).InFolder("src").Distance().NotInZoneOfPain()));

        var empty = Assert.Single(violations);
        Assert.Equal(
            "project metrics in folder 'src' not in zone of pain",
            Assert.IsType<EmptyTestViolation>(empty).RuleDescription);
    }

    [Fact]
    public void The_guard_names_a_should_satisfy_distance_rules_message()
    {
        IReadOnlyList<Violation> violations = Check(
            Rule(new Metrics(Graph()).Distance().Abstractness().ShouldSatisfy(
                static value => value > 0.3,
                "every file is abstract enough")));

        var empty = Assert.Single(violations);
        Assert.Equal(
            "project metrics abstractness should satisfy 'every file is abstract enough'",
            Assert.IsType<EmptyTestViolation>(empty).RuleDescription);
    }

    [Fact]
    public void A_class_selector_that_leaves_no_subject_is_guarded()
    {
        IReadOnlyList<Violation> violations = Check(
            Rule(Project(("src/Car.cs", Concrete))
                .ForClassesMatching("*.Controller")
                .Distance()
                .Instability()
                .ShouldBeBelow(0.8)));

        var empty = Assert.Single(violations);
        Assert.Equal(
            "project metrics for classes matching '*.Controller' instability should be below 0.8",
            Assert.IsType<EmptyTestViolation>(empty).RuleDescription);
    }

    [Fact]
    public void Allow_empty_tests_passes_an_empty_file_selection()
    {
        IReadOnlyList<Violation> violations = Check(
            Rule(new Metrics(Graph()).Distance().Instability().ShouldBeBelow(0.8)),
            options: new CheckOptions { AllowEmptyTests = true });

        Assert.Empty(violations);
    }

    [Fact]
    public void A_scope_without_a_source_provider_raises_a_user_error()
    {
        var rule = (DistanceMetricRule)new Metrics(Graph(Self("src/A.cs")))
            .Distance()
            .Abstractness()
            .ShouldBeAbove(0.3);

        Assert.Throws<UserError>(() => MetricsAssertion.CheckDistance(rule, options: null));
    }

    [Fact]
    public void A_zone_rule_over_a_scope_without_a_source_provider_raises_a_user_error()
    {
        var rule = (DistanceZoneRule)new Metrics(Graph(Self("src/A.cs")))
            .Distance()
            .NotInZoneOfPain();

        Assert.Throws<UserError>(() => MetricsAssertion.CheckZone(rule, options: null));
    }

    [Fact]
    public void CheckDistance_rejects_a_null_rule()
    {
        Assert.Throws<ArgumentNullException>(() => MetricsAssertion.CheckDistance(null!, options: null));
    }

    [Fact]
    public void CheckZone_rejects_a_null_rule()
    {
        Assert.Throws<ArgumentNullException>(() => MetricsAssertion.CheckZone(null!, options: null));
    }

    private static Metrics Project(params (string Path, string Source)[] files) =>
        Project(files, Array.Empty<(string Source, string Target)>());

    private static Metrics Project(
        (string Path, string Source)[] files,
        params (string Source, string Target)[] couplings)
    {
        var sources = files.ToDictionary(
            static file => file.Path,
            static file => file.Source,
            StringComparer.Ordinal);
        Edge[] edges = files
            .Select(static file => Self(file.Path))
            .Concat(couplings.Select(coupling =>
                new Edge(coupling.Source, coupling.Target, external: false, ImportKind.Using)))
            .ToArray();
        return new Metrics(new Graph(edges), identifier => sources[identifier]);
    }

    private static IReadOnlyList<Violation> Check(
        DistanceMetricRule rule,
        CheckOptions? options = null) =>
        MetricsAssertion.CheckDistance(rule, options);

    private static IReadOnlyList<Violation> Check(
        DistanceZoneRule rule,
        CheckOptions? options = null) =>
        MetricsAssertion.CheckZone(rule, options);

    private static DistanceMetricRule Rule(ICheckable checkable) =>
        Assert.IsType<DistanceMetricRule>(checkable);

    private static DistanceZoneRule ZoneRule(ICheckable checkable) =>
        Assert.IsType<DistanceZoneRule>(checkable);

    private const string Concrete = "namespace App;\npublic class Car { }\n";

    private const string Abstract = "namespace App;\npublic interface IThing { }\n";

    private const string HalfAbstract =
        "namespace App;\npublic interface IThing { }\npublic class Car { }\n";

    private static Graph Graph(params Edge[] edges) => new(edges);

    private static Edge Self(string file) => new(file, file, external: false, ImportKind.None);
}
