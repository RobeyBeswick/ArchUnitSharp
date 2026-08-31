using ArchUnitSharp.Common.Extraction;
using ArchUnitSharp.Metrics.Assertion;

namespace ArchUnitSharp.Metrics.Tests;

public class LcomMetricsAssertionTests
{
    [Fact]
    public void Lcom96a_passes_when_every_value_meets_the_threshold()
    {
        IReadOnlyList<Violation> violations = Check(
            Rule(Project("src/Split.cs", Split).Lcom().Lcom96a().ShouldBe(1.0)));

        Assert.Empty(violations);
    }

    [Fact]
    public void Lcom96a_flags_every_class_whose_value_misses_the_threshold()
    {
        IReadOnlyList<Violation> violations = Check(
            Rule(Project("src/Split.cs", Split).Lcom().Lcom96a().ShouldBe(0.5)));

        Assert.Equal(
            new Violation[]
            {
                new LcomMetricViolation("src/Split.cs", "App.Split", LcomMetricKind.Lcom96a, 1.0, MetricComparison.Equal, 0.5),
            },
            violations);
    }

    [Fact]
    public void Lcom96b_passes_when_every_value_meets_the_threshold()
    {
        IReadOnlyList<Violation> violations = Check(
            Rule(Project("src/Split.cs", Split).Lcom().Lcom96b().ShouldBe(0.5)));

        Assert.Empty(violations);
    }

    [Fact]
    public void Lcom96b_flags_a_value_strictly_above_the_threshold()
    {
        IReadOnlyList<Violation> violations = Check(
            Rule(Project("src/Split.cs", Split).Lcom().Lcom96b().ShouldBeBelow(0.4)));

        Assert.Equal(
            new Violation[]
            {
                new LcomMetricViolation("src/Split.cs", "App.Split", LcomMetricKind.Lcom96b, 0.5, MetricComparison.Below, 0.4),
            },
            violations);
    }

    [Fact]
    public void Lcom96b_flags_a_value_at_the_threshold_as_not_above()
    {
        IReadOnlyList<Violation> violations = Check(
            Rule(Project("src/Split.cs", Split).Lcom().Lcom96b().ShouldBeAbove(0.5)));

        Assert.Equal(
            new Violation[]
            {
                new LcomMetricViolation("src/Split.cs", "App.Split", LcomMetricKind.Lcom96b, 0.5, MetricComparison.Above, 0.5),
            },
            violations);
    }

    [Fact]
    public void Lcom96b_passes_a_value_strictly_above_the_threshold()
    {
        IReadOnlyList<Violation> violations = Check(
            Rule(Project("src/Split.cs", Split).Lcom().Lcom96b().ShouldBeAbove(0.4)));

        Assert.Empty(violations);
    }

    [Fact]
    public void Lcom96b_passes_a_value_at_the_threshold_when_below_or_equal()
    {
        IReadOnlyList<Violation> violations = Check(
            Rule(Project("src/Split.cs", Split).Lcom().Lcom96b().ShouldBeBelowOrEqual(0.5)));

        Assert.Empty(violations);
    }

    [Fact]
    public void Lcom96b_flags_a_value_strictly_above_the_threshold_when_below_or_equal()
    {
        IReadOnlyList<Violation> violations = Check(
            Rule(Project("src/Split.cs", Split).Lcom().Lcom96b().ShouldBeBelowOrEqual(0.4)));

        Assert.Equal(
            new Violation[]
            {
                new LcomMetricViolation("src/Split.cs", "App.Split", LcomMetricKind.Lcom96b, 0.5, MetricComparison.BelowOrEqual, 0.4),
            },
            violations);
    }

    [Fact]
    public void Lcom96b_passes_a_value_at_the_threshold_when_above_or_equal()
    {
        IReadOnlyList<Violation> violations = Check(
            Rule(Project("src/Split.cs", Split).Lcom().Lcom96b().ShouldBeAboveOrEqual(0.5)));

        Assert.Empty(violations);
    }

    [Fact]
    public void Lcom96b_flags_a_value_strictly_below_the_threshold_when_above_or_equal()
    {
        IReadOnlyList<Violation> violations = Check(
            Rule(Project("src/Split.cs", Split).Lcom().Lcom96b().ShouldBeAboveOrEqual(0.6)));

        Assert.Equal(
            new Violation[]
            {
                new LcomMetricViolation("src/Split.cs", "App.Split", LcomMetricKind.Lcom96b, 0.5, MetricComparison.AboveOrEqual, 0.6),
            },
            violations);
    }

    [Fact]
    public void Lcom1_flags_the_disjoint_pairs_of_a_split_class()
    {
        IReadOnlyList<Violation> violations = Check(
            Rule(Project("src/Split.cs", Split).Lcom().Lcom1().ShouldBe(0.0)));

        Assert.Equal(
            new Violation[]
            {
                new LcomMetricViolation("src/Split.cs", "App.Split", LcomMetricKind.Lcom1, 1.0, MetricComparison.Equal, 0.0),
            },
            violations);
    }

    [Fact]
    public void Lcom4_flags_a_class_with_more_than_one_component()
    {
        IReadOnlyList<Violation> violations = Check(
            Rule(Project("src/Split.cs", Split).Lcom().Lcom4().ShouldBe(1)));

        Assert.Equal(
            new Violation[]
            {
                new LcomMetricViolation("src/Split.cs", "App.Split", LcomMetricKind.Lcom4, 2.0, MetricComparison.Equal, 1.0),
            },
            violations);
    }

    [Fact]
    public void Lcom4_passes_for_a_cohesive_class()
    {
        IReadOnlyList<Violation> violations = Check(
            Rule(Project("src/Cohesive.cs", Cohesive).Lcom().Lcom4().ShouldBe(1)));

        Assert.Empty(violations);
    }

    [Fact]
    public void ShouldSatisfy_flags_every_value_the_predicate_rejects()
    {
        IReadOnlyList<Violation> violations = Check(
            Rule(Project("src/Split.cs", Split).Lcom().Lcom96b().ShouldSatisfy(
                static value => value < 0.4,
                "every class is cohesive")));

        Assert.Equal(
            new Violation[]
            {
                new LcomMetricViolation("src/Split.cs", "App.Split", LcomMetricKind.Lcom96b, 0.5, "every class is cohesive"),
            },
            violations);
    }

    [Fact]
    public void ShouldSatisfy_passes_when_every_value_the_predicate_accepts()
    {
        IReadOnlyList<Violation> violations = Check(
            Rule(Project("src/Split.cs", Split).Lcom().Lcom96b().ShouldSatisfy(
                static value => value < 1.0,
                "every class is cohesive")));

        Assert.Empty(violations);
    }

    [Fact]
    public void ForClassesMatching_narrows_the_classes_that_are_measured()
    {
        const string source =
            "namespace App;\n" +
            "public class Cohesive\n" +
            "{\n" +
            "    private int _a;\n" +
            "    private int _b;\n" +
            "    public void A() { _a = 1; }\n" +
            "    public void B() { _b = 2; }\n" +
            "}\n" +
            "public class Split\n" +
            "{\n" +
            "    private int _a;\n" +
            "    private int _b;\n" +
            "    public void A() { _a = 1; }\n" +
            "    public void B() { _b = 2; }\n" +
            "}\n";

        IReadOnlyList<Violation> violations = Check(
            Rule(Project("src/Classes.cs", source)
                .ForClassesMatching("*Split")
                .Lcom()
                .Lcom4()
                .ShouldBe(1)));

        Assert.Equal(
            new Violation[]
            {
                new LcomMetricViolation("src/Classes.cs", "App.Split", LcomMetricKind.Lcom4, 2.0, MetricComparison.Equal, 1.0),
            },
            violations);
    }

    [Fact]
    public void Violations_are_reported_in_class_identifier_order()
    {
        const string source =
            "namespace App;\n" +
            "public class Zeta\n" +
            "{\n" +
            "    private int _a;\n" +
            "    private int _b;\n" +
            "    public void A() { _a = 1; }\n" +
            "    public void B() { _b = 2; }\n" +
            "}\n" +
            "public class Alpha\n" +
            "{\n" +
            "    private int _a;\n" +
            "    private int _b;\n" +
            "    public void A() { _a = 1; }\n" +
            "    public void B() { _b = 2; }\n" +
            "}\n";

        IReadOnlyList<Violation> violations = Check(
            Rule(Project("src/Classes.cs", source).Lcom().Lcom4().ShouldBe(1)));

        Assert.Equal(
            new[] { "App.Alpha", "App.Zeta" },
            violations.Select(static violation => ((LcomMetricViolation)violation).Class));
    }

    [Fact]
    public void An_empty_file_selection_is_guarded()
    {
        IReadOnlyList<Violation> violations = Check(
            Rule(new Metrics(Graph()).Lcom().Lcom4().ShouldBe(1)));

        var empty = Assert.Single(violations);
        Assert.IsType<EmptyTestViolation>(empty);
        Assert.Equal("project metrics lcom4 should be 1", Assert.IsType<EmptyTestViolation>(empty).RuleDescription);
    }

    [Fact]
    public void The_guard_names_the_metric_and_the_threshold_in_the_rules_own_words()
    {
        IReadOnlyList<Violation> violations = Check(
            Rule(new Metrics(Graph()).InFolder("src").Lcom().Lcom96b().ShouldBeBelow(0.8)));

        var empty = Assert.Single(violations);
        Assert.Equal(
            "project metrics in folder 'src' lcom96b should be below 0.8",
            Assert.IsType<EmptyTestViolation>(empty).RuleDescription);
    }

    [Fact]
    public void The_guard_names_a_should_satisfy_rules_message()
    {
        IReadOnlyList<Violation> violations = Check(
            Rule(new Metrics(Graph()).Lcom().Lcom4().ShouldSatisfy(
                static value => value == 1.0,
                "every class is cohesive")));

        var empty = Assert.Single(violations);
        Assert.Equal(
            "project metrics lcom4 should satisfy 'every class is cohesive'",
            Assert.IsType<EmptyTestViolation>(empty).RuleDescription);
    }

    [Fact]
    public void A_class_selector_that_leaves_no_class_is_guarded()
    {
        const string source = "namespace App;\npublic class Car { }\n";

        IReadOnlyList<Violation> violations = Check(
            Rule(Project("src/Car.cs", source)
                .ForClassesMatching("*.Controller")
                .Lcom()
                .Lcom4()
                .ShouldBe(1)));

        var empty = Assert.Single(violations);
        Assert.IsType<EmptyTestViolation>(empty);
        Assert.Equal(
            "project metrics for classes matching '*.Controller' lcom4 should be 1",
            Assert.IsType<EmptyTestViolation>(empty).RuleDescription);
    }

    [Fact]
    public void Allow_empty_tests_passes_an_empty_file_selection()
    {
        IReadOnlyList<Violation> violations = Check(
            Rule(new Metrics(Graph()).Lcom().Lcom4().ShouldBe(1)),
            options: new CheckOptions { AllowEmptyTests = true });

        Assert.Empty(violations);
    }

    [Fact]
    public void A_scope_without_a_source_provider_raises_a_user_error()
    {
        var rule = (LcomMetricRule)new Metrics(Graph(Self("src/A.cs")))
            .Lcom()
            .Lcom4()
            .ShouldBe(1);

        Assert.Throws<UserError>(() => MetricsAssertion.CheckLcom(rule, options: null));
    }

    [Fact]
    public void CheckLcom_rejects_a_null_rule()
    {
        Assert.Throws<ArgumentNullException>(() => MetricsAssertion.CheckLcom(null!, options: null));
    }

    private static Metrics Project(string path, string source) =>
        new(Graph(Self(path)), _ => source);

    private static IReadOnlyList<Violation> Check(LcomMetricRule rule, CheckOptions? options = null) =>
        MetricsAssertion.CheckLcom(rule, options);

    private static LcomMetricRule Rule(ICheckable checkable) => Assert.IsType<LcomMetricRule>(checkable);

    private const string Cohesive =
        "namespace App;\n" +
        "public class Cohesive\n" +
        "{\n" +
        "    private int _a;\n" +
        "    private int _b;\n" +
        "    public void A() { _a = 1; _b = 2; }\n" +
        "    public void B() { _a = 3; }\n" +
        "}\n";

    private const string Split =
        "namespace App;\n" +
        "public class Split\n" +
        "{\n" +
        "    private int _a;\n" +
        "    private int _b;\n" +
        "    public void A() { _a = 1; }\n" +
        "    public void B() { _b = 2; }\n" +
        "}\n";

    private static Graph Graph(params Edge[] edges) => new(edges);

    private static Edge Self(string file) => new(file, file, external: false, ImportKind.None);
}
