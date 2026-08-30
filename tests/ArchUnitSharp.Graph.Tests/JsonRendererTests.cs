using ArchUnitSharp.Common.Extraction;
using ArchUnitSharp.Graph.Projection;
using ArchUnitSharp.Graph.Rendering;

namespace ArchUnitSharp.Graph.Tests;

public class JsonRendererTests
{
    [Fact]
    public void Render_produces_a_document_of_nodes_and_edges()
    {
        string json = JsonRenderer.Render(Snapshot(
            Self("src/App/Program.cs"),
            Self("src/Models/Car.cs"),
            Using("src/App/Program.cs", "src/Models/Car.cs")));

        Assert.Equal(
            "{\n"
            + "  \"nodes\": [\n"
            + "    { \"id\": \"src/App/Program.cs\" },\n"
            + "    { \"id\": \"src/Models/Car.cs\" }\n"
            + "  ],\n"
            + "  \"edges\": [\n"
            + "    { \"source\": \"src/App/Program.cs\", \"target\": \"src/Models/Car.cs\", \"external\": false, \"importKinds\": \"Using\" }\n"
            + "  ]\n"
            + "}\n",
            json);
    }

    [Fact]
    public void Render_includes_external_flags_and_import_kinds()
    {
        string json = JsonRenderer.Render(Snapshot(
            Self("a.cs"),
            Self("b.cs"),
            Using("a.cs", "b.cs"),
            External("a.cs", "System.Linq")));

        Assert.Equal(
            "{\n"
            + "  \"nodes\": [\n"
            + "    { \"id\": \"a.cs\" },\n"
            + "    { \"id\": \"b.cs\" }\n"
            + "  ],\n"
            + "  \"edges\": [\n"
            + "    { \"source\": \"a.cs\", \"target\": \"System.Linq\", \"external\": true, \"importKinds\": \"Using\" },\n"
            + "    { \"source\": \"a.cs\", \"target\": \"b.cs\", \"external\": false, \"importKinds\": \"Using\" }\n"
            + "  ]\n"
            + "}\n",
            json);
    }

    [Fact]
    public void Render_escapes_quotes_and_backslashes_in_identifiers()
    {
        string json = JsonRenderer.Render(Snapshot(Self("a\"b\\c.cs")));

        Assert.Contains("\"id\": \"a\\\"b\\\\c.cs\"", json);
    }

    [Fact]
    public void Render_handles_an_empty_graph()
    {
        string json = JsonRenderer.Render(Snapshot());

        Assert.Equal("{\n  \"nodes\": [],\n  \"edges\": []\n}\n", json);
    }

    [Fact]
    public void Render_is_stable_across_calls()
    {
        GraphSnapshot snapshot = Snapshot(
            Self("A/a.cs"),
            Self("B/b.cs"),
            Using("A/a.cs", "B/b.cs"));

        Assert.Equal(JsonRenderer.Render(snapshot), JsonRenderer.Render(snapshot));
    }

    [Fact]
    public void Render_rejects_a_null_snapshot()
    {
        Assert.Throws<ArgumentNullException>(() => JsonRenderer.Render(null!));
    }

    private static GraphSnapshot Snapshot(params Edge[] edges) =>
        GraphProjection.Build(new ArchUnitSharp.Common.Extraction.Graph(edges), new GraphQueryOptions { IncludeExternalDependencies = true });

    private static Edge Self(string file) => new(file, file, external: false, ImportKind.None);

    private static Edge Using(string source, string target) =>
        new(source, target, external: false, ImportKind.Using);

    private static Edge External(string source, string module) =>
        new(source, module, external: true, ImportKind.Using);
}
