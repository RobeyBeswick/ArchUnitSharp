using ArchUnitSharp.Common.Extraction;

namespace ArchUnitSharp.Common.Tests.Extraction;

public class GraphTests
{
    private static Edge CreateEdge(string source = "a.cs", string target = "b.cs") =>
        new(source, target, external: false, ImportKind.Using);

    [Fact]
    public void Constructor_stores_edges_in_supplied_order()
    {
        var first = CreateEdge("a.cs", "b.cs");
        var second = CreateEdge("b.cs", "c.cs");
        var graph = new Graph(new[] { first, second });

        Assert.Equal(new[] { first, second }, graph.Edges);
    }

    [Fact]
    public void Null_edge_sequence_is_rejected()
    {
        Assert.Throws<ArgumentNullException>(() => new Graph(null!));
    }

    [Fact]
    public void Empty_graph_is_allowed()
    {
        var graph = new Graph(Array.Empty<Edge>());

        Assert.Empty(graph.Edges);
    }

    [Fact]
    public void Input_sequence_is_copied_on_construction()
    {
        var edges = new List<Edge> { CreateEdge("a.cs", "b.cs") };
        var graph = new Graph(edges);

        edges.Add(CreateEdge("b.cs", "c.cs"));
        edges[0] = CreateEdge("a.cs", "evil.cs");

        Assert.Equal(new[] { CreateEdge("a.cs", "b.cs") }, graph.Edges);
    }

    [Fact]
    public void Every_read_returns_a_fresh_copy()
    {
        var graph = new Graph(new[] { CreateEdge() });

        var firstRead = graph.Edges;
        var secondRead = graph.Edges;

        Assert.NotSame(firstRead, secondRead);
    }

    [Fact]
    public void Mutating_a_returned_list_does_not_corrupt_the_graph()
    {
        var graph = new Graph(new[] { CreateEdge() });

        var returned = (Edge[])graph.Edges;
        returned[0] = CreateEdge("a.cs", "evil.cs");

        Assert.Equal(new[] { CreateEdge("a.cs", "b.cs") }, graph.Edges);
    }
}
