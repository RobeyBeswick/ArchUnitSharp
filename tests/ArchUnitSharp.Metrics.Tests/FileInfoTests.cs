namespace ArchUnitSharp.Metrics.Tests;

public class FileInfoTests
{
    [Fact]
    public void The_file_info_carries_its_extracted_facts()
    {
        var info = new FileInfo(
            "src/Models/Car.cs",
            linesOfCode: 42,
            statementCount: 5,
            importCount: 2,
            classCount: 1,
            interfaceCount: 0,
            new[] { new ClassInfo("App.Car", "src/Models/Car.cs", Array.Empty<MethodInfo>(), Array.Empty<FieldInfo>()) });

        Assert.Equal("src/Models/Car.cs", info.Path);
        Assert.Equal(42, info.LinesOfCode);
        Assert.Equal(5, info.StatementCount);
        Assert.Equal(2, info.ImportCount);
        Assert.Equal(1, info.ClassCount);
        Assert.Equal(0, info.InterfaceCount);
        Assert.Equal(new[] { "App.Car" }, info.ClassInfos.Select(static classInfo => classInfo.Name));
    }

    [Fact]
    public void The_class_infos_are_copied_on_receive_so_the_caller_cannot_corrupt_the_info()
    {
        var classInfos = new List<ClassInfo>
        {
            new("App.Car", "src/Car.cs", Array.Empty<MethodInfo>(), Array.Empty<FieldInfo>()),
        };

        var info = new FileInfo("src/Car.cs", 1, 0, 0, 1, 0, classInfos);

        classInfos.Add(new ClassInfo("App.Truck", "src/Car.cs", Array.Empty<MethodInfo>(), Array.Empty<FieldInfo>()));

        Assert.Single(info.ClassInfos);
    }

    [Fact]
    public void Each_access_of_class_infos_returns_a_fresh_copy()
    {
        var info = new FileInfo(
            "src/Car.cs",
            1,
            0,
            0,
            1,
            0,
            new[] { new ClassInfo("App.Car", "src/Car.cs", Array.Empty<MethodInfo>(), Array.Empty<FieldInfo>()) });

        IReadOnlyList<ClassInfo> first = info.ClassInfos;
        IReadOnlyList<ClassInfo> second = info.ClassInfos;

        Assert.NotSame(first, second);
        Assert.Equal(second, first);
    }

    [Fact]
    public void The_constructor_rejects_a_null_path()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new FileInfo(null!, 1, 0, 0, 0, 0, Array.Empty<ClassInfo>()));
    }

    [Fact]
    public void The_constructor_rejects_an_empty_path()
    {
        Assert.Throws<ArgumentException>(() =>
            new FileInfo(string.Empty, 1, 0, 0, 0, 0, Array.Empty<ClassInfo>()));
    }

    [Fact]
    public void The_constructor_rejects_null_class_infos()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new FileInfo("src/Car.cs", 1, 0, 0, 0, 0, null!));
    }

    [Fact]
    public void Two_file_infos_with_the_same_facts_are_equal()
    {
        var first = new FileInfo("src/Car.cs", 1, 0, 0, 0, 0, Array.Empty<ClassInfo>());
        var second = new FileInfo("src/Car.cs", 1, 0, 0, 0, 0, Array.Empty<ClassInfo>());

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Two_file_infos_that_differ_in_one_fact_are_not_equal()
    {
        var first = new FileInfo("src/Car.cs", 42, 5, 2, 1, 0, Array.Empty<ClassInfo>());
        var second = new FileInfo("src/Car.cs", 43, 5, 2, 1, 0, Array.Empty<ClassInfo>());

        Assert.NotEqual(first, second);
        Assert.NotEqual(second, first);
    }
}
