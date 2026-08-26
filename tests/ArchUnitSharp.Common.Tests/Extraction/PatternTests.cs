using ArchUnitSharp.Common.Extraction;

namespace ArchUnitSharp.Common.Tests.Extraction;

public class PatternTests
{
    [Fact]
    public void Constructor_stores_the_glob_as_supplied()
    {
        var pattern = new Pattern("src/**/Car.cs");

        Assert.Equal("src/**/Car.cs", pattern.Glob);
    }

    [Fact]
    public void Matches_returns_true_when_the_whole_candidate_is_matched()
    {
        var pattern = new Pattern("**/*.cs");

        Assert.True(pattern.Matches("src/Models/Car.cs"));
        Assert.False(pattern.Matches("src/Models/Car.txt"));
    }

    [Fact]
    public void Patterns_with_the_same_glob_are_equal()
    {
        var left = new Pattern("*.cs");
        var right = new Pattern("*.cs");

        Assert.Equal(left, right);
        Assert.Equal(left.GetHashCode(), right.GetHashCode());
    }

    [Fact]
    public void Patterns_with_different_globs_are_unequal()
    {
        var left = new Pattern("*.cs");
        var right = new Pattern("*.txt");

        Assert.NotEqual(left, right);
        Assert.False(left == right);
    }

    [Fact]
    public void Null_glob_is_rejected()
    {
        Assert.Throws<ArgumentNullException>(() => new Pattern(null!));
    }

    [Fact]
    public void Empty_glob_is_rejected()
    {
        Assert.Throws<ArgumentException>(() => new Pattern(string.Empty));
    }

    [Fact]
    public void Null_input_to_matches_is_rejected()
    {
        var pattern = new Pattern("*.cs");

        Assert.Throws<ArgumentNullException>(() => pattern.Matches(null!));
    }
}
