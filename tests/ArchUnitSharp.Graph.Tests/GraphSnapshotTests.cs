using ArchUnitSharp.Common.Extraction;

namespace ArchUnitSharp.Graph.Tests;

public class GraphSnapshotTests
{
    [Fact]
    public void Two_snapshots_with_equal_contents_are_equal()
    {
        var left = new GraphSnapshot(
            "title",
            new[] { new SnapshotNode("A/a.cs", new[] { "A/a.cs" }) },
            new[] { new SnapshotEdge("A/a.cs", "B/b.cs", count: 1, external: false, ImportKind.Using) });
        var right = new GraphSnapshot(
            "title",
            new[] { new SnapshotNode("A/a.cs", new[] { "A/a.cs" }) },
            new[] { new SnapshotEdge("A/a.cs", "B/b.cs", count: 1, external: false, ImportKind.Using) });

        Assert.Equal(left, right);
        Assert.Equal(left.GetHashCode(), right.GetHashCode());
    }

    [Fact]
    public void Two_snapshots_with_different_titles_are_unequal()
    {
        var left = new GraphSnapshot("one", Array.Empty<SnapshotNode>(), Array.Empty<SnapshotEdge>());
        var right = new GraphSnapshot("two", Array.Empty<SnapshotNode>(), Array.Empty<SnapshotEdge>());

        Assert.NotEqual(left, right);
    }

    [Fact]
    public void Two_snapshots_with_different_nodes_or_edges_are_unequal()
    {
        var left = new GraphSnapshot(
            "title",
            new[] { new SnapshotNode("A/a.cs", new[] { "A/a.cs" }) },
            Array.Empty<SnapshotEdge>());
        var right = new GraphSnapshot(
            "title",
            new[] { new SnapshotNode("B/b.cs", new[] { "B/b.cs" }) },
            Array.Empty<SnapshotEdge>());

        Assert.NotEqual(left, right);
    }

    [Fact]
    public void Two_snapshots_with_different_edges_are_unequal()
    {
        var left = new GraphSnapshot(
            "title",
            new[] { new SnapshotNode("A/a.cs", new[] { "A/a.cs" }) },
            new[] { new SnapshotEdge("A/a.cs", "B/b.cs", count: 1, external: false, ImportKind.Using) });
        var right = new GraphSnapshot(
            "title",
            new[] { new SnapshotNode("A/a.cs", new[] { "A/a.cs" }) },
            new[] { new SnapshotEdge("A/a.cs", "C/c.cs", count: 1, external: false, ImportKind.Using) });

        Assert.NotEqual(left, right);
    }

    [Fact]
    public void Nodes_return_a_fresh_copy_on_every_read()
    {
        var snapshot = new GraphSnapshot(
            "title",
            new[] { new SnapshotNode("A/a.cs", new[] { "A/a.cs" }) },
            Array.Empty<SnapshotEdge>());

        IReadOnlyList<SnapshotNode> nodes = snapshot.Nodes;
        ((SnapshotNode[])nodes)[0] = new SnapshotNode("Hacked", new[] { "Hacked" });

        Assert.Equal("A/a.cs", snapshot.Nodes[0].Label);
    }

    [Fact]
    public void Nodes_are_copied_on_construction()
    {
        var nodes = new[]
        {
            new SnapshotNode("A/a.cs", new[] { "A/a.cs" }),
            new SnapshotNode("B/b.cs", new[] { "B/b.cs" }),
        };
        var snapshot = new GraphSnapshot("title", nodes, Array.Empty<SnapshotEdge>());

        nodes[0] = new SnapshotNode("Hacked", new[] { "Hacked" });

        Assert.Equal("A/a.cs", snapshot.Nodes[0].Label);
    }

    [Fact]
    public void Edges_are_copied_on_construction()
    {
        var edges = new[]
        {
            new SnapshotEdge("A/a.cs", "B/b.cs", count: 1, external: false, ImportKind.Using),
            new SnapshotEdge("B/b.cs", "C/c.cs", count: 1, external: false, ImportKind.Using),
        };
        var snapshot = new GraphSnapshot("title", Array.Empty<SnapshotNode>(), edges);

        edges[0] = new SnapshotEdge("X", "Y", count: 1, external: false, ImportKind.None);

        Assert.Equal("A/a.cs", snapshot.Edges[0].Source);
    }

    [Fact]
    public void Edges_return_a_fresh_copy_on_every_read()
    {
        var snapshot = new GraphSnapshot(
            "title",
            Array.Empty<SnapshotNode>(),
            new[] { new SnapshotEdge("A/a.cs", "B/b.cs", count: 1, external: false, ImportKind.Using) });

        IReadOnlyList<SnapshotEdge> edges = snapshot.Edges;
        ((SnapshotEdge[])edges)[0] = new SnapshotEdge("X", "Y", count: 1, external: false, ImportKind.None);

        Assert.Equal("A/a.cs", snapshot.Edges[0].Source);
    }

    [Fact]
    public void Summary_counts_reflect_the_contents()
    {
        var snapshot = new GraphSnapshot(
            "title",
            new[]
            {
                new SnapshotNode("src/App", new[] { "src/App/A.cs", "src/App/B.cs", "src/App/C.cs" }),
                new SnapshotNode("src/Models", new[] { "src/Models/Car.cs" }),
            },
            new[]
            {
                new SnapshotEdge("src/App", "src/Models", count: 2, external: false, ImportKind.Using),
            });

        Assert.Equal(2, snapshot.NodeCount);
        Assert.Equal(1, snapshot.EdgeCount);
        Assert.Equal(4, snapshot.FileCount);
    }

    [Fact]
    public void A_with_expression_routes_title_through_validation()
    {
        var snapshot = new GraphSnapshot("title", Array.Empty<SnapshotNode>(), Array.Empty<SnapshotEdge>());

        Assert.Throws<ArgumentNullException>(() => snapshot with { Title = null! });
    }

    [Fact]
    public void Constructor_rejects_a_null_title()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new GraphSnapshot(null!, Array.Empty<SnapshotNode>(), Array.Empty<SnapshotEdge>()));
    }

    [Fact]
    public void Constructor_rejects_null_nodes()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new GraphSnapshot("title", null!, Array.Empty<SnapshotEdge>()));
    }

    [Fact]
    public void Constructor_rejects_null_edges()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new GraphSnapshot("title", Array.Empty<SnapshotNode>(), null!));
    }
}
