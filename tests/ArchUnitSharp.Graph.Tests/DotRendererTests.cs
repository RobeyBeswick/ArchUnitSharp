using ArchUnitSharp.Common.Extraction;
using ArchUnitSharp.Graph.Projection;
using ArchUnitSharp.Graph.Rendering;

namespace ArchUnitSharp.Graph.Tests;

public class DotRendererTests
{
    [Fact]
    public void Render_produces_a_digraph_of_nodes_and_edges()
    {
        string dot = DotRenderer.Render(Snapshot(
            Self("src/App/Program.cs"),
            Self("src/Models/Car.cs"),
            Using("src/App/Program.cs", "src/Models/Car.cs")));

        Assert.Equal(
            "digraph {\n"
            + "  \"src/App/Program.cs\";\n"
            + "  \"src/Models/Car.cs\";\n"
            + "  \"src/App/Program.cs\" -> \"src/Models/Car.cs\";\n"
            + "}\n",
            dot);
    }

    [Fact]
    public void Render_reaches_external_targets_through_edges_without_declaring_them()
    {
        string dot = DotRenderer.Render(Snapshot(
            Self("src/App/Program.cs"),
            Self("src/Models/Car.cs"),
            Using("src/App/Program.cs", "src/Models/Car.cs"),
            External("src/App/Program.cs", "System.Linq")));

        Assert.Equal(
            "digraph {\n"
            + "  \"src/App/Program.cs\";\n"
            + "  \"src/Models/Car.cs\";\n"
            + "  \"src/App/Program.cs\" -> \"System.Linq\";\n"
            + "  \"src/App/Program.cs\" -> \"src/Models/Car.cs\";\n"
            + "}\n",
            dot);
    }

    [Fact]
    public void Render_escapes_quotes_and_backslashes_in_identifiers()
    {
        string dot = DotRenderer.Render(Snapshot(Self("we\"ird\\path.cs")));

        Assert.Equal("digraph {\n  \"we\\\"ird\\\\path.cs\";\n}\n", dot);
    }

    [Fact]
    public void Render_handles_an_empty_graph()
    {
        string dot = DotRenderer.Render(Snapshot());

        Assert.Equal("digraph {\n}\n", dot);
    }

    [Fact]
    public void Render_is_stable_across_calls()
    {
        GraphSnapshot snapshot = Snapshot(
            Self("A/a.cs"),
            Self("B/b.cs"),
            Using("A/a.cs", "B/b.cs"));

        Assert.Equal(DotRenderer.Render(snapshot), DotRenderer.Render(snapshot));
    }

    [Fact]
    public void Render_rejects_a_null_snapshot()
    {
        Assert.Throws<ArgumentNullException>(() => DotRenderer.Render(null!));
    }

    private static GraphSnapshot Snapshot(params Edge[] edges) =>
        GraphProjection.Snapshot(new ArchUnitSharp.Common.Extraction.Graph(edges));

    private static Edge Self(string file) => new(file, file, external: false, ImportKind.None);

    private static Edge Using(string source, string target) =>
        new(source, target, external: false, ImportKind.Using);

    private static Edge External(string source, string module) =>
        new(source, module, external: true, ImportKind.Using);
}
