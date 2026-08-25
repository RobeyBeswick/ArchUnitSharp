using ArchUnitSharp.Common.Extraction;

namespace ArchUnitSharp.Common.Tests.Extraction;

public class EdgeTests
{
    private static Edge CreateEdge() =>
        new("src/Models/Car.cs", "src/Models/Engine.cs", external: false, ImportKind.Using);

    [Fact]
    public void Constructor_stores_all_four_values()
    {
        var edge = CreateEdge();

        Assert.Equal("src/Models/Car.cs", edge.Source);
        Assert.Equal("src/Models/Engine.cs", edge.Target);
        Assert.False(edge.External);
        Assert.Equal(ImportKind.Using, edge.ImportKinds);
    }

    [Fact]
    public void External_edges_are_distinguished()
    {
        var external = CreateEdge() with { External = true };

        Assert.True(external.External);
    }

    [Fact]
    public void Edges_with_the_same_four_values_are_equal()
    {
        var left = CreateEdge();
        var right = new Edge("src/Models/Car.cs", "src/Models/Engine.cs", external: false, ImportKind.Using);

        Assert.Equal(left, right);
        Assert.Equal(left.GetHashCode(), right.GetHashCode());
    }

    [Theory]
    [InlineData("src/Models/Other.cs", "src/Models/Engine.cs", false, ImportKind.Using)]
    [InlineData("src/Models/Car.cs", "src/Models/Other.cs", false, ImportKind.Using)]
    [InlineData("src/Models/Car.cs", "src/Models/Engine.cs", true, ImportKind.Using)]
    [InlineData("src/Models/Car.cs", "src/Models/Engine.cs", false, ImportKind.UsingStatic)]
    public void Changing_any_value_makes_edges_unequal(
        string source,
        string target,
        bool external,
        ImportKind importKinds)
    {
        var other = new Edge(source, target, external, importKinds);

        Assert.NotEqual(CreateEdge(), other);
    }

    [Fact]
    public void With_branches_do_not_see_each_others_data()
    {
        var parent = CreateEdge();
        var branchToWheel = parent with { Target = "src/Models/Wheel.cs" };
        var branchToBody = parent with { Target = "src/Models/Body.cs" };

        Assert.Equal("src/Models/Engine.cs", parent.Target);
        Assert.Equal("src/Models/Wheel.cs", branchToWheel.Target);
        Assert.Equal("src/Models/Body.cs", branchToBody.Target);
    }

    [Fact]
    public void With_branch_leaves_the_parent_unchanged_in_every_respect()
    {
        var parent = CreateEdge();
        var branch = parent with { ImportKinds = ImportKind.Using | ImportKind.UsingStatic, External = true };

        Assert.Equal("src/Models/Car.cs", parent.Source);
        Assert.Equal("src/Models/Engine.cs", parent.Target);
        Assert.False(parent.External);
        Assert.Equal(ImportKind.Using, parent.ImportKinds);

        Assert.Equal(ImportKind.Using | ImportKind.UsingStatic, branch.ImportKinds);
        Assert.True(branch.External);
    }

    [Fact]
    public void Merged_edge_carries_the_union_of_parallel_import_kinds()
    {
        var first = new Edge("a.cs", "b.cs", external: false, ImportKind.Using);
        var second = new Edge("a.cs", "b.cs", external: false, first.ImportKinds | ImportKind.UsingStatic | ImportKind.GlobalUsing);

        Assert.Equal(1 | 2 | 4, (int)second.ImportKinds);
    }

    [Fact]
    public void Null_source_is_rejected()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new Edge(null!, "b.cs", external: false, ImportKind.Using));
    }

    [Fact]
    public void Null_target_is_rejected()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new Edge("a.cs", null!, external: false, ImportKind.Using));
    }

    [Fact]
    public void Empty_source_is_rejected()
    {
        Assert.Throws<ArgumentException>(() =>
            new Edge(string.Empty, "b.cs", external: false, ImportKind.Using));
    }

    [Fact]
    public void Empty_target_is_rejected()
    {
        Assert.Throws<ArgumentException>(() =>
            new Edge("a.cs", string.Empty, external: false, ImportKind.Using));
    }

    [Fact]
    public void With_expression_cannot_introduce_a_null_source()
    {
        var edge = CreateEdge();

        Assert.Throws<ArgumentNullException>(() => edge with { Source = null! });
    }

    [Fact]
    public void With_expression_cannot_introduce_an_empty_target()
    {
        var edge = CreateEdge();

        Assert.Throws<ArgumentException>(() => edge with { Target = string.Empty });
    }
}
