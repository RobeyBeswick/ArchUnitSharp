using ArchUnitSharp.Common.Extraction;

namespace ArchUnitSharp.Graph.Tests;

public class GraphSnapshotTests
{
    [Fact]
    public void Two_snapshots_with_equal_contents_are_equal()
    {
        var left = new GraphSnapshot(
            "title",
            new[] { new SnapshotNode("A/a.cs", new[] { "A/a.cs" }, external: false) },
            new[] { new SnapshotEdge("A/a.cs", "B/b.cs", count: 1, external: false, ImportKind.Using) });
        var right = new GraphSnapshot(
            "title",
            new[] { new SnapshotNode("A/a.cs", new[] { "A/a.cs" }, external: false) },
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
    public void Nodes_return_a_fresh_copy_on_every_read()
    {
        var snapshot = new GraphSnapshot(
            "title",
            new[] { new SnapshotNode("A/a.cs", new[] { "A/a.cs" }, external: false) },
            Array.Empty<SnapshotEdge>());

        IReadOnlyList<SnapshotNode> nodes = snapshot.Nodes;
        ((SnapshotNode[])nodes)[0] = new SnapshotNode("Hacked", Array.Empty<string>(), external: false);

        Assert.Equal("A/a.cs", snapshot.Nodes[0].Label);
    }

    [Fact]
    public void Nodes_are_copied_on_construction()
    {
        var nodes = new[]
        {
            new SnapshotNode("A/a.cs", new[] { "A/a.cs" }, external: false),
            new SnapshotNode("B/b.cs", new[] { "B/b.cs" }, external: false),
        };
        var snapshot = new GraphSnapshot("title", nodes, Array.Empty<SnapshotEdge>());

        nodes[0] = new SnapshotNode("Hacked", Array.Empty<string>(), external: false);

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
                new SnapshotNode("A/a.cs", new[] { "A/a.cs", "A/b.cs", "A/c.cs" }, external: false),
                new SnapshotNode("B/b.cs", new[] { "B/b.cs" }, external: false),
                new SnapshotNode("System.Linq", Array.Empty<string>(), external: true),
            },
            new[]
            {
                new SnapshotEdge("A/a.cs", "B/b.cs", count: 1, external: false, ImportKind.Using),
                new SnapshotEdge("A/a.cs", "System.Linq", count: 1, external: true, ImportKind.Using),
            });

        Assert.Equal(3, snapshot.NodeCount);
        Assert.Equal(2, snapshot.EdgeCount);
        Assert.Equal(4, snapshot.FileCount);
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
