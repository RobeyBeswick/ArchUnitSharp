namespace ArchUnitSharp.Metrics.Tests;

public class DistanceInfoTests
{
    [Fact]
    public void The_distance_info_carries_its_facts()
    {
        var info = new DistanceInfo(
            "src/Models/Car.cs",
            typeCount: 4,
            abstractTypeCount: 2,
            linesOfCode: 80,
            afferentCoupling: 3,
            efferentCoupling: 1,
            projectFileCount: 10);

        Assert.Equal("src/Models/Car.cs", info.File);
        Assert.Equal(4, info.TypeCount);
        Assert.Equal(2, info.AbstractTypeCount);
        Assert.Equal(80, info.LinesOfCode);
        Assert.Equal(3, info.AfferentCoupling);
        Assert.Equal(1, info.EfferentCoupling);
        Assert.Equal(10, info.ProjectFileCount);
    }

    [Fact]
    public void Two_distance_infos_with_the_same_facts_are_equal()
    {
        var first = Info();
        var second = Info();

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Two_distance_infos_that_differ_in_one_fact_are_not_equal()
    {
        var first = Info();
        var second = Info() with { EfferentCoupling = 2 };

        Assert.NotEqual(first, second);
        Assert.NotEqual(second, first);
    }

    [Fact]
    public void The_constructor_rejects_a_null_file()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new DistanceInfo(null!, 1, 0, 0, 0, 0, 1));
    }

    [Fact]
    public void The_constructor_rejects_an_empty_file()
    {
        Assert.Throws<ArgumentException>(() =>
            new DistanceInfo(string.Empty, 1, 0, 0, 0, 0, 1));
    }

    [Fact]
    public void The_constructor_rejects_a_negative_type_count()
    {
        Assert.Throws<ArgumentException>(() =>
            new DistanceInfo("src/A.cs", -1, 0, 0, 0, 0, 1));
    }

    [Fact]
    public void The_constructor_rejects_abstract_types_above_the_type_count()
    {
        Assert.Throws<ArgumentException>(() =>
            new DistanceInfo("src/A.cs", 2, 3, 0, 0, 0, 1));
    }

    [Fact]
    public void The_constructor_rejects_a_negative_coupling()
    {
        Assert.Throws<ArgumentException>(() =>
            new DistanceInfo("src/A.cs", 1, 0, 0, -1, 0, 2));
    }

    [Fact]
    public void The_constructor_rejects_a_coupling_that_reaches_the_project_file_count()
    {
        Assert.Throws<ArgumentException>(() =>
            new DistanceInfo("src/A.cs", 1, 0, 0, 2, 0, 2));
    }

    [Fact]
    public void The_constructor_rejects_a_non_positive_project_file_count()
    {
        Assert.Throws<ArgumentException>(() =>
            new DistanceInfo("src/A.cs", 1, 0, 0, 0, 0, 0));
    }

    private static DistanceInfo Info() =>
        new("src/Models/Car.cs", 4, 2, 80, 3, 1, 10);
}
