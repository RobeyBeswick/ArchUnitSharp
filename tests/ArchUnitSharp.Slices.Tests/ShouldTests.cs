using ArchUnitSharp.Common.Extraction;

namespace ArchUnitSharp.Slices.Tests;

public class ShouldTests
{
    [Fact]
    public void ContainDependency_adds_a_positive_rule()
    {
        var policy = new Slices(Graph(Self("a.cs")))
            .DefinedBy("src/(**)/*.cs")
            .Should()
            .ContainDependency("src/features/**", "src/shared/**");

        SliceRule rule = Assert.Single(policy.Rules);
        Assert.False(rule.Negate);
        Assert.Equal("src/features/**", rule.From.Pattern.Glob);
        Assert.Equal("src/shared/**", rule.To.Pattern.Glob);
    }

    [Fact]
    public void ContainDependency_rejects_a_null_from_glob()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new Slices(Graph(Self("a.cs"))).Should().ContainDependency(null!, "src/shared/**"));
    }

    [Fact]
    public void ContainDependency_rejects_an_empty_from_glob()
    {
        Assert.Throws<ArgumentException>(() =>
            new Slices(Graph(Self("a.cs"))).Should().ContainDependency(string.Empty, "src/shared/**"));
    }

    [Fact]
    public void ContainDependency_rejects_a_null_to_glob()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new Slices(Graph(Self("a.cs"))).Should().ContainDependency("src/features/**", null!));
    }

    [Fact]
    public void ContainDependency_rejects_an_empty_to_glob()
    {
        Assert.Throws<ArgumentException>(() =>
            new Slices(Graph(Self("a.cs"))).Should().ContainDependency("src/features/**", string.Empty));
    }

    private static Graph Graph(params Edge[] edges) => new(edges);

    private static Edge Self(string file) => new(file, file, external: false, ImportKind.None);
}
