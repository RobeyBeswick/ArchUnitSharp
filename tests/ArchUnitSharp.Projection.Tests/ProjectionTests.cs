using ArchUnitSharp.Common.Extraction;
using ArchUnitSharp.Projection;

namespace ArchUnitSharp.Projection.Tests;

public class ProjectionTests
{
    [Fact]
    public void Edges_project_dependencies_and_filter_self_edges()
    {
        var graph = Graph(
            Self("a.cs"),
            Self("b.cs"),
            Using("a.cs", "b.cs"));

        IReadOnlyList<ProjectedEdge> edges = Projection.Edges(graph, Identity());

        var expected = new ProjectedEdge("a.cs", "b.cs", external: false, ImportKind.Using, new[] { Using("a.cs", "b.cs") });
        Assert.Equal(new[] { expected }, edges);
    }

    [Fact]
    public void Edges_relabel_via_the_map_function()
    {
        var graph = Graph(
            Using("A/a.cs", "C/c.cs"),
            Using("B/b.cs", "D/d.cs"));

        IReadOnlyList<ProjectedEdge> edges = Projection.Edges(graph, SliceMap());

        Assert.Equal(
            new[]
            {
                new ProjectedEdge("A", "C", external: false, ImportKind.Using, new[] { Using("A/a.cs", "C/c.cs") }),
                new ProjectedEdge("B", "D", external: false, ImportKind.Using, new[] { Using("B/b.cs", "D/d.cs") }),
            },
            edges);
    }

    [Fact]
    public void Edges_drop_edges_the_map_rejects()
    {
        var graph = Graph(
            Using("A/a.cs", "C/c.cs"),
            Using("A/a.cs", "orphan.cs"),
            Using("orphan.cs", "C/c.cs"));

        IReadOnlyList<ProjectedEdge> edges = Projection.Edges(graph, SliceMap());

        var expected = new ProjectedEdge("A", "C", external: false, ImportKind.Using, new[] { Using("A/a.cs", "C/c.cs") });
        Assert.Equal(new[] { expected }, edges);
    }

    [Fact]
    public void Edges_merge_parallel_projected_edges_and_union_their_import_kinds()
    {
        var graph = Graph(
            Using("A/a.cs", "C/c.cs"),
            new Edge("A/b.cs", "C/d.cs", external: false, ImportKind.Using | ImportKind.UsingStatic));

        IReadOnlyList<ProjectedEdge> edges = Projection.Edges(graph, SliceMap());

        var merged = Assert.Single(edges);
        Assert.Equal("A", merged.Source);
        Assert.Equal("C", merged.Target);
        Assert.Equal(ImportKind.Using | ImportKind.UsingStatic, merged.ImportKinds);
        Assert.Equal(2, merged.Edges.Count);
    }

    [Fact]
    public void Edges_filter_relabelled_self_loops()
    {
        var graph = Graph(Using("A/a.cs", "A/b.cs"));

        IReadOnlyList<ProjectedEdge> edges = Projection.Edges(graph, SliceMap());

        Assert.Empty(edges);
    }

    [Fact]
    public void Merged_edge_is_external_only_when_every_parallel_edge_was_external()
    {
        var graph = Graph(
            Using("a.cs", "b.cs"),
            new Edge("a.cs", "ext.cs", external: true, ImportKind.UsingStatic));

        MapFunction map = static edge =>
            new ProjectedEdge("P", "Q", edge.External, edge.ImportKinds, new[] { edge });

        var merged = Assert.Single(Projection.Edges(graph, map));
        Assert.False(merged.External);
        Assert.Equal(ImportKind.Using | ImportKind.UsingStatic, merged.ImportKinds);
        Assert.Equal(2, merged.Edges.Count);
    }

    [Fact]
    public void Merged_edge_is_not_external_when_the_earlier_parallel_edge_was_external()
    {
        var graph = Graph(
            new Edge("a.cs", "ext.cs", external: true, ImportKind.UsingStatic),
            Using("b.cs", "c.cs"));

        MapFunction map = static edge =>
            new ProjectedEdge("P", "Q", edge.External, edge.ImportKinds, new[] { edge });

        var merged = Assert.Single(Projection.Edges(graph, map));
        Assert.False(merged.External);
    }

    [Fact]
    public void Merged_edge_is_external_when_every_parallel_edge_was_external()
    {
        var graph = Graph(
            new Edge("a.cs", "ext.cs", external: true, ImportKind.Using),
            new Edge("b.cs", "ext.cs", external: true, ImportKind.UsingStatic));

        MapFunction map = static edge =>
            new ProjectedEdge("P", "Q", edge.External, edge.ImportKinds, new[] { edge });

        var merged = Assert.Single(Projection.Edges(graph, map));
        Assert.True(merged.External);
    }

    [Fact]
    public void Edges_result_is_sorted_by_source_then_target()
    {
        var graph = Graph(
            Using("B/b.cs", "A/a.cs"),
            Using("A/a.cs", "C/c.cs"));

        IReadOnlyList<ProjectedEdge> edges = Projection.Edges(graph, Identity());

        Assert.Equal("A/a.cs", edges[0].Source);
        Assert.Equal("B/b.cs", edges[1].Source);
    }

    [Fact]
    public void Edges_with_a_null_graph_are_rejected()
    {
        Assert.Throws<ArgumentNullException>(() => Projection.Edges(null!, Identity()));
    }

    [Fact]
    public void Edges_with_a_null_map_are_rejected()
    {
        Assert.Throws<ArgumentNullException>(() => Projection.Edges(Graph(Using("a.cs", "b.cs")), null!));
    }

    [Fact]
    public void ToNodes_projects_each_file_to_a_node_carrying_its_self_edge()
    {
        var graph = Graph(
            Self("A/a.cs"),
            Self("A/b.cs"),
            Using("A/a.cs", "A/b.cs"));

        IReadOnlyList<ProjectedNode> nodes = Projection.ToNodes(graph, Identity());

        Assert.Equal(
            new[]
            {
                new ProjectedNode("A/a.cs", new[] { Self("A/a.cs") }),
                new ProjectedNode("A/b.cs", new[] { Self("A/b.cs") }),
            },
            nodes);
    }

    [Fact]
    public void ToNodes_groups_files_that_share_a_label()
    {
        var graph = Graph(
            Self("A/a.cs"),
            Self("A/b.cs"),
            Self("C/c.cs"));

        IReadOnlyList<ProjectedNode> nodes = Projection.ToNodes(graph, SliceMap());

        Assert.Equal(
            new[]
            {
                new ProjectedNode("A", new[] { Self("A/a.cs"), Self("A/b.cs") }),
                new ProjectedNode("C", new[] { Self("C/c.cs") }),
            },
            nodes);
    }

    [Fact]
    public void ToNodes_drops_files_the_map_rejects()
    {
        var graph = Graph(Self("A/a.cs"), Self("orphan.cs"));

        IReadOnlyList<ProjectedNode> nodes = Projection.ToNodes(graph, SliceMap());

        Assert.Equal(new[] { new ProjectedNode("A", new[] { Self("A/a.cs") }) }, nodes);
    }

    [Fact]
    public void ToNodes_is_sorted_by_label()
    {
        var graph = Graph(Self("C/c.cs"), Self("A/a.cs"));

        IReadOnlyList<ProjectedNode> nodes = Projection.ToNodes(graph, Identity());

        Assert.Equal(new[] { "A/a.cs", "C/c.cs" }, nodes.Select(static node => node.Label));
    }

    [Fact]
    public void ToNodes_of_an_empty_graph_yields_no_nodes()
    {
        IReadOnlyList<ProjectedNode> nodes = Projection.ToNodes(Graph(), Identity());

        Assert.Empty(nodes);
    }

    [Fact]
    public void ToNodes_with_a_null_graph_are_rejected()
    {
        Assert.Throws<ArgumentNullException>(() => Projection.ToNodes(null!, Identity()));
    }

    [Fact]
    public void ToNodes_with_a_null_map_are_rejected()
    {
        Assert.Throws<ArgumentNullException>(() => Projection.ToNodes(Graph(Self("A/a.cs")), null!));
    }

    [Fact]
    public void Cycles_report_each_projected_cycle_with_its_hops()
    {
        var graph = Graph(
            Using("A/a.cs", "C/c.cs"),
            Using("C/c.cs", "D/d.cs"),
            Using("D/d.cs", "A/a.cs"));

        IReadOnlyList<ProjectedCycle> cycles = Projection.Cycles(graph, SliceMap());

        var cycle = Assert.Single(cycles);
        Assert.Equal(3, cycle.Edges.Count);
        Assert.Equal("A", cycle.Edges[0].Source);
        Assert.Equal("C", cycle.Edges[0].Target);
        Assert.Equal("C", cycle.Edges[1].Source);
        Assert.Equal("D", cycle.Edges[1].Target);
        Assert.Equal("D", cycle.Edges[2].Source);
        Assert.Equal("A", cycle.Edges[2].Target);
    }

    [Fact]
    public void Cycles_hops_carry_the_raw_edges_behind_them()
    {
        var graph = Graph(
            Using("A/a.cs", "C/c.cs"),
            Using("C/c.cs", "D/d.cs"),
            Using("D/d.cs", "A/a.cs"));

        IReadOnlyList<ProjectedCycle> cycles = Projection.Cycles(graph, SliceMap());

        ProjectedCycle cycle = Assert.Single(cycles);
        Assert.Equal(new[] { Using("A/a.cs", "C/c.cs") }, cycle.Edges[0].Edges);
        Assert.Equal(new[] { Using("C/c.cs", "D/d.cs") }, cycle.Edges[1].Edges);
        Assert.Equal(new[] { Using("D/d.cs", "A/a.cs") }, cycle.Edges[2].Edges);
    }

    [Fact]
    public void Cycles_report_nothing_for_an_acyclic_graph()
    {
        var graph = Graph(
            Using("A/a.cs", "C/c.cs"),
            Using("C/c.cs", "D/d.cs"));

        IReadOnlyList<ProjectedCycle> cycles = Projection.Cycles(graph, SliceMap());

        Assert.Empty(cycles);
    }

    [Fact]
    public void Cycles_do_not_report_relabelled_self_loops()
    {
        var graph = Graph(Using("A/a.cs", "A/b.cs"), Using("A/b.cs", "A/a.cs"));

        IReadOnlyList<ProjectedCycle> cycles = Projection.Cycles(graph, SliceMap());

        Assert.Empty(cycles);
    }

    [Fact]
    public void Cycles_merge_parallel_edges_before_detection()
    {
        var graph = Graph(
            Using("A/a.cs", "C/c.cs"),
            Using("A/b.cs", "C/c.cs"),
            Using("C/c.cs", "D/d.cs"),
            Using("D/d.cs", "A/a.cs"));

        IReadOnlyList<ProjectedCycle> cycles = Projection.Cycles(graph, SliceMap());

        ProjectedCycle cycle = Assert.Single(cycles);
        Assert.Equal(3, cycle.Edges.Count);
        ProjectedEdge hop = Assert.Single(cycle.Edges, static edge => edge.Source == "A" && edge.Target == "C");
        Assert.Equal(2, hop.Edges.Count);
    }

    [Fact]
    public void Cycles_with_a_null_graph_are_rejected()
    {
        Assert.Throws<ArgumentNullException>(() => Projection.Cycles(null!, SliceMap()));
    }

    [Fact]
    public void Cycles_with_a_null_map_are_rejected()
    {
        Assert.Throws<ArgumentNullException>(() => Projection.Cycles(Graph(Using("a.cs", "b.cs")), null!));
    }

    private static Graph Graph(params Edge[] edges) => new(edges);

    private static Edge Self(string file) => new(file, file, external: false, ImportKind.None);

    private static Edge Using(string source, string target) =>
        new(source, target, external: false, ImportKind.Using);

    private static MapFunction Identity() =>
        static edge => new ProjectedEdge(edge.Source, edge.Target, edge.External, edge.ImportKinds, new[] { edge });

    private static MapFunction SliceMap() =>
        static edge =>
        {
            string? source = SliceOf(edge.Source);
            string? target = SliceOf(edge.Target);
            if (source is null || target is null)
            {
                return null;
            }

            return new ProjectedEdge(source, target, edge.External, edge.ImportKinds, new[] { edge });
        };

    private static string? SliceOf(string file)
    {
        int separator = file.IndexOf('/');
        return separator < 0 ? null : file.Substring(0, separator);
    }
}
