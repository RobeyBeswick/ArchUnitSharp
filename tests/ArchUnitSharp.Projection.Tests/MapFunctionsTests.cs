using ArchUnitSharp.Common.Extraction;
using ArchUnitSharp.Projection;

namespace ArchUnitSharp.Projection.Tests;

public class MapFunctionsTests
{
    private static Edge Self(string file) => new(file, file, external: false, ImportKind.None);

    private static Edge Internal(string source, string target) =>
        new(source, target, external: false, ImportKind.Using);

    private static Edge External(string source, string target) =>
        new(source, target, external: true, ImportKind.UsingStatic);

    private static Graph Graph(params Edge[] edges) => new(edges);

    private static ProjectedEdge[] Apply(MapFunction map, IEnumerable<Edge> edges) =>
        edges.Select(edge => map(edge)).OfType<ProjectedEdge>().ToArray();

    [Fact]
    public void Identity_keeps_every_edge_under_its_own_labels()
    {
        var self = Self("a.cs");
        var dependency = Internal("a.cs", "b.cs");
        var external = External("b.cs", "System.IO");

        ProjectedEdge[] projected = Apply(MapFunctions.Identity, new[] { self, dependency, external });

        Assert.Equal(
            new[]
            {
                new ProjectedEdge("a.cs", "a.cs", external: false, ImportKind.None, new[] { self }),
                new ProjectedEdge("a.cs", "b.cs", external: false, ImportKind.Using, new[] { dependency }),
                new ProjectedEdge("b.cs", "System.IO", external: true, ImportKind.UsingStatic, new[] { external }),
            },
            projected);
    }

    [Fact]
    public void PerEdge_drops_self_edges_but_keeps_every_dependency()
    {
        var self = Self("a.cs");
        var dependency = Internal("a.cs", "b.cs");
        var external = External("b.cs", "System.IO");

        Assert.Empty(Apply(MapFunctions.PerEdge, new[] { self }));
        Assert.Equal(
            new[]
            {
                new ProjectedEdge("a.cs", "b.cs", external: false, ImportKind.Using, new[] { dependency }),
                new ProjectedEdge("b.cs", "System.IO", external: true, ImportKind.UsingStatic, new[] { external }),
            },
            Apply(MapFunctions.PerEdge, new[] { dependency, external }));
    }

    [Fact]
    public void PerInternalEdge_keeps_only_internal_dependencies()
    {
        var self = Self("a.cs");
        var dependency = Internal("a.cs", "b.cs");
        var external = External("b.cs", "System.IO");

        Assert.Empty(Apply(MapFunctions.PerInternalEdge, new[] { self, external }));
        Assert.Equal(
            new[] { new ProjectedEdge("a.cs", "b.cs", external: false, ImportKind.Using, new[] { dependency }) },
            Apply(MapFunctions.PerInternalEdge, new[] { dependency }));
    }

    [Fact]
    public void PerExternalEdge_keeps_only_external_dependencies()
    {
        var self = Self("a.cs");
        var dependency = Internal("a.cs", "b.cs");
        var external = External("b.cs", "System.IO");

        Assert.Empty(Apply(MapFunctions.PerExternalEdge, new[] { self, dependency }));
        Assert.Equal(
            new[] { new ProjectedEdge("b.cs", "System.IO", external: true, ImportKind.UsingStatic, new[] { external }) },
            Apply(MapFunctions.PerExternalEdge, new[] { external }));
    }

    [Fact]
    public void Internal_and_external_views_together_are_exactly_the_per_edge_view()
    {
        Edge[] edges =
        {
            Self("a.cs"),
            Internal("a.cs", "b.cs"),
            Internal("b.cs", "c.cs"),
            External("c.cs", "System.IO"),
            External("b.cs", "System.Text"),
        };

        ProjectedEdge[] perEdge = Apply(MapFunctions.PerEdge, edges);
        ProjectedEdge[] internalEdges = Apply(MapFunctions.PerInternalEdge, edges);
        ProjectedEdge[] externalEdges = Apply(MapFunctions.PerExternalEdge, edges);

        Assert.Equal(perEdge, internalEdges.Concat(externalEdges));
    }

    [Fact]
    public void Identity_is_the_per_edge_view_plus_self_edges()
    {
        Edge[] edges =
        {
            Self("a.cs"),
            Self("b.cs"),
            Internal("a.cs", "b.cs"),
        };

        ProjectedEdge[] identity = Apply(MapFunctions.Identity, edges);
        ProjectedEdge[] perEdge = Apply(MapFunctions.PerEdge, edges);

        Assert.Equal(
            identity.Where(static edge => edge.Source != edge.Target),
            perEdge);
    }

    [Fact]
    public void Every_function_preserves_the_raw_edge_behind_the_projection()
    {
        var dependency = Internal("a.cs", "b.cs");
        var external = External("b.cs", "System.IO");
        var self = Self("a.cs");

        MapFunction[] functions = { MapFunctions.PerEdge, MapFunctions.PerInternalEdge, MapFunctions.PerExternalEdge, MapFunctions.Identity };
        Edge[] edges = { dependency, external, self };

        ProjectedEdge[] projected = functions.SelectMany(function => Apply(function, edges)).ToArray();

        Assert.All(projected, static edge => Assert.Single(edge.Edges));
    }

    [Fact]
    public void Each_member_returns_the_same_shared_instance_every_call()
    {
        Assert.Same(MapFunctions.PerEdge, MapFunctions.PerEdge);
        Assert.Same(MapFunctions.PerInternalEdge, MapFunctions.PerInternalEdge);
        Assert.Same(MapFunctions.PerExternalEdge, MapFunctions.PerExternalEdge);
        Assert.Same(MapFunctions.Identity, MapFunctions.Identity);
    }

    [Fact]
    public void PerEdge_drives_the_dependency_projection()
    {
        Graph graph = Graph(
            Self("a.cs"),
            Self("b.cs"),
            Internal("a.cs", "b.cs"),
            External("a.cs", "System.IO"));

        IReadOnlyList<ProjectedEdge> edges = Projection.Edges(graph, MapFunctions.PerEdge);

        Assert.Equal(
            new[]
            {
                new ProjectedEdge("a.cs", "System.IO", external: true, ImportKind.UsingStatic, new[] { External("a.cs", "System.IO") }),
                new ProjectedEdge("a.cs", "b.cs", external: false, ImportKind.Using, new[] { Internal("a.cs", "b.cs") }),
            },
            edges);
    }

    [Fact]
    public void PerInternalEdge_drives_the_cycle_projection()
    {
        Graph graph = Graph(
            Internal("A/a.cs", "B/b.cs"),
            Internal("B/b.cs", "A/c.cs"),
            Internal("A/c.cs", "A/a.cs"),
            External("A/a.cs", "System.IO"),
            External("System.IO", "System.IO"));

        IReadOnlyList<ProjectedCycle> cycles = Projection.Cycles(graph, MapFunctions.PerInternalEdge);

        ProjectedCycle cycle = Assert.Single(cycles);
        Assert.Equal(3, cycle.Edges.Count);
        Assert.All(cycle.Edges, static hop => Assert.False(hop.External));
    }

    [Fact]
    public void Identity_drives_node_projection()
    {
        Graph graph = Graph(
            Self("a.cs"),
            Self("b.cs"),
            Internal("a.cs", "b.cs"));

        IReadOnlyList<ProjectedNode> nodes = Projection.ToNodes(graph, MapFunctions.Identity);

        Assert.Equal(
            new[]
            {
                new ProjectedNode("a.cs", new[] { Self("a.cs") }),
                new ProjectedNode("b.cs", new[] { Self("b.cs") }),
            },
            nodes);
    }
}
