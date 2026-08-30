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
        GraphSnapshot snapshot = GraphProjection.Snapshot(graph);

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
    public void GraphReport_rejects_a_null_graph()
    {
        Assert.Throws<ArgumentNullException>(() => new GraphReport(null!));
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
}
