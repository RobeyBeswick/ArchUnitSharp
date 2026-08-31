using ArchUnitSharp.Common.Extraction;
using ArchUnitSharp.Metrics.Assertion;

namespace ArchUnitSharp.Metrics.Tests;

public class CustomMetricAssertionTests
{
    [Fact]
    public void A_custom_metric_passes_when_every_class_meets_the_threshold()
    {
        IReadOnlyList<Violation> violations = Check(
            Rule(Project("src/A.cs", SmallCar)
                .CustomMetric("member count", "classes stay focused", static info => info.Methods.Count)
                .ShouldBeBelow(100)));

        Assert.Empty(violations);
    }

    [Fact]
    public void A_custom_metric_flags_every_class_whose_value_misses_the_threshold()
    {
        const string source =
            "namespace App;\n" +
            "public class Car { public void A() { } public void B() { } }\n";

        IReadOnlyList<Violation> violations = Check(
            Rule(Project("src/A.cs", source)
                .CustomMetric("method count", "classes stay focused", static info => info.Methods.Count)
                .ShouldBeBelow(2)));

        Assert.Equal(
            new Violation[]
            {
                new CustomMetricViolation(
                    "src/A.cs",
                    "App.Car",
                    "method count",
                    "classes stay focused",
                    2,
                    MetricComparison.Below,
                    2),
            },
            violations);
    }

    [Fact]
    public void The_calculation_receives_the_full_class_info()
    {
        const string source = "namespace App;\npublic class Car { public void A() { } }\n";

        IReadOnlyList<Violation> violations = Check(
            Rule(Project("src/A.cs", source)
                .CustomMetric(
                    "car methods",
                    "d",
                    static info => info.Name.EndsWith("Car", StringComparison.Ordinal) ? info.Methods.Count : 0)
                .ShouldBe(1)));

        Assert.Empty(violations);
    }

    [Fact]
    public void ShouldSatisfy_receives_the_value_and_the_class_info()
    {
        const string source =
            "namespace App;\n" +
            "public class Car { public void A() { } public void B() { } }\n" +
            "public class Van { public void A() { } }\n";

        IReadOnlyList<Violation> violations = Check(
            Rule(Project("src/A.cs", source)
                .CustomMetric("method count", "d", static info => info.Methods.Count)
                .ShouldSatisfy(
                    static (value, info) =>
                        value >= 1 && !info.Name.EndsWith("Van", StringComparison.Ordinal),
                    "no vans")));

        Assert.Equal(
            new Violation[]
            {
                new CustomMetricViolation("src/A.cs", "App.Van", "method count", "d", 1, "no vans"),
            },
            violations);
    }

    [Fact]
    public void A_custom_metric_measures_each_class_separately()
    {
        const string source =
            "namespace App;\n" +
            "public class Small { public void A() { } }\n" +
            "public class Big { public void A() { } public void B() { } public void C() { } }\n";

        IReadOnlyList<Violation> violations = Check(
            Rule(Project("src/Classes.cs", source)
                .CustomMetric("method count", "d", static info => info.Methods.Count)
                .ShouldBe(1)));

        Assert.Equal(
            new Violation[]
            {
                new CustomMetricViolation("src/Classes.cs", "App.Big", "method count", "d", 3, MetricComparison.Equal, 1),
            },
            violations);
    }

    [Fact]
    public void ForClassesMatching_narrows_a_custom_metrics_subjects()
    {
        const string source =
            "namespace App;\n" +
            "public class CarController { public void A() { } public void B() { } }\n" +
            "public class Truck { public void A() { } public void B() { } public void C() { } }\n";

        IReadOnlyList<Violation> violations = Check(
            Rule(Project("src/Classes.cs", source)
                .ForClassesMatching("*Controller")
                .CustomMetric("method count", "d", static info => info.Methods.Count)
                .ShouldBeBelow(3)));

        Assert.Empty(violations);
    }

    [Fact]
    public void Violations_are_reported_in_class_identifier_order()
    {
        const string source =
            "namespace App;\n" +
            "public class Zeta { public void A() { } public void B() { } }\n" +
            "public class Alpha { public void A() { } public void B() { } }\n";

        IReadOnlyList<Violation> violations = Check(
            Rule(Project("src/Classes.cs", source)
                .CustomMetric("method count", "d", static info => info.Methods.Count)
                .ShouldBe(1)));

        Assert.Equal(
            new[] { "App.Alpha", "App.Zeta" },
            violations.Select(static violation => ((CustomMetricViolation)violation).Class));
    }

    [Fact]
    public void An_empty_file_selection_is_guarded()
    {
        IReadOnlyList<Violation> violations = Check(
            Rule(new Metrics(Graph())
                .CustomMetric("member count", "d", static info => info.Methods.Count)
                .ShouldBe(1)));

        var empty = Assert.Single(violations);
        Assert.IsType<EmptyTestViolation>(empty);
        Assert.Equal(
            "project metrics custom metric 'member count' should be 1",
            Assert.IsType<EmptyTestViolation>(empty).RuleDescription);
    }

    [Fact]
    public void The_guard_names_the_selectors_that_left_the_selection_empty()
    {
        IReadOnlyList<Violation> violations = Check(
            Rule(new Metrics(Graph(Self("a.cs")))
                .WithName("Car.cs")
                .CustomMetric("member count", "d", static info => info.Methods.Count)
                .ShouldBe(1)));

        var empty = Assert.Single(violations);
        Assert.Equal(
            "project metrics with name 'Car.cs' custom metric 'member count' should be 1",
            Assert.IsType<EmptyTestViolation>(empty).RuleDescription);
    }

    [Fact]
    public void The_guard_names_the_threshold_in_the_rules_own_words()
    {
        IReadOnlyList<Violation> violations = Check(
            Rule(new Metrics(Graph())
                .InFolder("src")
                .CustomMetric("member count", "d", static info => info.Methods.Count)
                .ShouldBeBelow(20)));

        var empty = Assert.Single(violations);
        Assert.Equal(
            "project metrics in folder 'src' custom metric 'member count' should be below 20",
            Assert.IsType<EmptyTestViolation>(empty).RuleDescription);
    }

    [Fact]
    public void A_class_selector_that_leaves_no_class_is_guarded()
    {
        const string source = "namespace App;\npublic class Car { }\n";

        IReadOnlyList<Violation> violations = Check(
            Rule(Project("src/Car.cs", source)
                .ForClassesMatching("*.Controller")
                .CustomMetric("member count", "d", static info => info.Methods.Count)
                .ShouldBe(1)));

        var empty = Assert.Single(violations);
        Assert.IsType<EmptyTestViolation>(empty);
        Assert.Equal(
            "project metrics for classes matching '*.Controller' custom metric 'member count' should be 1",
            Assert.IsType<EmptyTestViolation>(empty).RuleDescription);
    }

    [Fact]
    public void The_guard_names_a_should_satisfy_rules_message()
    {
        IReadOnlyList<Violation> violations = Check(
            Rule(new Metrics(Graph())
                .CustomMetric("member count", "d", static info => info.Methods.Count)
                .ShouldSatisfy(static (value, info) => value < 100, "focused classes")));

        var empty = Assert.Single(violations);
        Assert.Equal(
            "project metrics custom metric 'member count' should satisfy 'focused classes'",
            Assert.IsType<EmptyTestViolation>(empty).RuleDescription);
    }

    [Fact]
    public void Allow_empty_tests_passes_an_empty_file_selection()
    {
        IReadOnlyList<Violation> violations = Check(
            Rule(new Metrics(Graph())
                .CustomMetric("member count", "d", static info => info.Methods.Count)
                .ShouldBe(1)),
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
                .CustomMetric("member count", "d", static info => info.Methods.Count)
                .ShouldBe(1)),
            options: new CheckOptions { AllowEmptyTests = true });

        Assert.Empty(violations);
    }

    [Fact]
    public void A_scope_without_a_source_provider_raises_a_user_error()
    {
        var rule = (CustomMetricRule)new Metrics(Graph(Self("src/A.cs")))
            .CustomMetric("member count", "d", static info => info.Methods.Count)
            .ShouldBeBelow(100);

        Assert.Throws<UserError>(() => MetricsAssertion.Check(rule, options: null));
    }

    [Fact]
    public void Check_rejects_a_null_rule()
    {
        Assert.Throws<ArgumentNullException>(() =>
            MetricsAssertion.Check((CustomMetricRule)null!, options: null));
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

    private static IReadOnlyList<Violation> Check(CustomMetricRule rule, CheckOptions? options = null) =>
        MetricsAssertion.Check(rule, options);

    private static CustomMetricRule Rule(ICheckable checkable) => Assert.IsType<CustomMetricRule>(checkable);

    private const string SmallCar =
        "namespace App;\n" +
        "public class Car\n" +
        "{\n" +
        "    public void Drive() { }\n" +
        "}\n";

    private static Graph Graph(params Edge[] edges) => new(edges);

    private static Edge Self(string file) => new(file, file, external: false, ImportKind.None);
}
