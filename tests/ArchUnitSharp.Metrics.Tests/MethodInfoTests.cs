namespace ArchUnitSharp.Metrics.Tests;

public class MethodInfoTests
{
    [Fact]
    public void The_method_info_carries_its_accessed_fields()
    {
        var method = new MethodInfo("Drive", new[] { "_speed", "_gear" });

        Assert.Equal("Drive", method.Name);
        Assert.Equal(new[] { "_gear", "_speed" }, method.AccessedFields);
    }

    [Fact]
    public void The_single_name_constructor_defaults_to_no_accessed_fields()
    {
        var method = new MethodInfo("Drive");

        Assert.Empty(method.AccessedFields);
    }

    [Fact]
    public void The_accessed_fields_are_copied_on_receive_so_the_caller_cannot_corrupt_the_info()
    {
        var accessed = new List<string> { "_speed" };

        var method = new MethodInfo("Drive", accessed);

        accessed.Add("_gear");

        Assert.Equal(new[] { "_speed" }, method.AccessedFields);
    }

    [Fact]
    public void Each_access_of_accessed_fields_returns_a_fresh_copy()
    {
        var method = new MethodInfo("Drive", new[] { "_speed" });

        IReadOnlyList<string> first = method.AccessedFields;
        IReadOnlyList<string> second = method.AccessedFields;

        Assert.NotSame(first, second);
        Assert.Equal(second, first);
    }

    [Fact]
    public void The_constructor_rejects_null_accessed_fields()
    {
        Assert.Throws<ArgumentNullException>(() => new MethodInfo("Drive", null!));
    }

    [Fact]
    public void MethodInfo_rejects_a_null_name()
    {
        Assert.Throws<ArgumentNullException>(() => new MethodInfo(null!));
    }

    [Fact]
    public void MethodInfo_rejects_an_empty_name()
    {
        Assert.Throws<ArgumentException>(() => new MethodInfo(string.Empty));
    }

    [Fact]
    public void Two_method_infos_with_the_same_facts_are_equal()
    {
        var first = new MethodInfo("Drive", new[] { "_speed" });
        var second = new MethodInfo("Drive", new[] { "_speed" });

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Two_method_infos_that_differ_in_one_fact_are_not_equal()
    {
        var first = new MethodInfo("Drive", new[] { "_speed" });
        var second = new MethodInfo("Drive", new[] { "_speed", "_gear" });

        Assert.NotEqual(first, second);
        Assert.NotEqual(second, first);
    }

    [Fact]
    public void A_with_expression_routes_through_the_same_validation()
    {
        var method = new MethodInfo("Drive", new[] { "_speed" });

        Assert.Throws<ArgumentException>(() => method with { Name = string.Empty });
        Assert.Throws<ArgumentNullException>(() => method with { AccessedFields = null! });
    }
}
