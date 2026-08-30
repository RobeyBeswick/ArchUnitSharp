using ArchUnitSharp.Common.Extraction;
using ArchUnitSharp.Graph.Projection;
using ArchUnitSharp.Graph.Rendering;

namespace ArchUnitSharp.Graph.Tests;

public class MermaidRendererTests
{
    [Fact]
    public void Render_produces_a_flowchart_of_nodes_and_edges()
    {
        string mermaid = MermaidRenderer.Render(Snapshot(
            Self("src/App/Program.cs"),
            Self("src/Models/Car.cs"),
            Using("src/App/Program.cs", "src/Models/Car.cs")));

        Assert.Equal(
            "flowchart LR\n"
            + "  n0[\"src/App/Program.cs\"]\n"
            + "  n1[\"src/Models/Car.cs\"]\n"
            + "  n0 --> n1\n",
            mermaid);
    }

    [Fact]
    public void Render_gives_external_targets_a_node_of_their_own()
    {
        string mermaid = MermaidRenderer.Render(Snapshot(
            Self("src/App/Program.cs"),
            Self("src/Models/Car.cs"),
            Using("src/App/Program.cs", "src/Models/Car.cs"),
            External("src/App/Program.cs", "System.Linq")));

        Assert.Equal(
            "flowchart LR\n"
            + "  n0[\"src/App/Program.cs\"]\n"
            + "  n1[\"src/Models/Car.cs\"]\n"
            + "  n2[\"System.Linq\"]\n"
            + "  n0 --> n2\n"
            + "  n0 --> n1\n",
            mermaid);
    }

    [Fact]
    public void Render_escapes_quotes_in_labels()
    {
        string mermaid = MermaidRenderer.Render(Snapshot(Self("a\"b.cs")));

        Assert.Equal("flowchart LR\n  n0[\"a#quot;b.cs\"]\n", mermaid);
    }

    [Fact]
    public void Render_handles_an_empty_graph()
    {
        string mermaid = MermaidRenderer.Render(Snapshot());

        Assert.Equal("flowchart LR\n", mermaid);
    }

    [Fact]
    public void Render_is_stable_across_calls()
    {
        GraphSnapshot snapshot = Snapshot(
            Self("A/a.cs"),
            Self("B/b.cs"),
            Using("A/a.cs", "B/b.cs"));

        Assert.Equal(MermaidRenderer.Render(snapshot), MermaidRenderer.Render(snapshot));
    }

    [Fact]
    public void Render_rejects_a_null_snapshot()
    {
        Assert.Throws<ArgumentNullException>(() => MermaidRenderer.Render(null!));
    }

    private static GraphSnapshot Snapshot(params Edge[] edges) =>
        GraphProjection.Build(new ArchUnitSharp.Common.Extraction.Graph(edges), new GraphQueryOptions { IncludeExternalDependencies = true });

    private static Edge Self(string file) => new(file, file, external: false, ImportKind.None);

    private static Edge Using(string source, string target) =>
        new(source, target, external: false, ImportKind.Using);

    private static Edge External(string source, string module) =>
        new(source, module, external: true, ImportKind.Using);
}
