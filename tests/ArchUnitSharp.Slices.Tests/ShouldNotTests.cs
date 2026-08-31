using ArchUnitSharp.Common.Extraction;

namespace ArchUnitSharp.Slices.Tests;

public class ShouldNotTests
{
    [Fact]
    public void ContainDependency_adds_a_negated_rule()
    {
        var policy = new Slices(Graph(Self("a.cs")))
            .DefinedBy("src/(**)/*.cs")
            .ShouldNot()
            .ContainDependency("src/features/**", "src/legacy/**");

        SliceRule rule = Assert.Single(policy.Rules);
        Assert.True(rule.Negate);
        Assert.Equal("src/features/**", rule.From.Pattern.Glob);
        Assert.Equal("src/legacy/**", rule.To.Pattern.Glob);
    }

    [Fact]
    public void ContainDependency_rejects_a_null_from_glob()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new Slices(Graph(Self("a.cs"))).ShouldNot().ContainDependency(null!, "src/legacy/**"));
    }

    [Fact]
    public void ContainDependency_rejects_an_empty_from_glob()
    {
        Assert.Throws<ArgumentException>(() =>
            new Slices(Graph(Self("a.cs"))).ShouldNot().ContainDependency(string.Empty, "src/legacy/**"));
    }

    [Fact]
    public void ContainDependency_rejects_a_null_to_glob()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new Slices(Graph(Self("a.cs"))).ShouldNot().ContainDependency("src/features/**", null!));
    }

    [Fact]
    public void ContainDependency_rejects_an_empty_to_glob()
    {
        Assert.Throws<ArgumentException>(() =>
            new Slices(Graph(Self("a.cs"))).ShouldNot().ContainDependency("src/features/**", string.Empty));
    }

    private static Graph Graph(params Edge[] edges) => new(edges);

    private static Edge Self(string file) => new(file, file, external: false, ImportKind.None);
}
