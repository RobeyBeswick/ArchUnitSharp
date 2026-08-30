using ArchUnitSharp.Common.Extraction;

namespace ArchUnitSharp.Graph.Tests;

public class SnapshotEdgeTests
{
    [Fact]
    public void Two_edges_with_equal_contents_are_equal()
    {
        var left = new SnapshotEdge("src/App", "System.Linq", count: 3, external: true, ImportKind.Using | ImportKind.UsingStatic);
        var right = new SnapshotEdge("src/App", "System.Linq", count: 3, external: true, ImportKind.Using | ImportKind.UsingStatic);

        Assert.Equal(left, right);
        Assert.Equal(left.GetHashCode(), right.GetHashCode());
    }

    [Fact]
    public void Two_edges_with_different_counts_are_unequal()
    {
        var left = new SnapshotEdge("A", "B", count: 1, external: false, ImportKind.Using);
        var right = new SnapshotEdge("A", "B", count: 2, external: false, ImportKind.Using);

        Assert.NotEqual(left, right);
    }

    [Fact]
    public void Two_edges_with_different_import_kinds_are_unequal()
    {
        var left = new SnapshotEdge("A", "B", count: 1, external: false, ImportKind.Using);
        var right = new SnapshotEdge("A", "B", count: 1, external: false, ImportKind.UsingStatic);

        Assert.NotEqual(left, right);
    }

    [Fact]
    public void Two_edges_with_different_external_flags_are_unequal()
    {
        var left = new SnapshotEdge("A", "B", count: 1, external: false, ImportKind.Using);
        var right = new SnapshotEdge("A", "B", count: 1, external: true, ImportKind.Using);

        Assert.NotEqual(left, right);
    }

    [Fact]
    public void Constructor_rejects_a_null_source()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new SnapshotEdge(null!, "B", count: 1, external: false, ImportKind.Using));
    }

    [Fact]
    public void Constructor_rejects_an_empty_source()
    {
        Assert.Throws<ArgumentException>(() =>
            new SnapshotEdge(string.Empty, "B", count: 1, external: false, ImportKind.Using));
    }

    [Fact]
    public void Constructor_rejects_a_null_target()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new SnapshotEdge("A", null!, count: 1, external: false, ImportKind.Using));
    }

    [Fact]
    public void Constructor_rejects_an_empty_target()
    {
        Assert.Throws<ArgumentException>(() =>
            new SnapshotEdge("A", string.Empty, count: 1, external: false, ImportKind.Using));
    }

    [Fact]
    public void Constructor_rejects_a_count_of_zero()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new SnapshotEdge("A", "B", count: 0, external: false, ImportKind.Using));
    }
}
