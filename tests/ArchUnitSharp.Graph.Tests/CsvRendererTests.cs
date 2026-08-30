using ArchUnitSharp.Common.Extraction;
using ArchUnitSharp.Graph.Projection;
using ArchUnitSharp.Graph.Rendering;

namespace ArchUnitSharp.Graph.Tests;

public class CsvRendererTests
{
    [Fact]
    public void Render_produces_a_header_and_one_row_per_edge()
    {
        string csv = CsvRenderer.Render(Snapshot(
            Self("src/App/Program.cs"),
            Self("src/Models/Car.cs"),
            Using("src/App/Program.cs", "src/Models/Car.cs")));

        Assert.Equal(
            "source,target,external,importKinds\n"
            + "src/App/Program.cs,src/Models/Car.cs,false,Using\n",
            csv);
    }

    [Fact]
    public void Render_marks_external_edges_and_unions_import_kinds()
    {
        string csv = CsvRenderer.Render(Snapshot(
            Self("a.cs"),
            Self("b.cs"),
            External("a.cs", "System.Linq"),
            new Edge("a.cs", "b.cs", external: false, ImportKind.Using),
            new Edge("a.cs", "b.cs", external: false, ImportKind.UsingStatic)));

        Assert.Equal(
            "source,target,external,importKinds\n"
            + "a.cs,System.Linq,true,Using\n"
            + "a.cs,b.cs,false,\"Using, UsingStatic\"\n",
            csv);
    }

    [Fact]
    public void Render_quotes_fields_that_contain_separators()
    {
        string csv = CsvRenderer.Render(Snapshot(
            Self("a,b.cs"),
            Self("a\"b.cs"),
            Using("a,b.cs", "a\"b.cs")));

        Assert.Equal(
            "source,target,external,importKinds\n"
            + "\"a,b.cs\",\"a\"\"b.cs\",false,Using\n",
            csv);
    }

    [Fact]
    public void Render_handles_an_empty_graph()
    {
        string csv = CsvRenderer.Render(Snapshot());

        Assert.Equal("source,target,external,importKinds\n", csv);
    }

    [Fact]
    public void Render_is_stable_across_calls()
    {
        GraphSnapshot snapshot = Snapshot(
            Self("A/a.cs"),
            Self("B/b.cs"),
            Using("A/a.cs", "B/b.cs"));

        Assert.Equal(CsvRenderer.Render(snapshot), CsvRenderer.Render(snapshot));
    }

    [Fact]
    public void Render_rejects_a_null_snapshot()
    {
        Assert.Throws<ArgumentNullException>(() => CsvRenderer.Render(null!));
    }

    private static GraphSnapshot Snapshot(params Edge[] edges) =>
        GraphProjection.Build(new ArchUnitSharp.Common.Extraction.Graph(edges), new GraphQueryOptions { IncludeExternalDependencies = true });

    private static Edge Self(string file) => new(file, file, external: false, ImportKind.None);

    private static Edge Using(string source, string target) =>
        new(source, target, external: false, ImportKind.Using);

    private static Edge External(string source, string module) =>
        new(source, module, external: true, ImportKind.Using);
}
