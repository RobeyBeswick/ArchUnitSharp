using ArchUnitSharp.Common.Extraction;

namespace ArchUnitSharp.Common.Tests.Extraction;

public class ImportKindTests
{
    [Fact]
    public void None_is_zero()
    {
        Assert.Equal(0, (int)ImportKind.None);
    }

    [Fact]
    public void Every_kind_is_a_distinct_power_of_two()
    {
        var kinds = new[]
        {
            ImportKind.Using,
            ImportKind.UsingStatic,
            ImportKind.GlobalUsing,
            ImportKind.AliasUsing,
            ImportKind.ExternAlias,
        };

        var seen = new HashSet<int>();
        foreach (var kind in kinds)
        {
            var value = (int)kind;
            Assert.True((value & (value - 1)) == 0, $"{kind} must be a single flag bit");
            Assert.True(seen.Add(value), $"{kind} must be distinct from the other kinds");
        }
    }

    [Fact]
    public void Union_reports_every_component_kind()
    {
        var union = ImportKind.Using | ImportKind.UsingStatic;

        Assert.True(union.HasFlag(ImportKind.Using));
        Assert.True(union.HasFlag(ImportKind.UsingStatic));
        Assert.False(union.HasFlag(ImportKind.GlobalUsing));
    }

    [Fact]
    public void Union_value_is_the_bitwise_or_of_its_components()
    {
        var union = ImportKind.GlobalUsing | ImportKind.AliasUsing | ImportKind.ExternAlias;

        Assert.Equal(4 | 8 | 16, (int)union);
    }

    [Fact]
    public void Every_kind_has_its_declared_flag_value()
    {
        Assert.Equal(1, (int)ImportKind.Using);
        Assert.Equal(2, (int)ImportKind.UsingStatic);
        Assert.Equal(4, (int)ImportKind.GlobalUsing);
        Assert.Equal(8, (int)ImportKind.AliasUsing);
        Assert.Equal(16, (int)ImportKind.ExternAlias);
    }
}
