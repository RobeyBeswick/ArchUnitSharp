using ArchUnitSharp.Common.Extraction;
using ArchUnitSharp.Graph.Projection;
using ArchUnitSharp.Graph.Rendering;

namespace ArchUnitSharp.Graph.Tests;

public class HtmlRendererTests
{
    private const string Head =
        "<!DOCTYPE html>\n"
        + "<html lang=\"en\">\n"
        + "<head>\n"
        + "<meta charset=\"utf-8\">\n"
        + "<title>Dependency graph</title>\n"
        + "<style>\n"
        + "body { margin: 2rem; font-family: sans-serif; }\n"
        + "h1 { font-size: 1.4rem; }\n"
        + "svg { border: 1px solid #ddd; }\n"
        + "path.edge { fill: none; stroke: #666; stroke-width: 1.5; }\n"
        + "path.edge.external { stroke: #c00; stroke-dasharray: 4 3; }\n"
        + ".node rect { fill: #eef; stroke: #446; stroke-width: 1.5; }\n"
        + ".node text { font: 12px sans-serif; fill: #222; }\n"
        + ".node.external rect { fill: #fdd; stroke: #c00; }\n"
        + "</style>\n"
        + "</head>\n"
        + "<body>\n"
        + "<h1>Dependency graph</h1>\n";

    [Fact]
    public void Render_produces_a_self_contained_page_with_the_graph_as_inline_svg()
    {
        string html = HtmlRenderer.Render(Snapshot(
            Self("src/App/Program.cs"),
            Self("src/Models/Car.cs"),
            Using("src/App/Program.cs", "src/Models/Car.cs")));

        Assert.Equal(
            Head
            + "<svg width=\"420\" height=\"70\" viewBox=\"0 0 420 70\" xmlns=\"http://www.w3.org/2000/svg\">\n"
            + "<defs>\n"
            + "<marker id=\"arrow\" markerWidth=\"9\" markerHeight=\"6\" refX=\"8\" refY=\"3\" orient=\"auto\">\n"
            + "<path d=\"M0,0 L9,3 L0,6 z\" fill=\"#666\"></path>\n"
            + "</marker>\n"
            + "</defs>\n"
            + "<path class=\"edge\" d=\"M180,35 C205,35 215,35 240,35\" marker-end=\"url(#arrow)\"></path>\n"
            + "<g class=\"node\" transform=\"translate(20,20)\">\n"
            + "<rect width=\"160\" height=\"30\"></rect>\n"
            + "<text x=\"80\" y=\"20\" text-anchor=\"middle\">src/App/Program.cs</text>\n"
            + "</g>\n"
            + "<g class=\"node\" transform=\"translate(240,20)\">\n"
            + "<rect width=\"160\" height=\"30\"></rect>\n"
            + "<text x=\"80\" y=\"20\" text-anchor=\"middle\">src/Models/Car.cs</text>\n"
            + "</g>\n"
            + "</svg>\n"
            + "<p>2 nodes, 1 edge</p>\n"
            + "</body>\n</html>\n",
            html);
    }

    [Fact]
    public void Render_styles_external_targets_as_distinct_nodes()
    {
        string html = HtmlRenderer.Render(Snapshot(
            Self("a.cs"),
            Self("b.cs"),
            Using("a.cs", "b.cs"),
            External("a.cs", "System.Linq")));

        Assert.Equal(
            Head
            + "<svg width=\"308\" height=\"114\" viewBox=\"0 0 308 114\" xmlns=\"http://www.w3.org/2000/svg\">\n"
            + "<defs>\n"
            + "<marker id=\"arrow\" markerWidth=\"9\" markerHeight=\"6\" refX=\"8\" refY=\"3\" orient=\"auto\">\n"
            + "<path d=\"M0,0 L9,3 L0,6 z\" fill=\"#666\"></path>\n"
            + "</marker>\n"
            + "</defs>\n"
            + "<path class=\"edge external\" d=\"M124,35 C149,35 159,79 184,79\" marker-end=\"url(#arrow)\"></path>\n"
            + "<path class=\"edge\" d=\"M124,35 C149,35 159,35 184,35\" marker-end=\"url(#arrow)\"></path>\n"
            + "<g class=\"node\" transform=\"translate(20,20)\">\n"
            + "<rect width=\"104\" height=\"30\"></rect>\n"
            + "<text x=\"52\" y=\"20\" text-anchor=\"middle\">a.cs</text>\n"
            + "</g>\n"
            + "<g class=\"node\" transform=\"translate(184,20)\">\n"
            + "<rect width=\"104\" height=\"30\"></rect>\n"
            + "<text x=\"52\" y=\"20\" text-anchor=\"middle\">b.cs</text>\n"
            + "</g>\n"
            + "<g class=\"node external\" transform=\"translate(184,64)\">\n"
            + "<rect width=\"104\" height=\"30\"></rect>\n"
            + "<text x=\"52\" y=\"20\" text-anchor=\"middle\">System.Linq</text>\n"
            + "</g>\n"
            + "</svg>\n"
            + "<p>3 nodes, 2 edges</p>\n"
            + "</body>\n</html>\n",
            html);
    }

    [Fact]
    public void Render_places_cycle_nodes_on_fresh_columns()
    {
        string html = HtmlRenderer.Render(Snapshot(
            Self("a.cs"),
            Self("b.cs"),
            Using("a.cs", "b.cs"),
            Using("b.cs", "a.cs")));

        Assert.Equal(
            Head
            + "<svg width=\"280\" height=\"70\" viewBox=\"0 0 280 70\" xmlns=\"http://www.w3.org/2000/svg\">\n"
            + "<defs>\n"
            + "<marker id=\"arrow\" markerWidth=\"9\" markerHeight=\"6\" refX=\"8\" refY=\"3\" orient=\"auto\">\n"
            + "<path d=\"M0,0 L9,3 L0,6 z\" fill=\"#666\"></path>\n"
            + "</marker>\n"
            + "</defs>\n"
            + "<path class=\"edge\" d=\"M110,35 C135,35 145,35 170,35\" marker-end=\"url(#arrow)\"></path>\n"
            + "<path class=\"edge\" d=\"M260,35 C285,35 -5,35 20,35\" marker-end=\"url(#arrow)\"></path>\n"
            + "<g class=\"node\" transform=\"translate(20,20)\">\n"
            + "<rect width=\"90\" height=\"30\"></rect>\n"
            + "<text x=\"45\" y=\"20\" text-anchor=\"middle\">a.cs</text>\n"
            + "</g>\n"
            + "<g class=\"node\" transform=\"translate(170,20)\">\n"
            + "<rect width=\"90\" height=\"30\"></rect>\n"
            + "<text x=\"45\" y=\"20\" text-anchor=\"middle\">b.cs</text>\n"
            + "</g>\n"
            + "</svg>\n"
            + "<p>2 nodes, 2 edges</p>\n"
            + "</body>\n</html>\n",
            html);
    }

    [Fact]
    public void Render_escapes_html_special_characters_in_labels()
    {
        string html = HtmlRenderer.Render(Snapshot(Self("a\"b&<>.cs")));

        Assert.Contains("<text x=\"45\" y=\"20\" text-anchor=\"middle\">a&quot;b&amp;&lt;&gt;.cs</text>", html);
    }

    [Fact]
    public void Render_handles_an_empty_graph()
    {
        string html = HtmlRenderer.Render(Snapshot());

        Assert.Equal(Head + "<p>The graph is empty.</p>\n</body>\n</html>\n", html);
    }

    [Fact]
    public void Render_is_stable_across_calls()
    {
        GraphSnapshot snapshot = Snapshot(
            Self("A/a.cs"),
            Self("B/b.cs"),
            Using("A/a.cs", "B/b.cs"));

        Assert.Equal(HtmlRenderer.Render(snapshot), HtmlRenderer.Render(snapshot));
    }

    [Fact]
    public void Render_rejects_a_null_snapshot()
    {
        Assert.Throws<ArgumentNullException>(() => HtmlRenderer.Render(null!));
    }

    private static GraphSnapshot Snapshot(params Edge[] edges) =>
        GraphProjection.Build(new ArchUnitSharp.Common.Extraction.Graph(edges), new GraphQueryOptions { IncludeExternalDependencies = true });

    private static Edge Self(string file) => new(file, file, external: false, ImportKind.None);

    private static Edge Using(string source, string target) =>
        new(source, target, external: false, ImportKind.Using);

    private static Edge External(string source, string module) =>
        new(source, module, external: true, ImportKind.Using);
}
