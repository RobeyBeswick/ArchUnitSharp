using ArchUnitSharp.Common.Extraction;
using ArchUnitSharp.Projection;
using ArchUnitSharp.Slices.Projection;

namespace ArchUnitSharp.Slices.Tests;

public class SlicesDiagramProjectionTests
{
    [Fact]
    public void DiagramMap_projects_an_internal_edge_between_slices()
    {
        var graph = Graph(
            Self("billing/order.cs"),
            Self("shared/Util.cs"),
            Using("billing/order.cs", "shared/Util.cs"));

        IReadOnlyList<ProjectedEdge> edges = Projection(graph);

        Assert.Equal(new[] { ("billing", "shared") }, Labels(edges));
    }

    [Fact]
    public void DiagramMap_keeps_an_external_edge_with_the_module_name_as_the_target()
    {
        var graph = Graph(
            Self("billing/order.cs"),
            External("billing/order.cs", "System.Linq"));

        IReadOnlyList<ProjectedEdge> edges = Projection(graph);

        var edge = Assert.Single(edges);
        Assert.Equal("billing", edge.Source);
        Assert.Equal("System.Linq", edge.Target);
        Assert.True(edge.External);
    }

    [Fact]
    public void DiagramMap_drops_an_edge_whose_source_is_unsliced()
    {
        var graph = Graph(
            Self("Old.cs"),
            Self("shared/Util.cs"),
            Using("Old.cs", "shared/Util.cs"));

        Assert.Empty(Projection(graph));
    }

    [Fact]
    public void DiagramMap_drops_an_edge_whose_internal_target_is_unsliced()
    {
        var graph = Graph(
            Self("billing/order.cs"),
            Self("Old.cs"),
            Using("billing/order.cs", "Old.cs"));

        Assert.Empty(Projection(graph));
    }

    [Fact]
    public void DiagramMap_drops_self_edges()
    {
        var graph = Graph(
            Self("billing/order.cs"),
            Self("billing/invoice.cs"));

        Assert.Empty(Projection(graph));
    }

    [Fact]
    public void DiagramMap_merges_parallel_edges_between_the_same_slice_pair()
    {
        var graph = Graph(
            Self("billing/order.cs"),
            Self("billing/invoice.cs"),
            Self("legacy/Old.cs"),
            Using("billing/order.cs", "legacy/Old.cs"),
            Using("billing/invoice.cs", "legacy/Old.cs"));

        IReadOnlyList<ProjectedEdge> edges = Projection(graph);

        var edge = Assert.Single(edges);
        Assert.Equal("billing", edge.Source);
        Assert.Equal("legacy", edge.Target);
        Assert.Equal(2, edge.Edges.Count);
    }

    [Fact]
    public void DiagramMap_result_is_sorted_by_source_then_target()
    {
        var graph = Graph(
            Self("billing/order.cs"),
            Self("auth/login.cs"),
            Self("shared/Util.cs"),
            Self("legacy/Old.cs"),
            Using("billing/order.cs", "legacy/Old.cs"),
            Using("auth/login.cs", "shared/Util.cs"),
            Using("auth/login.cs", "legacy/Old.cs"));

        IReadOnlyList<ProjectedEdge> edges = Projection(graph);

        Assert.Equal(
            new[] { ("auth", "legacy"), ("auth", "shared"), ("billing", "legacy") },
            Labels(edges));
    }

    [Fact]
    public void DiagramMap_rejects_a_null_label_function()
    {
        Assert.Throws<ArgumentNullException>(() => SlicesProjection.DiagramMap(null!));
    }

    private static IReadOnlyList<ProjectedEdge> Projection(Graph graph) =>
        ArchUnitSharp.Projection.Projection.Edges(
            graph,
            SlicesProjection.DiagramMap(
                identifier => SlicesProjection.SliceOf(new[] { ByPattern("(**)/*.cs") }, identifier)));

    private static (string Source, string Target)[] Labels(IReadOnlyList<ProjectedEdge> edges) =>
        edges.Select(static edge => (edge.Source, edge.Target)).ToArray();

    private static SliceDefinition ByPattern(string glob) => SliceDefinition.ByPattern(glob);

    private static Graph Graph(params Edge[] edges) => new(edges);

    private static Edge Self(string file) => new(file, file, external: false, ImportKind.None);

    private static Edge Using(string source, string target) =>
        new(source, target, external: false, ImportKind.Using);

    private static Edge External(string source, string module) =>
        new(source, module, external: true, ImportKind.Using);
}
