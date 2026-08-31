using ArchUnitSharp.Common.Extraction;
using ArchUnitSharp.Metrics.Assertion;

namespace ArchUnitSharp.Metrics.Tests;

public class MetricsAssertionTests
{
    [Fact]
    public void A_file_metric_passes_when_every_value_meets_the_threshold()
    {
        IReadOnlyList<Violation> violations = Check(
            Rule(Project("src/A.cs", SmallCar).Count().LinesOfCode().ShouldBeBelow(100)));

        Assert.Empty(violations);
    }

    [Fact]
    public void A_file_metric_flags_every_file_whose_value_misses_the_threshold()
    {
        IReadOnlyList<Violation> violations = Check(
            Rule(Project("src/A.cs", SmallCar).Count().LinesOfCode().ShouldBeBelow(5)));

        Assert.Equal(
            new Violation[]
            {
                new MetricViolation("src/A.cs", null, CountMetricKind.LinesOfCode, 5, MetricComparison.Below, 5),
            },
            violations);
    }

    [Fact]
    public void ShouldBe_passes_when_the_value_equals_the_threshold()
    {
        IReadOnlyList<Violation> violations = Check(
            Rule(Project("src/A.cs", SmallCar).Count().Classes().ShouldBe(1)));

        Assert.Empty(violations);
    }

    [Fact]
    public void ShouldBe_flags_a_value_that_is_not_the_threshold()
    {
        IReadOnlyList<Violation> violations = Check(
            Rule(Project("src/A.cs", SmallCar).Count().Classes().ShouldBe(2)));

        Assert.Equal(
            new Violation[]
            {
                new MetricViolation("src/A.cs", null, CountMetricKind.Classes, 1, MetricComparison.Equal, 2),
            },
            violations);
    }

    [Fact]
    public void ShouldBeBelow_passes_for_a_value_strictly_below_the_threshold()
    {
        IReadOnlyList<Violation> violations = Check(
            Rule(Project("src/A.cs", SmallCar).Count().LinesOfCode().ShouldBeBelow(7)));

        Assert.Empty(violations);
    }

    [Fact]
    public void ShouldBeBelow_flags_a_value_strictly_above_the_threshold()
    {
        IReadOnlyList<Violation> violations = Check(
            Rule(Project("src/A.cs", SmallCar).Count().LinesOfCode().ShouldBeBelow(3)));

        Assert.Equal(
            new Violation[]
            {
                new MetricViolation("src/A.cs", null, CountMetricKind.LinesOfCode, 5, MetricComparison.Below, 3),
            },
            violations);
    }

    [Fact]
    public void ShouldBeAbove_passes_for_a_value_strictly_above_the_threshold()
    {
        IReadOnlyList<Violation> violations = Check(
            Rule(Project("src/A.cs", SmallCar).Count().LinesOfCode().ShouldBeAbove(3)));

        Assert.Empty(violations);
    }

    [Fact]
    public void ShouldBeAbove_flags_a_value_strictly_below_the_threshold()
    {
        IReadOnlyList<Violation> violations = Check(
            Rule(Project("src/A.cs", SmallCar).Count().LinesOfCode().ShouldBeAbove(7)));

        Assert.Equal(
            new Violation[]
            {
                new MetricViolation("src/A.cs", null, CountMetricKind.LinesOfCode, 5, MetricComparison.Above, 7),
            },
            violations);
    }

    [Fact]
    public void ShouldBeBelowOrEqual_passes_for_a_value_strictly_below_the_threshold()
    {
        IReadOnlyList<Violation> violations = Check(
            Rule(Project("src/A.cs", SmallCar).Count().LinesOfCode().ShouldBeBelowOrEqual(7)));

        Assert.Empty(violations);
    }

    [Fact]
    public void ShouldBeBelowOrEqual_flags_a_value_strictly_above_the_threshold()
    {
        IReadOnlyList<Violation> violations = Check(
            Rule(Project("src/A.cs", SmallCar).Count().LinesOfCode().ShouldBeBelowOrEqual(3)));

        Assert.Equal(
            new Violation[]
            {
                new MetricViolation("src/A.cs", null, CountMetricKind.LinesOfCode, 5, MetricComparison.BelowOrEqual, 3),
            },
            violations);
    }

    [Fact]
    public void ShouldBeAboveOrEqual_passes_for_a_value_strictly_above_the_threshold()
    {
        IReadOnlyList<Violation> violations = Check(
            Rule(Project("src/A.cs", SmallCar).Count().LinesOfCode().ShouldBeAboveOrEqual(3)));

        Assert.Empty(violations);
    }

    [Fact]
    public void ShouldBeAboveOrEqual_flags_a_value_strictly_below_the_threshold()
    {
        IReadOnlyList<Violation> violations = Check(
            Rule(Project("src/A.cs", SmallCar).Count().LinesOfCode().ShouldBeAboveOrEqual(7)));

        Assert.Equal(
            new Violation[]
            {
                new MetricViolation("src/A.cs", null, CountMetricKind.LinesOfCode, 5, MetricComparison.AboveOrEqual, 7),
            },
            violations);
    }

    [Fact]
    public void ShouldBeBelowOrEqual_passes_at_the_boundary()
    {
        IReadOnlyList<Violation> violations = Check(
            Rule(Project("src/A.cs", SmallCar).Count().LinesOfCode().ShouldBeBelowOrEqual(5)));

        Assert.Empty(violations);
    }

    [Fact]
    public void ShouldBeAboveOrEqual_passes_at_the_boundary()
    {
        IReadOnlyList<Violation> violations = Check(
            Rule(Project("src/A.cs", SmallCar).Count().LinesOfCode().ShouldBeAboveOrEqual(5)));

        Assert.Empty(violations);
    }

    [Fact]
    public void ShouldBeAboveOrEqual_flags_a_value_below_the_boundary()
    {
        IReadOnlyList<Violation> violations = Check(
            Rule(Project("src/A.cs", SmallCar).Count().LinesOfCode().ShouldBeAboveOrEqual(6)));

        Assert.Equal(
            new Violation[]
            {
                new MetricViolation("src/A.cs", null, CountMetricKind.LinesOfCode, 5, MetricComparison.AboveOrEqual, 6),
            },
            violations);
    }

    [Fact]
    public void ShouldBeAbove_flags_a_value_equal_to_the_threshold()
    {
        IReadOnlyList<Violation> violations = Check(
            Rule(Project("src/A.cs", SmallCar).Count().LinesOfCode().ShouldBeAbove(5)));

        Assert.Equal(
            new Violation[]
            {
                new MetricViolation("src/A.cs", null, CountMetricKind.LinesOfCode, 5, MetricComparison.Above, 5),
            },
            violations);
    }

    [Fact]
    public void A_class_metric_measures_each_class_separately()
    {
        const string source =
            "namespace App;\n" +
            "public class Small { public void A() { } }\n" +
            "public class Big { public void A() { } public void B() { } public void C() { } }\n";

        IReadOnlyList<Violation> violations = Check(
            Rule(Project("src/Classes.cs", source).Count().MethodCount().ShouldBe(1)));

        Assert.Equal(
            new Violation[]
            {
                new MetricViolation(
                    "src/Classes.cs",
                    "App.Big",
                    CountMetricKind.MethodCount,
                    3,
                    MetricComparison.Equal,
                    1),
            },
            violations);
    }

    [Fact]
    public void FieldCount_counts_every_declared_variable_not_properties()
    {
        const string source =
            "namespace App;\n" +
            "public class Car { private int a, b; public int X { get; set; } }\n";

        IReadOnlyList<Violation> violations = Check(
            Rule(Project("src/Car.cs", source).Count().FieldCount().ShouldBe(2)));

        Assert.Empty(violations);
    }

    [Fact]
    public void ForClassesMatching_narrows_a_class_metrics_subjects()
    {
        const string source =
            "namespace App;\n" +
            "public class CarController { public void A() { } }\n" +
            "public class Car { public void A() { } public void B() { } }\n";

        IReadOnlyList<Violation> violations = Check(
            Rule(Project("src/Classes.cs", source)
                .ForClassesMatching("*Controller")
                .Count()
                .MethodCount()
                .ShouldBe(1)));

        Assert.Empty(violations);
    }

    [Fact]
    public void ForClassesMatching_narrows_a_file_metrics_subjects_to_files_with_a_matching_class()
    {
        var project = Project(
            ("src/A.cs", "namespace App;\npublic class CarController { }\n"),
            ("src/B.cs", "namespace App;\npublic class Car { }\npublic class Van { }\n"));

        IReadOnlyList<Violation> violations = Check(
            Rule(project
                .ForClassesMatching("*Controller")
                .Count()
                .Classes()
                .ShouldBe(1)));

        Assert.Empty(violations);
    }

    [Fact]
    public void A_file_metric_measures_a_surviving_file_whole()
    {
        const string source =
            "namespace App;\n" +
            "public class CarController { }\n" +
            "public class Helper { }\n";

        IReadOnlyList<Violation> violations = Check(
            Rule(Project("src/A.cs", source)
                .ForClassesMatching("*Controller")
                .Count()
                .Classes()
                .ShouldBe(2)));

        Assert.Empty(violations);
    }

    [Fact]
    public void ShouldSatisfy_flags_every_value_the_predicate_rejects()
    {
        IReadOnlyList<Violation> violations = Check(
            Rule(Project("src/A.cs", SmallCar).Count().LinesOfCode().ShouldSatisfy(
                static value => value < 5,
                "every file is short")));

        Assert.Equal(
            new Violation[]
            {
                new MetricViolation("src/A.cs", null, CountMetricKind.LinesOfCode, 5, "every file is short"),
            },
            violations);
    }

    [Fact]
    public void ShouldSatisfy_passes_when_every_value_the_predicate_accepts()
    {
        IReadOnlyList<Violation> violations = Check(
            Rule(Project("src/A.cs", SmallCar).Count().LinesOfCode().ShouldSatisfy(
                static value => value < 10,
                "every file is short")));

        Assert.Empty(violations);
    }

    [Fact]
    public void Violations_are_reported_in_file_subject_order()
    {
        var project = Project(
            ("src/Z.cs", "namespace App;\npublic class Z { public void A() { } public void B() { } }\n"),
            ("src/A.cs", "namespace App;\npublic class A { public void A() { } public void B() { } }\n"));

        IReadOnlyList<Violation> violations = Check(
            Rule(project.Count().MethodCount().ShouldBe(1)));

        Assert.Equal(
            new[] { "src/A.cs", "src/Z.cs" },
            violations.Select(static violation => ((MetricViolation)violation).File));
    }

    [Fact]
    public void Violations_are_reported_in_class_identifier_order()
    {
        const string source =
            "namespace App;\n" +
            "public class Zeta { public void A() { } public void B() { } }\n" +
            "public class Alpha { public void A() { } public void B() { } }\n";

        IReadOnlyList<Violation> violations = Check(
            Rule(Project("src/Classes.cs", source).Count().MethodCount().ShouldBe(1)));

        Assert.Equal(
            new[] { "App.Alpha", "App.Zeta" },
            violations.Select(static violation => ((MetricViolation)violation).Class));
    }

    [Fact]
    public void An_empty_file_selection_is_guarded()
    {
        IReadOnlyList<Violation> violations = Check(
            Rule(new Metrics(Graph()).Count().Classes().ShouldBe(1)));

        var empty = Assert.Single(violations);
        Assert.IsType<EmptyTestViolation>(empty);
        Assert.Equal("project metrics classes should be 1", Assert.IsType<EmptyTestViolation>(empty).RuleDescription);
    }

    [Fact]
    public void The_guard_names_the_selectors_that_left_the_selection_empty()
    {
        IReadOnlyList<Violation> violations = Check(
            Rule(new Metrics(Graph(Self("a.cs"))).WithName("Car.cs").Count().Classes().ShouldBe(1)));

        var empty = Assert.Single(violations);
        Assert.Equal(
            "project metrics with name 'Car.cs' classes should be 1",
            Assert.IsType<EmptyTestViolation>(empty).RuleDescription);
    }

    [Fact]
    public void The_guard_names_the_metric_and_the_threshold_in_the_rules_own_words()
    {
        IReadOnlyList<Violation> violations = Check(
            Rule(new Metrics(Graph()).InFolder("src").Count().MethodCount().ShouldBeBelow(20)));

        var empty = Assert.Single(violations);
        Assert.Equal(
            "project metrics in folder 'src' method count should be below 20",
            Assert.IsType<EmptyTestViolation>(empty).RuleDescription);
    }

    [Fact]
    public void A_class_selector_that_leaves_no_class_is_guarded()
    {
        const string source = "namespace App;\npublic class Car { }\n";

        IReadOnlyList<Violation> violations = Check(
            Rule(Project("src/Car.cs", source)
                .ForClassesMatching("*.Controller")
                .Count()
                .MethodCount()
                .ShouldBe(1)));

        var empty = Assert.Single(violations);
        Assert.IsType<EmptyTestViolation>(empty);
        Assert.Equal(
            "project metrics for classes matching '*.Controller' method count should be 1",
            Assert.IsType<EmptyTestViolation>(empty).RuleDescription);
    }

    [Fact]
    public void A_class_selector_that_leaves_no_file_subject_is_guarded()
    {
        const string source = "namespace App;\npublic class Car { }\n";

        IReadOnlyList<Violation> violations = Check(
            Rule(Project("src/Car.cs", source)
                .ForClassesMatching("*.Controller")
                .Count()
                .Classes()
                .ShouldBe(1)));

        var empty = Assert.Single(violations);
        Assert.IsType<EmptyTestViolation>(empty);
        Assert.Equal(
            "project metrics for classes matching '*.Controller' classes should be 1",
            Assert.IsType<EmptyTestViolation>(empty).RuleDescription);
    }

    [Fact]
    public void The_guard_names_a_should_satisfy_rules_message()
    {
        IReadOnlyList<Violation> violations = Check(
            Rule(new Metrics(Graph()).Count().LinesOfCode().ShouldSatisfy(
                static value => value < 100,
                "every file is short")));

        var empty = Assert.Single(violations);
        Assert.Equal(
            "project metrics lines of code should satisfy 'every file is short'",
            Assert.IsType<EmptyTestViolation>(empty).RuleDescription);
    }

    [Fact]
    public void Allow_empty_tests_passes_an_empty_file_selection()
    {
        IReadOnlyList<Violation> violations = Check(
            Rule(new Metrics(Graph()).Count().Classes().ShouldBe(1)),
            options: new CheckOptions { AllowEmptyTests = true });

        Assert.Empty(violations);
    }

    [Fact]
    public void Allow_empty_tests_passes_a_class_selector_that_leaves_no_class()
    {
        const string source = "namespace App;\npublic class Car { }\n";

        IReadOnlyList<Violation> violations = Check(
            Rule(Project("src/Car.cs", source)
                .ForClassesMatching("*.Controller")
                .Count()
                .MethodCount()
                .ShouldBe(1)),
            options: new CheckOptions { AllowEmptyTests = true });

        Assert.Empty(violations);
    }

    [Fact]
    public void A_scope_without_a_source_provider_raises_a_user_error()
    {
        var rule = (MetricRule)new Metrics(Graph(Self("src/A.cs")))
            .Count()
            .LinesOfCode()
            .ShouldBeBelow(100);

        Assert.Throws<UserError>(() => MetricsAssertion.Check(rule, options: null));
    }

    [Fact]
    public void Check_rejects_a_null_rule()
    {
        Assert.Throws<ArgumentNullException>(() => MetricsAssertion.Check(null!, options: null));
    }

    private static Metrics Project(params (string Path, string Source)[] files)
    {
        var sources = files.ToDictionary(
            static file => file.Path,
            static file => file.Source,
            StringComparer.Ordinal);
        return new Metrics(Graph(files.Select(static file => Self(file.Path)).ToArray()), identifier => sources[identifier]);
    }

    private static Metrics Project(string path, string source) =>
        new(Graph(Self(path)), _ => source);

    private static IReadOnlyList<Violation> Check(MetricRule rule, CheckOptions? options = null) =>
        MetricsAssertion.Check(rule, options);

    private static MetricRule Rule(ICheckable checkable) => Assert.IsType<MetricRule>(checkable);

    private const string SmallCar =
        "namespace App;\n" +
        "public class Car\n" +
        "{\n" +
        "    public void Drive() { }\n" +
        "}\n";

    private static Graph Graph(params Edge[] edges) => new(edges);

    private static Edge Self(string file) => new(file, file, external: false, ImportKind.None);
}
