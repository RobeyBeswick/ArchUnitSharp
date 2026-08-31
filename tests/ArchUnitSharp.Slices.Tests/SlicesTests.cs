using ArchUnitSharp.Common.Extraction;

namespace ArchUnitSharp.Slices.Tests;

public class SlicesTests
{
    [Fact]
    public void A_full_rule_passes_through_the_fluent_chain()
    {
        var policy = new Slices(Graph(
                Self("src/features/billing/order.cs"),
                Self("src/features/billing/invoice.cs"),
                Self("src/features/auth/login.cs"),
                Self("src/legacy/Old.cs"),
                Using("src/features/billing/order.cs", "src/features/billing/invoice.cs")))
            .DefinedBy("src/features/(**)/*.cs")
            .ShouldNot()
            .ContainDependency("src/features/**", "src/legacy/**");

        Assert.Empty(policy.Check());
    }

    [Fact]
    public void A_full_rule_reports_a_forbidden_dependency_through_the_fluent_chain()
    {
        var policy = new Slices(Graph(
                Self("src/features/billing/order.cs"),
                Self("src/features/billing/invoice.cs"),
                Self("src/legacy/Old.cs"),
                Using("src/features/billing/order.cs", "src/legacy/Old.cs")))
            .DefinedBy("src/features/(**)/*.cs")
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
    public void DefinedByRegex_defines_slices_by_a_capture_group()
    {
        var policy = new Slices(Graph(
                Self("src/features/billing/order.cs"),
                Self("src/legacy/Old.cs"),
                Using("src/features/billing/order.cs", "src/legacy/Old.cs")))
            .DefinedByRegex("src/features/([a-z]+)/.*\\.cs")
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
    public void A_rule_can_be_checked_twice_and_reports_the_same_result()
    {
        var policy = new Slices(Graph(
                Self("src/features/billing/order.cs"),
                Self("src/legacy/Old.cs"),
                Using("src/features/billing/order.cs", "src/legacy/Old.cs")))
            .DefinedBy("src/features/(**)/*.cs")
            .ShouldNot()
            .ContainDependency("src/features/**", "src/legacy/**");

        IReadOnlyList<Violation> first = policy.Check();
        IReadOnlyList<Violation> second = policy.Check();

        Assert.Equal(first, second);
        Assert.NotSame(first, second);
    }

    [Fact]
    public void Two_branches_off_one_parent_do_not_see_each_others_definitions()
    {
        var parent = new Slices(Graph(Self("src/features/billing/order.cs")))
            .DefinedBy("src/features/(**)/*.cs");

        var withShared = parent.DefinedBy("src/shared/(**)/*.cs");
        var withLegacy = parent.DefinedBy("src/legacy/(**)/*.cs");

        Assert.Equal(new[] { "defined by 'src/features/(**)/*.cs'" }, Descriptions(parent));
        Assert.Equal(
            new[]
            {
                "defined by 'src/features/(**)/*.cs'",
                "defined by 'src/shared/(**)/*.cs'",
            },
            Descriptions(withShared));
        Assert.Equal(
            new[]
            {
                "defined by 'src/features/(**)/*.cs'",
                "defined by 'src/legacy/(**)/*.cs'",
            },
            Descriptions(withLegacy));
    }

    [Fact]
    public void Two_branches_off_one_parent_do_not_see_each_others_rules()
    {
        var parent = new Slices(Graph(Self("src/features/billing/order.cs")))
            .DefinedBy("src/features/(**)/*.cs");

        var blocked = parent.ShouldNot().ContainDependency("src/features/**", "src/legacy/**");
        var required = parent.Should().ContainDependency("src/features/**", "src/shared/**");

        Assert.Single(blocked.Rules);
        Assert.Single(required.Rules);
        Assert.Empty(parent.Rules);
    }

    [Fact]
    public void Definitions_return_a_fresh_copy_on_every_read()
    {
        var slices = new Slices(Graph(Self("src/features/billing/order.cs")))
            .DefinedBy("src/features/(**)/*.cs")
            .DefinedBy("src/shared/(**)/*.cs");

        IReadOnlyList<SliceDefinition> definitions = slices.Definitions;
        ((SliceDefinition[])definitions)[0] = null!;

        Assert.NotNull(slices.Definitions[0]);
    }

    [Fact]
    public void Rules_return_a_fresh_copy_on_every_read()
    {
        var slices = new Slices(Graph(Self("src/features/billing/order.cs")))
            .DefinedBy("src/features/(**)/*.cs")
            .ShouldNot()
            .ContainDependency("src/features/**", "src/legacy/**");

        IReadOnlyList<SliceRule> rules = slices.Rules;
        ((SliceRule[])rules)[0] = null!;

        Assert.NotNull(slices.Rules[0]);
    }

    [Fact]
    public void Two_branches_off_one_parent_do_not_see_each_others_diagram_rules()
    {
        var parent = new Slices(Graph(Self("src/features/billing/order.cs")))
            .DefinedBy("src/features/(**)/*.cs");

        var withBilling = parent.Should().AdhereToDiagram("component [billing]");
        var withAuth = parent.Should().AdhereToDiagram("component [auth]");

        Assert.Single(withBilling.DiagramRules);
        Assert.Single(withAuth.DiagramRules);
        Assert.Empty(parent.DiagramRules);
    }

    [Fact]
    public void DiagramRules_return_a_fresh_copy_on_every_read()
    {
        var slices = new Slices(Graph(Self("src/features/billing/order.cs")))
            .DefinedBy("src/features/(**)/*.cs")
            .Should()
            .AdhereToDiagram("component [billing]");

        IReadOnlyList<DiagramRule> rules = slices.DiagramRules;
        ((DiagramRule[])rules)[0] = null!;

        Assert.NotNull(slices.DiagramRules[0]);
    }

    [Fact]
    public void DefinedBy_rejects_a_null_glob()
    {
        Assert.Throws<ArgumentNullException>(() => new Slices(Graph(Self("a.cs"))).DefinedBy(null!));
    }

    [Fact]
    public void DefinedBy_rejects_an_empty_glob()
    {
        Assert.Throws<ArgumentException>(() => new Slices(Graph(Self("a.cs"))).DefinedBy(string.Empty));
    }

    [Fact]
    public void DefinedBy_rejects_a_glob_without_a_capture()
    {
        Assert.Throws<UserError>(() => new Slices(Graph(Self("a.cs"))).DefinedBy("src/features/**"));
    }

    [Fact]
    public void DefinedByRegex_rejects_a_null_pattern()
    {
        Assert.Throws<ArgumentNullException>(() => new Slices(Graph(Self("a.cs"))).DefinedByRegex(null!));
    }

    [Fact]
    public void DefinedByRegex_rejects_an_empty_pattern()
    {
        Assert.Throws<ArgumentException>(() => new Slices(Graph(Self("a.cs"))).DefinedByRegex(string.Empty));
    }

    [Fact]
    public void DefinedByRegex_rejects_a_pattern_without_a_capture()
    {
        Assert.Throws<UserError>(() => new Slices(Graph(Self("a.cs"))).DefinedByRegex("src/features/.*"));
    }

    [Fact]
    public void A_rule_rejects_a_null_from_glob()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new Slices(Graph(Self("a.cs"))).Should().ContainDependency(null!, "src/shared/**"));
    }

    [Fact]
    public void A_rule_rejects_an_empty_from_glob()
    {
        Assert.Throws<ArgumentException>(() =>
            new Slices(Graph(Self("a.cs"))).Should().ContainDependency(string.Empty, "src/shared/**"));
    }

    [Fact]
    public void A_rule_rejects_a_null_to_glob()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new Slices(Graph(Self("a.cs"))).Should().ContainDependency("src/features/**", null!));
    }

    [Fact]
    public void A_rule_rejects_an_empty_to_glob()
    {
        Assert.Throws<ArgumentException>(() =>
            new Slices(Graph(Self("a.cs"))).Should().ContainDependency("src/features/**", string.Empty));
    }

    private static string[] Descriptions(Slices slices) =>
        slices.Definitions.Select(static definition => definition.Description).ToArray();

    private static Graph Graph(params Edge[] edges) => new(edges);

    private static Edge Self(string file) => new(file, file, external: false, ImportKind.None);

    private static Edge Using(string source, string target) =>
        new(source, target, external: false, ImportKind.Using);
}
