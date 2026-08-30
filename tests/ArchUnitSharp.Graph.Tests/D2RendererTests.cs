using ArchUnitSharp.Common.Extraction;
using ArchUnitSharp.Graph.Projection;
using ArchUnitSharp.Graph.Rendering;

namespace ArchUnitSharp.Graph.Tests;

public class D2RendererTests
{
    [Fact]
    public void Render_produces_a_diagram_of_nodes_and_edges()
    {
        string d2 = D2Renderer.Render(Snapshot(
            Self("src/App/Program.cs"),
            Self("src/Models/Car.cs"),
            Using("src/App/Program.cs", "src/Models/Car.cs")));

        Assert.Equal(
            "\"src/App/Program.cs\"\n"
            + "\"src/Models/Car.cs\"\n"
            + "\"src/App/Program.cs\" -> \"src/Models/Car.cs\"\n",
            d2);
    }

    [Fact]
    public void Render_declares_external_targets_as_nodes()
    {
        string d2 = D2Renderer.Render(Snapshot(
            Self("src/App/Program.cs"),
            Self("src/Models/Car.cs"),
            Using("src/App/Program.cs", "src/Models/Car.cs"),
            External("src/App/Program.cs", "System.Linq")));

        Assert.Equal(
            "\"src/App/Program.cs\"\n"
            + "\"src/Models/Car.cs\"\n"
            + "\"System.Linq\"\n"
            + "\"src/App/Program.cs\" -> \"System.Linq\"\n"
            + "\"src/App/Program.cs\" -> \"src/Models/Car.cs\"\n",
            d2);
    }

    [Fact]
    public void Render_escapes_quotes_and_backslashes_in_identifiers()
    {
        string d2 = D2Renderer.Render(Snapshot(Self("we\"ird\\path.cs")));

        Assert.Equal("\"we\\\"ird\\\\path.cs\"\n", d2);
    }

    [Fact]
    public void Render_handles_an_empty_graph()
    {
        string d2 = D2Renderer.Render(Snapshot());

        Assert.Equal(string.Empty, d2);
    }

    [Fact]
    public void Render_is_stable_across_calls()
    {
        GraphSnapshot snapshot = Snapshot(
            Self("A/a.cs"),
            Self("B/b.cs"),
            Using("A/a.cs", "B/b.cs"));

        Assert.Equal(D2Renderer.Render(snapshot), D2Renderer.Render(snapshot));
    }

    [Fact]
    public void Render_rejects_a_null_snapshot()
    {
        Assert.Throws<ArgumentNullException>(() => D2Renderer.Render(null!));
    }

    private static GraphSnapshot Snapshot(params Edge[] edges) =>
        GraphProjection.Snapshot(new ArchUnitSharp.Common.Extraction.Graph(edges));

    private static Edge Self(string file) => new(file, file, external: false, ImportKind.None);

    private static Edge Using(string source, string target) =>
        new(source, target, external: false, ImportKind.Using);

    private static Edge External(string source, string module) =>
        new(source, module, external: true, ImportKind.Using);
}
