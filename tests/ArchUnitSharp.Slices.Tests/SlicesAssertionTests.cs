using ArchUnitSharp.Common.Extraction;
using ArchUnitSharp.Slices.Assertion;

namespace ArchUnitSharp.Slices.Tests;

public class SlicesAssertionTests
{
    [Fact]
    public void A_negated_rule_reports_a_forbidden_dependency()
    {
        var policy = Features(Fixture())
            .ShouldNot()
            .ContainDependency("src/features/**", "src/legacy/**");

        IReadOnlyList<Violation> violations = policy.Check();

        Assert.Equal(
            new Violation[]
            {
                new ForbiddenDependencyViolation(
                    "billing",
                    "src/features/billing/order.cs",
                    "src/legacy/Old.cs"),
            },
            violations);
    }

    [Fact]
    public void A_negated_rule_passes_when_no_forbidden_dependency_exists()
    {
        var graph = Graph(
            Self("src/features/billing/order.cs"),
            Self("src/features/auth/login.cs"),
            Self("src/legacy/Old.cs"),
            Using("src/features/billing/order.cs", "src/features/auth/login.cs"));

        IReadOnlyList<Violation> violations = Features(graph)
            .ShouldNot()
            .ContainDependency("src/features/**", "src/legacy/**")
            .Check();

        Assert.Empty(violations);
    }

    [Fact]
    public void A_negated_rule_counts_a_dependency_whose_target_is_unsliced()
    {
        var graph = Graph(
            Self("src/features/billing/order.cs"),
            Self("src/legacy/Old.cs"),
            Using("src/features/billing/order.cs", "src/legacy/Old.cs"));

        IReadOnlyList<Violation> violations = Features(graph)
            .ShouldNot()
            .ContainDependency("src/features/**", "src/legacy/**")
            .Check();

        Assert.Equal(
            new Violation[]
            {
                new ForbiddenDependencyViolation(
                    "billing",
                    "src/features/billing/order.cs",
                    "src/legacy/Old.cs"),
            },
            violations);
    }

    [Fact]
    public void A_negated_rule_reports_one_violation_per_forbidden_dependency()
    {
        var graph = Graph(
            Self("src/features/billing/order.cs"),
            Self("src/features/billing/invoice.cs"),
            Self("src/legacy/Old.cs"),
            Using("src/features/billing/order.cs", "src/legacy/Old.cs"),
            Using("src/features/billing/invoice.cs", "src/legacy/Old.cs"));

        IReadOnlyList<Violation> violations = Features(graph)
            .ShouldNot()
            .ContainDependency("src/features/**", "src/legacy/**")
            .Check();

        Assert.Equal(
            new Violation[]
            {
                new ForbiddenDependencyViolation(
                    "billing",
                    "src/features/billing/invoice.cs",
                    "src/legacy/Old.cs"),
                new ForbiddenDependencyViolation(
                    "billing",
                    "src/features/billing/order.cs",
                    "src/legacy/Old.cs"),
            },
            violations);
    }

    [Fact]
    public void A_positive_rule_reports_a_slice_that_misses_the_dependency()
    {
        var policy = Features(Fixture())
            .Should()
            .ContainDependency("src/features/billing/**", "src/shared/**");

        IReadOnlyList<Violation> violations = policy.Check();

        Assert.Equal(
            new Violation[]
            {
                new MissingDependencyViolation("auth", "src/features/billing/**", "src/shared/**"),
            },
            violations);
    }

    [Fact]
    public void A_positive_rule_passes_when_every_slice_contains_the_dependency()
    {
        IReadOnlyList<Violation> violations = Features(Fixture())
            .Should()
            .ContainDependency("src/features/**", "src/shared/**")
            .Check();

        Assert.Empty(violations);
    }

    [Fact]
    public void A_positive_rule_reports_a_slice_whose_dependency_leaves_the_slicing()
    {
        var graph = Graph(
            Self("src/features/billing/order.cs"),
            Self("src/shared/Util.cs"),
            Using("src/features/billing/order.cs", "src/shared/Util.cs"));

        IReadOnlyList<Violation> violations = Features(graph)
            .Should()
            .ContainDependency("src/features/**", "src/shared/**")
            .Check();

        Assert.Empty(violations);
    }

    [Fact]
    public void A_rule_whose_slicing_selects_no_files_is_an_empty_test()
    {
        var graph = Graph(Self("src/legacy/Old.cs"));

        IReadOnlyList<Violation> violations = Features(graph)
            .ShouldNot()
            .ContainDependency("src/features/**", "src/legacy/**")
            .Check();

        Assert.Equal(
            new Violation[]
            {
                new EmptyTestViolation(
                    "project slices defined by 'src/features/(**)/*.cs' should not contain "
                    + "dependency from 'src/features/**' to 'src/legacy/**'"),
            },
            violations);
    }

    [Fact]
    public void A_rule_whose_from_glob_matches_no_sliced_file_is_an_empty_test()
    {
        IReadOnlyList<Violation> violations = Features(Fixture())
            .ShouldNot()
            .ContainDependency("src/other/**", "src/legacy/**")
            .Check();

        Assert.Equal(
            new Violation[]
            {
                new EmptyTestViolation(
                    "project slices defined by 'src/features/(**)/*.cs' should not contain "
                    + "dependency from 'src/other/**' to 'src/legacy/**'"),
            },
            violations);
    }

    [Fact]
    public void A_rule_whose_from_glob_matches_only_unsliced_files_is_an_empty_test()
    {
        var graph = Graph(
            Self("src/features/billing/order.cs"),
            Self("src/legacy/Old.cs"),
            Self("src/shared/Util.cs"),
            Using("src/legacy/Old.cs", "src/shared/Util.cs"));

        IReadOnlyList<Violation> violations = Features(graph)
            .ShouldNot()
            .ContainDependency("src/legacy/**", "src/shared/**")
            .Check();

        Assert.Equal(
            new Violation[]
            {
                new EmptyTestViolation(
                    "project slices defined by 'src/features/(**)/*.cs' should not contain "
                    + "dependency from 'src/legacy/**' to 'src/shared/**'"),
            },
            violations);
    }

    [Fact]
    public void A_rule_whose_to_glob_matches_no_file_is_an_empty_test()
    {
        IReadOnlyList<Violation> violations = Features(Fixture())
            .ShouldNot()
            .ContainDependency("src/features/**", "src/nonexistent/**")
            .Check();

        Assert.Equal(
            new Violation[]
            {
                new EmptyTestViolation(
                    "project slices defined by 'src/features/(**)/*.cs' should not contain "
                    + "dependency from 'src/features/**' to 'src/nonexistent/**'"),
            },
            violations);
    }

    [Fact]
    public void An_empty_test_honours_allow_empty_tests()
    {
        IReadOnlyList<Violation> violations = Features(Fixture())
            .ShouldNot()
            .ContainDependency("src/other/**", "src/legacy/**")
            .Check(new CheckOptions { AllowEmptyTests = true });

        Assert.Empty(violations);
    }

    [Fact]
    public void A_policy_with_no_rules_passes()
    {
        var policy = new Slices(Fixture()).DefinedBy("src/features/(**)/*.cs");

        Assert.Empty(policy.Check());
    }

    [Fact]
    public void A_policy_checks_every_rule()
    {
        var graph = Graph(
            Self("src/features/billing/order.cs"),
            Self("src/features/auth/login.cs"),
            Self("src/legacy/Old.cs"),
            Self("src/shared/Util.cs"),
            Using("src/features/billing/order.cs", "src/legacy/Old.cs"),
            Using("src/features/billing/order.cs", "src/shared/Util.cs"));

        IReadOnlyList<Violation> violations = Features(graph)
            .ShouldNot()
            .ContainDependency("src/features/**", "src/legacy/**")
            .Should()
            .ContainDependency("src/features/**", "src/shared/**")
            .Check();

        Assert.Equal(
            new Violation[]
            {
                new ForbiddenDependencyViolation(
                    "billing",
                    "src/features/billing/order.cs",
                    "src/legacy/Old.cs"),
                new MissingDependencyViolation("auth", "src/features/**", "src/shared/**"),
            },
            violations);
    }

    [Fact]
    public void CheckRule_rejects_a_null_slices()
    {
        var rule = new SliceRule("src/features/**", "src/legacy/**", negate: true);

        Assert.Throws<ArgumentNullException>(() => SlicesAssertion.CheckRule(null!, rule, null));
    }

    [Fact]
    public void CheckRule_rejects_a_null_rule()
    {
        var slices = new Slices(Graph(Self("a.cs")));

        Assert.Throws<ArgumentNullException>(() => SlicesAssertion.CheckRule(slices, null!, null));
    }

    [Fact]
    public void Check_rejects_a_null_slices()
    {
        Assert.Throws<ArgumentNullException>(() => SlicesAssertion.Check(null!, null));
    }

    private static Slices Features(Graph graph) =>
        new Slices(graph).DefinedBy("src/features/(**)/*.cs");

    private static Graph Fixture() => Graph(
        Self("src/features/billing/order.cs"),
        Self("src/features/billing/invoice.cs"),
        Self("src/features/auth/login.cs"),
        Self("src/legacy/Old.cs"),
        Self("src/shared/Util.cs"),
        Using("src/features/billing/order.cs", "src/legacy/Old.cs"),
        Using("src/features/billing/invoice.cs", "src/shared/Util.cs"),
        Using("src/features/auth/login.cs", "src/shared/Util.cs"),
        Using("src/features/auth/login.cs", "src/features/billing/order.cs"));

    private static Graph Graph(params Edge[] edges) => new(edges);

    private static Edge Self(string file) => new(file, file, external: false, ImportKind.None);

    private static Edge Using(string source, string target) =>
        new(source, target, external: false, ImportKind.Using);
}
