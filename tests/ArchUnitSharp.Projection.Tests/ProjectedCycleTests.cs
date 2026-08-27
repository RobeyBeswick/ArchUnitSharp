using ArchUnitSharp.Common.Extraction;
using ArchUnitSharp.Projection;

namespace ArchUnitSharp.Projection.Tests;

public class ProjectedCycleTests
{
    private static ProjectedEdge Hop(string source, string target, string rawSource, string rawTarget) =>
        new(source, target, external: false, ImportKind.Using, new[] { new Edge(rawSource, rawTarget, external: false, ImportKind.Using) });

    private static ProjectedCycle CreateCycle() =>
        new(new[]
        {
            Hop("A", "B", "src/a.cs", "src/b.cs"),
            Hop("B", "A", "src/b.cs", "src/a.cs"),
        });

    [Fact]
    public void Constructor_stores_the_hops_in_order()
    {
        var cycle = CreateCycle();

        Assert.Equal(2, cycle.Edges.Count);
        Assert.Equal("A", cycle.Edges[0].Source);
        Assert.Equal("A", cycle.Edges[1].Target);
    }

    [Fact]
    public void Cycles_with_the_same_hops_are_equal()
    {
        var left = CreateCycle();
        var right = new ProjectedCycle(new[]
        {
            Hop("A", "B", "src/a.cs", "src/b.cs"),
            Hop("B", "A", "src/b.cs", "src/a.cs"),
        });

        Assert.Equal(left, right);
        Assert.Equal(left.GetHashCode(), right.GetHashCode());
    }

    [Fact]
    public void Cycles_with_different_hops_are_not_equal()
    {
        var left = CreateCycle();
        var right = new ProjectedCycle(new[] { Hop("A", "B", "src/a.cs", "src/b.cs"), Hop("B", "C", "src/b.cs", "src/c.cs") });

        Assert.NotEqual(left, right);
    }

    [Fact]
    public void Input_hops_are_copied_on_construction()
    {
        var hops = new List<ProjectedEdge>
        {
            Hop("A", "B", "src/a.cs", "src/b.cs"),
            Hop("B", "A", "src/b.cs", "src/a.cs"),
        };
        var cycle = new ProjectedCycle(hops);

        hops.Add(Hop("B", "C", "src/b.cs", "src/c.cs"));

        Assert.Equal(2, cycle.Edges.Count);
    }

    [Fact]
    public void Every_read_returns_a_fresh_copy()
    {
        var cycle = CreateCycle();

        Assert.NotSame(cycle.Edges, cycle.Edges);
    }

    [Fact]
    public void Mutating_a_returned_list_does_not_corrupt_the_cycle()
    {
        var cycle = CreateCycle();

        var returned = (ProjectedEdge[])cycle.Edges;
        returned[0] = Hop("X", "Z", "src/x.cs", "src/z.cs");

        Assert.Equal(2, cycle.Edges.Count);
        Assert.Equal(Hop("A", "B", "src/a.cs", "src/b.cs"), cycle.Edges[0]);
    }

    [Fact]
    public void Null_hops_are_rejected()
    {
        Assert.Throws<ArgumentNullException>(() => new ProjectedCycle(null!));
    }

    [Fact]
    public void Empty_hops_are_rejected()
    {
        Assert.Throws<ArgumentException>(() => new ProjectedCycle(Array.Empty<ProjectedEdge>()));
    }

    [Fact]
    public void A_single_self_loop_hop_is_rejected()
    {
        Assert.Throws<ArgumentException>(() =>
            new ProjectedCycle(new[] { Hop("A", "A", "src/a.cs", "src/a.cs") }));
    }
}
