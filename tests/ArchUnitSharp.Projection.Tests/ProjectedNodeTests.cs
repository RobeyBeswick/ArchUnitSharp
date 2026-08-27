using ArchUnitSharp.Common.Extraction;
using ArchUnitSharp.Projection;

namespace ArchUnitSharp.Projection.Tests;

public class ProjectedNodeTests
{
    private static Edge SelfEdge(string file = "src/a.cs") =>
        new(file, file, external: false, ImportKind.None);

    private static ProjectedNode CreateNode() =>
        new("Layers.UI", new[] { SelfEdge() });

    [Fact]
    public void Constructor_stores_label_and_raw_edges()
    {
        var node = new ProjectedNode("Slices.Admin", new[] { SelfEdge("src/admin/a.cs"), SelfEdge("src/admin/b.cs") });

        Assert.Equal("Slices.Admin", node.Label);
        Assert.Equal(new[] { SelfEdge("src/admin/a.cs"), SelfEdge("src/admin/b.cs") }, node.Edges);
    }

    [Fact]
    public void Nodes_with_the_same_values_are_equal()
    {
        var left = CreateNode();
        var right = new ProjectedNode("Layers.UI", new[] { SelfEdge() });

        Assert.Equal(left, right);
        Assert.Equal(left.GetHashCode(), right.GetHashCode());
    }

    [Fact]
    public void Nodes_with_different_raw_edges_are_not_equal()
    {
        var left = CreateNode();
        var right = new ProjectedNode("Layers.UI", new[] { SelfEdge("src/other.cs") });

        Assert.NotEqual(left, right);
    }

    [Fact]
    public void Changing_the_label_makes_nodes_unequal()
    {
        var other = CreateNode() with { Label = "Layers.Core" };

        Assert.NotEqual(CreateNode(), other);
    }

    [Fact]
    public void Input_edge_list_is_copied_on_construction()
    {
        var raw = new List<Edge> { SelfEdge() };
        var node = new ProjectedNode("Layers.UI", raw);

        raw.Add(SelfEdge("src/other.cs"));

        Assert.Equal(new[] { SelfEdge() }, node.Edges);
    }

    [Fact]
    public void Every_read_returns_a_fresh_copy()
    {
        var node = CreateNode();

        Assert.NotSame(node.Edges, node.Edges);
    }

    [Fact]
    public void Mutating_a_returned_list_does_not_corrupt_the_node()
    {
        var node = CreateNode();

        var returned = (Edge[])node.Edges;
        returned[0] = SelfEdge("src/evil.cs");

        Assert.Equal(new[] { SelfEdge() }, node.Edges);
    }

    [Fact]
    public void Null_label_is_rejected()
    {
        Assert.Throws<ArgumentNullException>(() => new ProjectedNode(null!, new[] { SelfEdge() }));
    }

    [Fact]
    public void Null_edges_are_rejected()
    {
        Assert.Throws<ArgumentNullException>(() => new ProjectedNode("Layers.UI", null!));
    }

    [Fact]
    public void Empty_label_is_rejected()
    {
        Assert.Throws<ArgumentException>(() => new ProjectedNode(string.Empty, new[] { SelfEdge() }));
    }

    [Fact]
    public void Empty_edges_are_rejected()
    {
        Assert.Throws<ArgumentException>(() => new ProjectedNode("Layers.UI", Array.Empty<Edge>()));
    }

    [Fact]
    public void With_expression_cannot_introduce_a_bad_label()
    {
        var node = CreateNode();

        Assert.Throws<ArgumentException>(() => node with { Label = string.Empty });
    }
}
