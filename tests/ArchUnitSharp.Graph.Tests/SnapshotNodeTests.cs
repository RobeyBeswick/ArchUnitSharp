namespace ArchUnitSharp.Graph.Tests;

public class SnapshotNodeTests
{
    [Fact]
    public void Two_nodes_with_equal_contents_are_equal()
    {
        var left = new SnapshotNode("src/App", new[] { "src/App/A.cs", "src/App/B.cs" }, external: false);
        var right = new SnapshotNode("src/App", new[] { "src/App/A.cs", "src/App/B.cs" }, external: false);

        Assert.Equal(left, right);
        Assert.Equal(left.GetHashCode(), right.GetHashCode());
    }

    [Fact]
    public void Two_nodes_with_different_labels_are_unequal()
    {
        var left = new SnapshotNode("A", new[] { "A/a.cs" }, external: false);
        var right = new SnapshotNode("B", new[] { "A/a.cs" }, external: false);

        Assert.NotEqual(left, right);
    }

    [Fact]
    public void Two_nodes_with_different_external_flags_are_unequal()
    {
        var left = new SnapshotNode("System.Linq", Array.Empty<string>(), external: true);
        var right = new SnapshotNode("System.Linq", Array.Empty<string>(), external: false);

        Assert.NotEqual(left, right);
    }

    [Fact]
    public void Files_return_a_fresh_copy_on_every_read()
    {
        var node = new SnapshotNode("src/App", new[] { "src/App/A.cs" }, external: false);

        IReadOnlyList<string> files = node.Files;
        ((string[])files)[0] = "Hacked";

        Assert.Equal(new[] { "src/App/A.cs" }, node.Files);
    }

    [Fact]
    public void Files_are_copied_on_construction()
    {
        var files = new[] { "src/App/A.cs", "src/App/B.cs" };
        var node = new SnapshotNode("src/App", files, external: false);

        files[0] = "Hacked";

        Assert.Equal(new[] { "src/App/A.cs", "src/App/B.cs" }, node.Files);
    }

    [Fact]
    public void An_external_node_may_carry_no_files()
    {
        var node = new SnapshotNode("System.Linq", Array.Empty<string>(), external: true);

        Assert.True(node.External);
        Assert.Empty(node.Files);
    }

    [Fact]
    public void Constructor_rejects_a_null_label()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new SnapshotNode(null!, Array.Empty<string>(), external: false));
    }

    [Fact]
    public void Constructor_rejects_an_empty_label()
    {
        Assert.Throws<ArgumentException>(() =>
            new SnapshotNode(string.Empty, Array.Empty<string>(), external: false));
    }

    [Fact]
    public void Constructor_rejects_null_files()
    {
        Assert.Throws<ArgumentNullException>(() => new SnapshotNode("A", null!, external: false));
    }
}
