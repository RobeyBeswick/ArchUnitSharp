using ArchUnitSharp.Common.Extraction;
using ArchUnitSharp.Graph.Projection;
using ArchUnitSharp.Projection;

namespace ArchUnitSharp.Graph.Tests;

public class GraphProjectionTests
{
    [Fact]
    public void Snapshot_captures_every_file_as_a_node()
    {
        GraphSnapshot snapshot = GraphProjection.Snapshot(Graph(
            Self("src/App/Program.cs"),
            Self("src/Models/Car.cs")));

        Assert.Equal(new[] { "src/App/Program.cs", "src/Models/Car.cs" }, snapshot.Nodes.Select(static n => n.Label));
    }

    [Fact]
    public void Snapshot_captures_dependencies_between_distinct_files_only()
    {
        GraphSnapshot snapshot = GraphProjection.Snapshot(Graph(
            Self("src/App/Program.cs"),
            Self("src/Models/Car.cs"),
            Using("src/App/Program.cs", "src/Models/Car.cs")));

        IReadOnlyList<ProjectedEdge> edges = snapshot.Edges;

        Assert.Equal(new[] { ("src/App/Program.cs", "src/Models/Car.cs") }, edges.Select(static e => (e.Source, e.Target)));
    }

    [Fact]
    public void Snapshot_keeps_external_dependencies()
    {
        GraphSnapshot snapshot = GraphProjection.Snapshot(Graph(
            Self("src/App/Program.cs"),
            External("src/App/Program.cs", "System.Linq")));

        var edge = Assert.Single(snapshot.Edges);
        Assert.Equal("src/App/Program.cs", edge.Source);
        Assert.Equal("System.Linq", edge.Target);
        Assert.True(edge.External);
    }

    [Fact]
    public void Snapshot_nodes_are_sorted_by_label()
    {
        GraphSnapshot snapshot = GraphProjection.Snapshot(Graph(
            Self("Z/z.cs"),
            Self("A/a.cs"),
            Self("M/m.cs")));

        Assert.Equal(new[] { "A/a.cs", "M/m.cs", "Z/z.cs" }, snapshot.Nodes.Select(static n => n.Label));
    }

    [Fact]
    public void Snapshot_edges_are_sorted_by_source_then_target()
    {
        GraphSnapshot snapshot = GraphProjection.Snapshot(Graph(
            Self("B/b.cs"),
            Self("A/a.cs"),
            Self("D/d.cs"),
            Self("C/c.cs"),
            Using("B/b.cs", "D/d.cs"),
            Using("A/a.cs", "C/c.cs"),
            Using("A/a.cs", "D/d.cs")));

        Assert.Equal(
            new[] { ("A/a.cs", "C/c.cs"), ("A/a.cs", "D/d.cs"), ("B/b.cs", "D/d.cs") },
            snapshot.Edges.Select(static e => (e.Source, e.Target)));
    }

    [Fact]
    public void Snapshot_merges_parallel_edges_and_unions_their_import_kinds()
    {
        GraphSnapshot snapshot = GraphProjection.Snapshot(Graph(
            Self("A/a.cs"),
            Self("B/b.cs"),
            Using("A/a.cs", "B/b.cs"),
            new Edge("A/a.cs", "B/b.cs", external: false, ImportKind.UsingStatic)));

        var edge = Assert.Single(snapshot.Edges);
        Assert.Equal(ImportKind.Using | ImportKind.UsingStatic, edge.ImportKinds);
    }

    [Fact]
    public void Snapshot_nodes_return_a_fresh_copy_on_every_read()
    {
        GraphSnapshot snapshot = GraphProjection.Snapshot(Graph(
            Self("A/a.cs"),
            Self("B/b.cs")));

        ((ProjectedNode[])snapshot.Nodes)[0] = null!;

        Assert.Equal(new[] { "A/a.cs", "B/b.cs" }, snapshot.Nodes.Select(static n => n.Label));
    }

    [Fact]
    public void Snapshot_edges_return_a_fresh_copy_on_every_read()
    {
        GraphSnapshot snapshot = GraphProjection.Snapshot(Graph(
            Self("A/a.cs"),
            Self("B/b.cs"),
            Using("A/a.cs", "B/b.cs")));

        ((ProjectedEdge[])snapshot.Edges)[0] = null!;

        Assert.Equal(new[] { ("A/a.cs", "B/b.cs") }, snapshot.Edges.Select(static e => (e.Source, e.Target)));
    }

    [Fact]
    public void Snapshot_is_identical_when_captured_twice_from_the_same_graph()
    {
        var graph = Graph(
            Self("A/a.cs"),
            Self("B/b.cs"),
            Using("A/a.cs", "B/b.cs"));

        Assert.Equal(
            GraphProjection.Snapshot(graph).Edges,
            GraphProjection.Snapshot(graph).Edges);
    }

    [Fact]
    public void Snapshot_rejects_a_null_graph()
    {
        Assert.Throws<ArgumentNullException>(() => GraphProjection.Snapshot(null!));
    }

    private static ArchUnitSharp.Common.Extraction.Graph Graph(params Edge[] edges) => new(edges);

    private static Edge Self(string file) => new(file, file, external: false, ImportKind.None);

    private static Edge Using(string source, string target) =>
        new(source, target, external: false, ImportKind.Using);

    private static Edge External(string source, string module) =>
        new(source, module, external: true, ImportKind.Using);
}
