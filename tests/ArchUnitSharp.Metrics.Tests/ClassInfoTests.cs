namespace ArchUnitSharp.Metrics.Tests;

public class ClassInfoTests
{
    [Fact]
    public void The_class_info_carries_its_extracted_facts()
    {
        var info = new ClassInfo(
            "App.Models.Car",
            "src/Models/Car.cs",
            new[] { new MethodInfo("Drive"), new MethodInfo("Stop") },
            new[] { new FieldInfo("_speed"), new FieldInfo("_name") });

        Assert.Equal("App.Models.Car", info.Name);
        Assert.Equal("src/Models/Car.cs", info.FilePath);
        Assert.Equal(
            new[] { "Drive", "Stop" },
            info.Methods.Select(static method => method.Name));
        Assert.Equal(
            new[] { "_speed", "_name" },
            info.Fields.Select(static field => field.Name));
        Assert.Equal("src/Models/Car.cs:App.Models.Car", info.Identifier);
    }

    [Fact]
    public void The_lists_are_copied_on_receive_so_the_caller_cannot_corrupt_the_info()
    {
        var methods = new List<MethodInfo> { new("Drive") };
        var fields = new List<FieldInfo> { new("_speed") };

        var info = new ClassInfo("App.Car", "src/Car.cs", methods, fields);

        methods.Add(new MethodInfo("Stop"));
        fields.Add(new FieldInfo("_name"));

        Assert.Equal(new[] { "Drive" }, info.Methods.Select(static method => method.Name));
        Assert.Equal(new[] { "_speed" }, info.Fields.Select(static field => field.Name));
    }

    [Fact]
    public void Each_access_of_a_list_returns_a_fresh_copy()
    {
        var info = new ClassInfo(
            "App.Car",
            "src/Car.cs",
            new[] { new MethodInfo("Drive") },
            Array.Empty<FieldInfo>());

        IReadOnlyList<MethodInfo> first = info.Methods;
        IReadOnlyList<MethodInfo> second = info.Methods;

        Assert.NotSame(first, second);
        Assert.Equal(second, first);
    }

    [Fact]
    public void The_constructor_rejects_a_null_name()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ClassInfo(null!, "src/Car.cs", Array.Empty<MethodInfo>(), Array.Empty<FieldInfo>()));
    }

    [Fact]
    public void The_constructor_rejects_an_empty_name()
    {
        Assert.Throws<ArgumentException>(() =>
            new ClassInfo(string.Empty, "src/Car.cs", Array.Empty<MethodInfo>(), Array.Empty<FieldInfo>()));
    }

    [Fact]
    public void The_constructor_rejects_a_null_file_path()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ClassInfo("App.Car", null!, Array.Empty<MethodInfo>(), Array.Empty<FieldInfo>()));
    }

    [Fact]
    public void The_constructor_rejects_null_lists()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ClassInfo("App.Car", "src/Car.cs", null!, Array.Empty<FieldInfo>()));
        Assert.Throws<ArgumentNullException>(() =>
            new ClassInfo("App.Car", "src/Car.cs", Array.Empty<MethodInfo>(), null!));
    }

    [Fact]
    public void Two_class_infos_with_the_same_facts_are_equal()
    {
        var first = new ClassInfo("App.Car", "src/Car.cs", new[] { new MethodInfo("Drive") }, Array.Empty<FieldInfo>());
        var second = new ClassInfo("App.Car", "src/Car.cs", new[] { new MethodInfo("Drive") }, Array.Empty<FieldInfo>());

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Two_class_infos_that_differ_in_one_fact_are_not_equal()
    {
        var first = new ClassInfo("App.Car", "src/Car.cs", new[] { new MethodInfo("Drive") }, new[] { new FieldInfo("_speed") });
        var second = new ClassInfo("App.Car", "src/Car.cs", new[] { new MethodInfo("Drive") }, new[] { new FieldInfo("_speed"), new FieldInfo("_name") });

        Assert.NotEqual(first, second);
        Assert.NotEqual(second, first);
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
    public void FieldInfo_rejects_a_null_name()
    {
        Assert.Throws<ArgumentNullException>(() => new FieldInfo(null!));
    }

    [Fact]
    public void FieldInfo_rejects_an_empty_name()
    {
        Assert.Throws<ArgumentException>(() => new FieldInfo(string.Empty));
    }
}
