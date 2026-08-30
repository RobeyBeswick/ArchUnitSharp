using ArchUnitSharp.Common.Extraction;
using ArchUnitSharp.Graph.Projection;
using ArchUnitSharp.Graph.Rendering;

namespace ArchUnitSharp.Graph.Tests;

public class GraphReportTests
{
    [Fact]
    public void To_methods_are_the_rendered_snapshot()
    {
        var graph = Graph(
            Self("src/App/Program.cs"),
            Self("src/Models/Car.cs"),
            Using("src/App/Program.cs", "src/Models/Car.cs"));
        var report = new GraphReport(graph);
        GraphSnapshot snapshot = GraphProjection.Build(graph, GraphQueryOptions.Default);

        Assert.Equal(DotRenderer.Render(snapshot), report.ToDot());
        Assert.Equal(MermaidRenderer.Render(snapshot), report.ToMermaid());
        Assert.Equal(D2Renderer.Render(snapshot), report.ToD2());
        Assert.Equal(CsvRenderer.Render(snapshot), report.ToCsv());
        Assert.Equal(JsonRenderer.Render(snapshot), report.ToJson());
        Assert.Equal(HtmlRenderer.Render(snapshot), report.ToHtml());
    }

    [Fact]
    public void All_six_formats_describe_the_same_nodes_and_edges()
    {
        var report = new GraphReport(Graph(
            Self("src/App/Program.cs"),
            Self("src/Models/Car.cs"),
            Using("src/App/Program.cs", "src/Models/Car.cs")));

        string dot = report.ToDot();
        string mermaid = report.ToMermaid();
        string d2 = report.ToD2();
        string csv = report.ToCsv();
        string json = report.ToJson();
        string html = report.ToHtml();

        foreach (string format in new[] { dot, mermaid, d2, csv, json, html })
        {
            Assert.Contains("src/App/Program.cs", format);
            Assert.Contains("src/Models/Car.cs", format);
        }

        Assert.Contains("\"src/App/Program.cs\" -> \"src/Models/Car.cs\"", dot);
        Assert.Contains("n0 --> n1", mermaid);
        Assert.Contains("\"src/App/Program.cs\" -> \"src/Models/Car.cs\"", d2);
        Assert.Contains("src/App/Program.cs,src/Models/Car.cs,false,Using", csv);
        Assert.Contains("\"source\": \"src/App/Program.cs\", \"target\": \"src/Models/Car.cs\"", json);
        Assert.Contains(">src/App/Program.cs</text>", html);
        Assert.Contains(">src/Models/Car.cs</text>", html);
    }

    [Fact]
    public void ExportAs_writes_the_same_text_the_to_method_returns_and_returns_the_path()
    {
        using var dir = new TempDir();
        var report = new GraphReport(Graph(
            Self("src/App/Program.cs"),
            Self("src/Models/Car.cs"),
            Using("src/App/Program.cs", "src/Models/Car.cs")));

        Assert.Equal(dir.File("graph.dot"), report.ExportAsDot(dir.File("graph.dot")));
        Assert.Equal(dir.File("graph.mmd"), report.ExportAsMermaid(dir.File("graph.mmd")));
        Assert.Equal(dir.File("graph.d2"), report.ExportAsD2(dir.File("graph.d2")));
        Assert.Equal(dir.File("graph.csv"), report.ExportAsCsv(dir.File("graph.csv")));
        Assert.Equal(dir.File("graph.json"), report.ExportAsJson(dir.File("graph.json")));
        Assert.Equal(dir.File("graph.html"), report.ExportAsHtml(dir.File("graph.html")));

        Assert.Equal(report.ToDot(), File.ReadAllText(dir.File("graph.dot")));
        Assert.Equal(report.ToMermaid(), File.ReadAllText(dir.File("graph.mmd")));
        Assert.Equal(report.ToD2(), File.ReadAllText(dir.File("graph.d2")));
        Assert.Equal(report.ToCsv(), File.ReadAllText(dir.File("graph.csv")));
        Assert.Equal(report.ToJson(), File.ReadAllText(dir.File("graph.json")));
        Assert.Equal(report.ToHtml(), File.ReadAllText(dir.File("graph.html")));
    }

    [Fact]
    public void Every_export_method_writes_its_format()
    {
        using var dir = new TempDir();
        var report = new GraphReport(Graph(Self("a.cs")));

        report.ExportAsDot(dir.File("graph.dot"));
        report.ExportAsMermaid(dir.File("graph.mmd"));
        report.ExportAsD2(dir.File("graph.d2"));
        report.ExportAsCsv(dir.File("graph.csv"));
        report.ExportAsJson(dir.File("graph.json"));
        report.ExportAsHtml(dir.File("graph.html"));

        Assert.StartsWith("digraph {", File.ReadAllText(dir.File("graph.dot")));
        Assert.StartsWith("flowchart LR", File.ReadAllText(dir.File("graph.mmd")));
        Assert.StartsWith("\"a.cs\"", File.ReadAllText(dir.File("graph.d2")));
        Assert.StartsWith("source,target", File.ReadAllText(dir.File("graph.csv")));
        Assert.StartsWith("{", File.ReadAllText(dir.File("graph.json")));
        Assert.StartsWith("<!DOCTYPE html>", File.ReadAllText(dir.File("graph.html")));
    }

    [Fact]
    public void Render_is_identical_every_call_and_between_reports_over_the_same_graph()
    {
        var graph = Graph(
            Self("src/App/Program.cs"),
            Self("src/Models/Car.cs"),
            Using("src/App/Program.cs", "src/Models/Car.cs"));
        var first = new GraphReport(graph);
        var second = new GraphReport(graph);

        Assert.Equal(first.ToDot(), first.ToDot());
        Assert.Equal(first.ToCsv(), second.ToCsv());
        Assert.Equal(first.ToHtml(), second.ToHtml());
    }

    [Fact]
    public void Build_is_deterministic_across_calls()
    {
        var report = new GraphReport(Graph(
            Self("a.cs"),
            Self("b.cs"),
            Using("a.cs", "b.cs")));

        Assert.Equal(report.Build(), report.Build());
    }

    [Fact]
    public void Including_external_dependencies_builds_a_snapshot_with_external_edges()
    {
        var report = new GraphReport(Graph(
            Self("a.cs"),
            External("a.cs", "System.Linq"))).IncludingExternalDependencies();

        var edge = Assert.Single(report.Build().Edges);
        Assert.Equal("System.Linq", edge.Target);
        Assert.True(edge.External);
    }

    [Fact]
    public void Including_self_dependencies_builds_a_snapshot_with_self_loops()
    {
        var report = new GraphReport(Graph(Self("a.cs"))).IncludingSelfDependencies();

        var edge = Assert.Single(report.Build().Edges);
        Assert.Equal("a.cs", edge.Source);
        Assert.Equal("a.cs", edge.Target);
    }

    [Fact]
    public void Focusing_on_narrows_the_snapshot_to_the_neighbourhood()
    {
        var graph = Graph(
            Self("A/a.cs"),
            Self("B/b.cs"),
            Self("C/c.cs"),
            Using("A/a.cs", "B/b.cs"),
            Using("B/b.cs", "C/c.cs"));
        var report = new GraphReport(graph).FocusingOn("A/a.cs", depth: 1);

        GraphSnapshot snapshot = report.Build();
        Assert.Equal(
            new[] { "A/a.cs", "B/b.cs" },
            snapshot.Nodes.Select(static n => n.Label));
        Assert.Equal(
            new[] { ("A/a.cs", "B/b.cs") },
            snapshot.Edges.Select(static e => (e.Source, e.Target)));
    }

    [Fact]
    public void Reachable_from_narrows_the_snapshot_to_the_outgoing_closure()
    {
        var graph = Graph(
            Self("A/a.cs"),
            Self("B/b.cs"),
            Self("C/c.cs"),
            Using("A/a.cs", "B/b.cs"),
            Using("B/b.cs", "C/c.cs"));
        var report = new GraphReport(graph).ReachableFrom("A/a.cs");

        Assert.Equal(
            new[] { "A/a.cs", "B/b.cs", "C/c.cs" },
            report.Build().Nodes.Select(static n => n.Label));
    }

    [Fact]
    public void Dependents_of_narrows_the_snapshot_to_the_incoming_closure()
    {
        var graph = Graph(
            Self("A/a.cs"),
            Self("B/b.cs"),
            Self("C/c.cs"),
            Using("A/a.cs", "C/c.cs"),
            Using("B/b.cs", "C/c.cs"));
        var report = new GraphReport(graph).DependentsOf("C/c.cs");

        Assert.Equal(
            new[] { "A/a.cs", "B/b.cs", "C/c.cs" },
            report.Build().Nodes.Select(static n => n.Label));
    }

    [Fact]
    public void Collapsed_to_folder_depth_changes_the_snapshot_labels()
    {
        var graph = Graph(
            Self("src/App/Program.cs"),
            Self("src/Models/Car.cs"),
            Using("src/App/Program.cs", "src/Models/Car.cs"));
        var report = new GraphReport(graph).CollapsedToFolderDepth(1);

        GraphSnapshot snapshot = report.Build();

        Assert.Equal(new[] { "src" }, snapshot.Nodes.Select(static n => n.Label));
        var edge = Assert.Single(snapshot.Edges);
        Assert.Equal("src", edge.Source);
        Assert.Equal("src", edge.Target);
    }

    [Fact]
    public void Collapsed_by_pattern_changes_the_snapshot_labels()
    {
        var report = new GraphReport(Graph(
            Self("src/Models/Car.cs"),
            Self("src/App/Program.cs"))).CollapsedByPattern("src/Models/**");

        Assert.Equal(
            new[] { "src/App/Program.cs", "src/Models/**" },
            report.Build().Nodes.Select(static n => n.Label));
    }

    [Fact]
    public void Titled_sets_the_snapshot_title()
    {
        var report = new GraphReport(Graph(Self("a.cs"))).Titled("The app");

        Assert.Equal("The app", report.Build().Title);
    }

    [Fact]
    public void Query_options_combine_without_losing_earlier_ones()
    {
        var report = new GraphReport(Graph(
            Self("a.cs"),
            External("a.cs", "System.Linq")))
            .IncludingExternalDependencies()
            .Titled("The app");

        GraphSnapshot snapshot = report.Build();

        Assert.Equal("The app", snapshot.Title);
        var edge = Assert.Single(snapshot.Edges);
        Assert.True(edge.External);
    }

    [Fact]
    public void Two_branches_off_one_parent_do_not_see_each_others_options()
    {
        var graph = Graph(
            Self("a.cs"),
            External("a.cs", "System.Linq"));
        GraphReport parent = new GraphReport(graph);
        GraphReport external = parent.IncludingExternalDependencies();
        GraphReport titled = parent.Titled("The app");

        Assert.Single(external.Build().Edges);
        Assert.Empty(titled.Build().Edges);
        Assert.Empty(parent.Build().Edges);
        Assert.Equal("The app", titled.Build().Title);
        Assert.Empty(external.Build().Title);
    }

    [Fact]
    public void Two_branches_off_one_parent_do_not_see_each_others_collapse_rules()
    {
        var graph = Graph(
            Self("A/Models/Car.cs"),
            Self("B/Service.cs"));
        GraphReport parent = new GraphReport(graph);
        GraphReport byFolder = parent.CollapsedToFolderDepth(1);
        GraphReport byPattern = parent.CollapsedByPattern("A/**");

        Assert.Equal(
            new[] { "A", "B" },
            byFolder.Build().Nodes.Select(static n => n.Label));
        Assert.Equal(
            new[] { "A/**", "B/Service.cs" },
            byPattern.Build().Nodes.Select(static n => n.Label));
        Assert.Equal(
            new[] { "A/Models/Car.cs", "B/Service.cs" },
            parent.Build().Nodes.Select(static n => n.Label));
    }

    [Fact]
    public void Check_passes_when_the_scope_matches_files()
    {
        var report = new GraphReport(Graph(Self("a.cs"), Self("b.cs")));

        Assert.Empty(report.Check());
    }

    [Fact]
    public void Check_reports_an_empty_test_violation_for_an_empty_graph()
    {
        var report = new GraphReport(Graph());

        Assert.Equal(
            new Violation[] { new EmptyTestViolation("project graph") },
            report.Check());
    }

    [Fact]
    public void Check_reports_an_empty_test_violation_when_a_restriction_matches_nothing()
    {
        var report = new GraphReport(Graph(Self("a.cs"))).ReachableFrom("No/Such/File.cs");

        Assert.Equal(
            new Violation[] { new EmptyTestViolation("project graph reachable from 'No/Such/File.cs'") },
            report.Check());
    }

    [Fact]
    public void WithCheckOptions_allows_an_empty_scope_to_pass()
    {
        var report = new GraphReport(Graph(Self("a.cs")))
            .ReachableFrom("No/Such/File.cs")
            .WithCheckOptions(new CheckOptions { AllowEmptyTests = true });

        Assert.Empty(report.Check());
    }

    [Fact]
    public void Check_options_passed_to_check_override_the_queries_own()
    {
        var report = new GraphReport(Graph(Self("a.cs"))).ReachableFrom("No/Such/File.cs");

        Assert.Empty(report.Check(new CheckOptions { AllowEmptyTests = true }));
        Assert.Equal(
            new Violation[] { new EmptyTestViolation("project graph reachable from 'No/Such/File.cs'") },
            report.Check());
    }

    [Fact]
    public void GraphReport_rejects_a_null_graph()
    {
        Assert.Throws<ArgumentNullException>(() => new GraphReport(null!));
    }

    [Fact]
    public void FocusingOn_rejects_a_null_glob()
    {
        var report = new GraphReport(Graph(Self("a.cs")));

        Assert.Throws<ArgumentNullException>(() => report.FocusingOn(null!, depth: 0));
    }

    [Fact]
    public void FocusingOn_rejects_a_negative_depth()
    {
        var report = new GraphReport(Graph(Self("a.cs")));

        Assert.Throws<ArgumentOutOfRangeException>(() => report.FocusingOn("a.cs", depth: -1));
    }

    [Fact]
    public void ReachableFrom_rejects_a_null_glob()
    {
        var report = new GraphReport(Graph(Self("a.cs")));

        Assert.Throws<ArgumentNullException>(() => report.ReachableFrom(null!));
    }

    [Fact]
    public void DependentsOf_rejects_a_null_glob()
    {
        var report = new GraphReport(Graph(Self("a.cs")));

        Assert.Throws<ArgumentNullException>(() => report.DependentsOf(null!));
    }

    [Fact]
    public void CollapsedToFolderDepth_rejects_a_negative_depth()
    {
        var report = new GraphReport(Graph(Self("a.cs")));

        Assert.Throws<ArgumentOutOfRangeException>(() => report.CollapsedToFolderDepth(-1));
    }

    [Fact]
    public void CollapsedByPattern_rejects_a_null_glob()
    {
        var report = new GraphReport(Graph(Self("a.cs")));

        Assert.Throws<ArgumentNullException>(() => report.CollapsedByPattern(null!));
    }

    [Fact]
    public void Titled_rejects_a_null_title()
    {
        var report = new GraphReport(Graph(Self("a.cs")));

        Assert.Throws<ArgumentNullException>(() => report.Titled(null!));
    }

    [Fact]
    public void WithCheckOptions_rejects_null_options()
    {
        var report = new GraphReport(Graph(Self("a.cs")));

        Assert.Throws<ArgumentNullException>(() => report.WithCheckOptions(null!));
    }

    [Fact]
    public void ExportAs_rejects_a_null_path()
    {
        var report = new GraphReport(Graph(Self("a.cs")));

        Assert.Throws<ArgumentNullException>(() => report.ExportAsDot(null!));
        Assert.Throws<ArgumentNullException>(() => report.ExportAsMermaid(null!));
        Assert.Throws<ArgumentNullException>(() => report.ExportAsD2(null!));
        Assert.Throws<ArgumentNullException>(() => report.ExportAsCsv(null!));
        Assert.Throws<ArgumentNullException>(() => report.ExportAsJson(null!));
        Assert.Throws<ArgumentNullException>(() => report.ExportAsHtml(null!));
    }

    [Fact]
    public void ExportAs_rejects_an_empty_path()
    {
        var report = new GraphReport(Graph(Self("a.cs")));

        Assert.Throws<ArgumentException>(() => report.ExportAsDot(string.Empty));
        Assert.Throws<ArgumentException>(() => report.ExportAsMermaid(string.Empty));
        Assert.Throws<ArgumentException>(() => report.ExportAsD2(string.Empty));
        Assert.Throws<ArgumentException>(() => report.ExportAsCsv(string.Empty));
        Assert.Throws<ArgumentException>(() => report.ExportAsJson(string.Empty));
        Assert.Throws<ArgumentException>(() => report.ExportAsHtml(string.Empty));
    }

    [Fact]
    public void ExportAs_throws_a_technical_error_when_the_file_cannot_be_written()
    {
        using var dir = new TempDir();
        var report = new GraphReport(Graph(Self("a.cs")));

        string missing = Path.Combine(dir.Path, "no-such-directory", "graph.dot");

        Assert.Throws<TechnicalError>(() => report.ExportAsDot(missing));
    }

    private static ArchUnitSharp.Common.Extraction.Graph Graph(params Edge[] edges) => new(edges);

    private static Edge Self(string file) => new(file, file, external: false, ImportKind.None);

    private static Edge Using(string source, string target) =>
        new(source, target, external: false, ImportKind.Using);

    private static Edge External(string source, string module) =>
        new(source, module, external: true, ImportKind.Using);
}
