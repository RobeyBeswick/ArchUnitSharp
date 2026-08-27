using ArchUnitSharp.Common.Extraction;
using ArchUnitSharp.Projection;

namespace ArchUnitSharp.Projection.Tests;

public class ProjectedEdgeTests
{
    private static Edge RawEdge(string source = "a.cs", string target = "b.cs", ImportKind kinds = ImportKind.Using) =>
        new(source, target, external: false, kinds);

    private static ProjectedEdge CreateProjectedEdge(string source = "A", string target = "B") =>
        new(source, target, external: false, ImportKind.Using, new[] { RawEdge() });

    [Fact]
    public void Constructor_stores_all_five_values()
    {
        var edge = new ProjectedEdge("Layers.UI", "Layers.Core", external: false, ImportKind.Using, new[] { RawEdge() });

        Assert.Equal("Layers.UI", edge.Source);
        Assert.Equal("Layers.Core", edge.Target);
        Assert.False(edge.External);
        Assert.Equal(ImportKind.Using, edge.ImportKinds);
        Assert.Equal(new[] { RawEdge() }, edge.Edges);
    }

    [Fact]
    public void Projected_edges_with_the_same_values_are_equal()
    {
        var left = CreateProjectedEdge();
        var right = new ProjectedEdge("A", "B", external: false, ImportKind.Using, new[] { RawEdge() });

        Assert.Equal(left, right);
        Assert.Equal(left.GetHashCode(), right.GetHashCode());
    }

    [Fact]
    public void Projected_edges_with_different_raw_edges_are_not_equal()
    {
        var left = CreateProjectedEdge();
        var right = new ProjectedEdge("A", "B", external: false, ImportKind.Using, new[] { RawEdge(target: "other.cs") });

        Assert.NotEqual(left, right);
    }

    [Fact]
    public void Changing_any_value_makes_edges_unequal()
    {
        var otherSource = new ProjectedEdge("Other", "B", external: false, ImportKind.Using, new[] { RawEdge() });
        var otherTarget = new ProjectedEdge("A", "Other", external: false, ImportKind.Using, new[] { RawEdge() });
        var otherExternal = new ProjectedEdge("A", "B", external: true, ImportKind.Using, new[] { RawEdge() });
        var otherKinds = new ProjectedEdge("A", "B", external: false, ImportKind.UsingStatic, new[] { RawEdge() });

        Assert.NotEqual(CreateProjectedEdge(), otherSource);
        Assert.NotEqual(CreateProjectedEdge(), otherTarget);
        Assert.NotEqual(CreateProjectedEdge(), otherExternal);
        Assert.NotEqual(CreateProjectedEdge(), otherKinds);
    }

    [Fact]
    public void With_branches_do_not_see_each_others_data()
    {
        var parent = CreateProjectedEdge();
        var branchToTarget = parent with { Target = "C" };
        var branchToSource = parent with { Source = "Z" };

        Assert.Equal("A", parent.Source);
        Assert.Equal("B", parent.Target);
        Assert.Equal("C", branchToTarget.Target);
        Assert.Equal("Z", branchToSource.Source);
    }

    [Fact]
    public void Input_edge_list_is_copied_on_construction()
    {
        var raw = new List<Edge> { RawEdge() };
        var projected = new ProjectedEdge("A", "B", external: false, ImportKind.Using, raw);

        raw.Add(RawEdge(target: "c.cs"));
        raw[0] = RawEdge(target: "evil.cs");

        Assert.Equal(new[] { RawEdge() }, projected.Edges);
    }

    [Fact]
    public void Every_read_returns_a_fresh_copy()
    {
        var projected = CreateProjectedEdge();

        Assert.NotSame(projected.Edges, projected.Edges);
    }

    [Fact]
    public void Mutating_a_returned_list_does_not_corrupt_the_edge()
    {
        var projected = CreateProjectedEdge();

        var returned = (Edge[])projected.Edges;
        returned[0] = RawEdge(target: "evil.cs");

        Assert.Equal(new[] { RawEdge() }, projected.Edges);
    }

    [Fact]
    public void Null_source_is_rejected()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ProjectedEdge(null!, "B", external: false, ImportKind.Using, new[] { RawEdge() }));
    }

    [Fact]
    public void Null_target_is_rejected()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ProjectedEdge("A", null!, external: false, ImportKind.Using, new[] { RawEdge() }));
    }

    [Fact]
    public void Null_edges_are_rejected()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ProjectedEdge("A", "B", external: false, ImportKind.Using, null!));
    }

    [Fact]
    public void Empty_source_is_rejected()
    {
        Assert.Throws<ArgumentException>(() =>
            new ProjectedEdge(string.Empty, "B", external: false, ImportKind.Using, new[] { RawEdge() }));
    }

    [Fact]
    public void Empty_target_is_rejected()
    {
        Assert.Throws<ArgumentException>(() =>
            new ProjectedEdge("A", string.Empty, external: false, ImportKind.Using, new[] { RawEdge() }));
    }

    [Fact]
    public void Empty_edges_are_rejected()
    {
        Assert.Throws<ArgumentException>(() =>
            new ProjectedEdge("A", "B", external: false, ImportKind.Using, Array.Empty<Edge>()));
    }

    [Fact]
    public void With_expression_cannot_introduce_a_bad_source()
    {
        var edge = CreateProjectedEdge();

        Assert.Throws<ArgumentException>(() => edge with { Source = string.Empty });
    }

    [Fact]
    public void With_expression_cannot_introduce_an_empty_edge_list()
    {
        var edge = CreateProjectedEdge();

        Assert.Throws<ArgumentException>(() => edge with { Edges = Array.Empty<Edge>() });
    }
}
