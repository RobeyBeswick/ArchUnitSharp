using ArchUnitSharp.Common.Extraction;
using ArchUnitSharp.Slices.Assertion;
using ArchUnitSharp.Slices.Uml;

namespace ArchUnitSharp.Slices.Tests;

public class SlicesDiagramAssertionTests
{
    [Fact]
    public void A_rule_reports_a_dependency_the_diagram_does_not_allow()
    {
        IReadOnlyList<Violation> violations = Adhere(Fixture(), IntendedDiagram());

        Assert.Equal(
            new Violation[] { new DiagramAdherenceViolation("billing", "System.Linq") },
            violations);
    }

    [Fact]
    public void A_rule_passes_when_every_dependency_is_allowed()
    {
        const string diagram = """
            component [billing]
            component [auth]
            component [shared]
            component [legacy]
            [billing] --> [shared]
            [auth] --> [legacy]
            [billing] --> [System.Linq]
            """;

        Assert.Empty(Adhere(Fixture(), diagram));
    }

    [Fact]
    public void A_rule_reports_one_violation_per_slice_pair()
    {
        var graph = Graph(
            Self("billing/order.cs"),
            Self("billing/invoice.cs"),
            Self("legacy/Old.cs"),
            Using("billing/order.cs", "legacy/Old.cs"),
            Using("billing/invoice.cs", "legacy/Old.cs"));

        IReadOnlyList<Violation> violations = Adhere(graph, "[billing] --> [shared]");

        Assert.Equal(
            new Violation[] { new DiagramAdherenceViolation("billing", "legacy") },
            violations);
    }

    [Fact]
    public void A_rule_reports_every_disallowed_slice_pair_not_just_the_first()
    {
        const string diagram = """
            component [billing]
            component [auth]
            component [shared]
            [billing] --> [shared]
            [auth] --> [shared]
            """;

        IReadOnlyList<Violation> violations = Adhere(Fixture(), diagram);

        Assert.Equal(
            new Violation[]
            {
                new DiagramAdherenceViolation("auth", "legacy"),
                new DiagramAdherenceViolation("billing", "System.Linq"),
            },
            violations);
    }

    [Fact]
    public void A_dependency_the_diagram_declares_but_the_code_lacks_is_not_a_violation()
    {
        var graph = Graph(
            Self("billing/order.cs"),
            Self("shared/Util.cs"));

        IReadOnlyList<Violation> violations = Adhere(
            graph,
            "[billing] --> [shared]\n[shared] --> [auth]");

        Assert.Empty(violations);
    }

    [Fact]
    public void Ignoring_external_slices_hides_external_dependencies_only()
    {
        IReadOnlyList<Violation> violations = Adhere(
            Fixture(),
            """
            component [billing]
            component [auth]
            component [shared]
            component [legacy]
            [billing] --> [shared]
            """,
            new DiagramAdherenceOptions { IgnoreExternalSlices = true });

        Assert.Equal(
            new Violation[] { new DiagramAdherenceViolation("auth", "legacy") },
            violations);
    }

    [Fact]
    public void A_slice_pair_merging_an_internal_and_an_external_edge_is_not_ignored_as_external()
    {
        var graph = Graph(
            Self("billing/order.cs"),
            Self("System.Linq/Enumerable.cs"),
            Using("billing/order.cs", "System.Linq/Enumerable.cs"),
            External("billing/order.cs", "System.Linq"));

        IReadOnlyList<Violation> violations = Adhere(
            graph,
            "[billing] --> [shared]",
            new DiagramAdherenceOptions { IgnoreExternalSlices = true });

        Assert.Equal(
            new Violation[] { new DiagramAdherenceViolation("billing", "System.Linq") },
            violations);
    }

    [Fact]
    public void Ignoring_orphan_slices_ignores_dependencies_the_diagram_does_not_declare()
    {
        var graph = Graph(
            Self("billing/order.cs"),
            Self("auth/login.cs"),
            Self("shared/Util.cs"),
            Self("legacy/Old.cs"),
            Using("billing/order.cs", "shared/Util.cs"),
            Using("auth/login.cs", "shared/Util.cs"),
            Using("auth/login.cs", "legacy/Old.cs"),
            External("billing/order.cs", "System.Linq"));

        const string diagram = """
            component [billing]
            component [auth]
            component [shared]
            [billing] --> [shared]
            """;

        IReadOnlyList<Violation> violations = Adhere(
            graph,
            diagram,
            new DiagramAdherenceOptions { IgnoreOrphanSlices = true });

        Assert.Equal(
            new Violation[] { new DiagramAdherenceViolation("auth", "shared") },
            violations);
    }

    [Fact]
    public void Without_ignoring_orphan_slices_an_undeclared_endpoint_is_a_violation()
    {
        var graph = Graph(
            Self("billing/order.cs"),
            Self("auth/login.cs"),
            Self("shared/Util.cs"),
            Using("billing/order.cs", "shared/Util.cs"),
            Using("auth/login.cs", "shared/Util.cs"));

        IReadOnlyList<Violation> violations = Adhere(graph, "[billing] --> [shared]");

        Assert.Equal(
            new Violation[] { new DiagramAdherenceViolation("auth", "shared") },
            violations);
    }

    [Fact]
    public void Ignoring_orphan_slices_ignores_an_edge_whose_source_is_undeclared()
    {
        var graph = Graph(
            Self("auth/login.cs"),
            Self("shared/Util.cs"),
            Using("auth/login.cs", "shared/Util.cs"));

        IReadOnlyList<Violation> violations = Adhere(
            graph,
            "component [shared]",
            new DiagramAdherenceOptions { IgnoreOrphanSlices = true });

        Assert.Empty(violations);
    }

    [Fact]
    public void Both_modifiers_together_still_ignore_an_edge_whose_source_is_undeclared()
    {
        var graph = Graph(
            Self("auth/login.cs"),
            Self("shared/Util.cs"),
            Self("billing/order.cs"),
            Using("auth/login.cs", "shared/Util.cs"),
            External("billing/order.cs", "System.Linq"));

        IReadOnlyList<Violation> violations = Adhere(
            graph,
            "component [shared]",
            new DiagramAdherenceOptions
            {
                IgnoreOrphanSlices = true,
                IgnoreExternalSlices = true,
            });

        Assert.Empty(violations);
    }

    [Fact]
    public void A_rule_whose_slicing_selects_no_files_is_an_empty_test()
    {
        var graph = Graph(Self("thing.cs"));

        IReadOnlyList<Violation> violations = Adhere(graph, IntendedDiagram());

        Assert.Equal(
            new Violation[]
            {
                new EmptyTestViolation(
                    "project slices defined by '(**)/*.cs' should adhere to diagram"),
            },
            violations);
    }

    [Fact]
    public void A_rule_whose_diagram_declares_nothing_is_an_empty_test()
    {
        IReadOnlyList<Violation> violations = Adhere(Fixture(), "@startuml\n@enduml");

        Assert.Equal(
            new Violation[]
            {
                new EmptyTestViolation(
                    "project slices defined by '(**)/*.cs' should adhere to diagram"),
            },
            violations);
    }

    [Fact]
    public void An_empty_test_honours_allow_empty_tests()
    {
        IReadOnlyList<Violation> violations = Adhere(
            Fixture(),
            "@startuml\n@enduml",
            options: null,
            checkOptions: new CheckOptions { AllowEmptyTests = true });

        Assert.Empty(violations);
    }

    [Fact]
    public void A_policy_checks_diagram_rules_alongside_dependency_rules()
    {
        var policy = new Slices(Fixture())
            .DefinedBy("(**)/*.cs")
            .Should()
            .AdhereToDiagram(IntendedDiagram())
            .ShouldNot()
            .ContainDependency("billing/**", "legacy/**");

        IReadOnlyList<Violation> violations = policy.Check();

        Assert.Equal(
            new Violation[] { new DiagramAdherenceViolation("billing", "System.Linq") },
            violations);
    }

    [Fact]
    public void CheckDiagramRule_rejects_a_null_slices()
    {
        var rule = new DiagramRule(
            Diagram("@startuml\n@enduml"),
            DiagramAdherenceOptions.Default,
            "adhere to diagram");

        Assert.Throws<ArgumentNullException>(() => SlicesAssertion.CheckDiagramRule(null!, rule, null));
    }

    [Fact]
    public void CheckDiagramRule_rejects_a_null_rule()
    {
        var slices = new Slices(Graph(Self("a.cs")));

        Assert.Throws<ArgumentNullException>(() => SlicesAssertion.CheckDiagramRule(slices, null!, null));
    }

    private static IReadOnlyList<Violation> Adhere(
        Graph graph,
        string diagram,
        DiagramAdherenceOptions? options = null,
        CheckOptions? checkOptions = null)
    {
        var policy = new Slices(graph).DefinedBy("(**)/*.cs");
        Should mood = policy.Should();
        if (options?.IgnoreOrphanSlices == true)
        {
            mood = mood.IgnoringOrphanSlices();
        }

        if (options?.IgnoreExternalSlices == true)
        {
            mood = mood.IgnoringExternalSlices();
        }

        return mood.AdhereToDiagram(diagram).Check(checkOptions);
    }

    private static PlantUmlDiagram Diagram(string text) => PlantUmlParser.Parse(text);

    private static string IntendedDiagram() =>
        """
        component [billing]
        component [auth]
        component [shared]
        component [legacy]
        [billing] --> [shared]
        [auth] --> [legacy]
        """;

    private static Graph Fixture() => Graph(
        Self("billing/order.cs"),
        Self("billing/invoice.cs"),
        Self("auth/login.cs"),
        Self("shared/Util.cs"),
        Self("shared/Helper.cs"),
        Self("legacy/Old.cs"),
        Using("billing/order.cs", "shared/Util.cs"),
        Using("billing/invoice.cs", "shared/Helper.cs"),
        Using("auth/login.cs", "legacy/Old.cs"),
        External("billing/order.cs", "System.Linq"));

    private static Graph Graph(params Edge[] edges) => new(edges);

    private static Edge Self(string file) => new(file, file, external: false, ImportKind.None);

    private static Edge Using(string source, string target) =>
        new(source, target, external: false, ImportKind.Using);

    private static Edge External(string source, string module) =>
        new(source, module, external: true, ImportKind.Using);
}
