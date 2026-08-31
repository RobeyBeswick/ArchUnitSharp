namespace ArchUnitSharp.Metrics.Tests;

public class FieldInfoTests
{
    [Fact]
    public void The_field_info_carries_its_accessed_by_methods()
    {
        var field = new FieldInfo("_speed", new[] { "Drive", "Stop" });

        Assert.Equal("_speed", field.Name);
        Assert.Equal(new[] { "Drive", "Stop" }, field.AccessedBy);
    }

    [Fact]
    public void The_single_name_constructor_defaults_to_no_accessed_by_methods()
    {
        var field = new FieldInfo("_speed");

        Assert.Empty(field.AccessedBy);
    }

    [Fact]
    public void The_accessed_by_methods_are_copied_on_receive_so_the_caller_cannot_corrupt_the_info()
    {
        var accessedBy = new List<string> { "Drive" };

        var field = new FieldInfo("_speed", accessedBy);

        accessedBy.Add("Stop");

        Assert.Equal(new[] { "Drive" }, field.AccessedBy);
    }

    [Fact]
    public void Each_access_of_accessed_by_returns_a_fresh_copy()
    {
        var field = new FieldInfo("_speed", new[] { "Drive" });

        IReadOnlyList<string> first = field.AccessedBy;
        IReadOnlyList<string> second = field.AccessedBy;

        Assert.NotSame(first, second);
        Assert.Equal(second, first);
    }

    [Fact]
    public void The_constructor_rejects_null_accessed_by_methods()
    {
        Assert.Throws<ArgumentNullException>(() => new FieldInfo("_speed", null!));
    }

    [Fact]
    public void FieldInfo_rejects_a_null_name()
    {
        Assert.Throws<ArgumentNullException>(() => new FieldInfo(null!));
    }

    [Fact]
    public void FieldInfo_rejects_an_empty_name()
    {
        Assert.Throws<ArgumentException>(() => new FieldInfo(string.Empty));
    }

    [Fact]
    public void Two_field_infos_with_the_same_facts_are_equal()
    {
        var first = new FieldInfo("_speed", new[] { "Drive" });
        var second = new FieldInfo("_speed", new[] { "Drive" });

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Two_field_infos_that_differ_in_one_fact_are_not_equal()
    {
        var first = new FieldInfo("_speed", new[] { "Drive" });
        var second = new FieldInfo("_speed", new[] { "Drive", "Stop" });

        Assert.NotEqual(first, second);
        Assert.NotEqual(second, first);
    }

    [Fact]
    public void A_with_expression_routes_through_the_same_validation()
    {
        var field = new FieldInfo("_speed", new[] { "Drive" });

        Assert.Throws<ArgumentException>(() => field with { Name = string.Empty });
        Assert.Throws<ArgumentNullException>(() => field with { AccessedBy = null! });
    }
}
