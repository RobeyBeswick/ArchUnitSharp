namespace ArchUnitSharp.Extraction.Tests;

using ArchUnitSharp.Common.Extraction;

public class ImportTests
{
    [Fact]
    public void Constructor_stores_kind_and_name()
    {
        var import = new Import(ImportKind.UsingStatic, "System.Math");

        Assert.Equal(ImportKind.UsingStatic, import.Kind);
        Assert.Equal("System.Math", import.Name);
    }

    [Fact]
    public void A_with_expression_cannot_introduce_a_bad_name()
    {
        var import = new Import(ImportKind.Using, "System");

        Assert.Throws<ArgumentNullException>(() => import with { Name = null! });
        Assert.Throws<ArgumentException>(() => import with { Name = string.Empty });

        Assert.Equal("System", import.Name);
    }

    [Fact]
    public void Null_name_is_rejected()
    {
        Assert.Throws<ArgumentNullException>(() => new Import(ImportKind.Using, null!));
    }

    [Fact]
    public void Empty_name_is_rejected()
    {
        Assert.Throws<ArgumentException>(() => new Import(ImportKind.Using, string.Empty));
    }

    [Fact]
    public void Two_imports_with_the_same_values_are_equal()
    {
        var left = new Import(ImportKind.Using, "System");
        var right = new Import(ImportKind.Using, "System");

        Assert.Equal(left, right);
        Assert.Equal(left.GetHashCode(), right.GetHashCode());
    }

    [Fact]
    public void Two_imports_with_different_kinds_are_unequal()
    {
        var left = new Import(ImportKind.Using, "System");
        var right = new Import(ImportKind.GlobalUsing, "System");

        Assert.NotEqual(left, right);
    }
}
