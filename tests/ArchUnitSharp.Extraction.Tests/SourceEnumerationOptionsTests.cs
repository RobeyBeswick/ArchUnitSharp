namespace ArchUnitSharp.Extraction.Tests;

public class SourceEnumerationOptionsTests
{
    [Fact]
    public void Default_options_carry_the_default_exclusion_set()
    {
        var options = new SourceEnumerationOptions();

        Assert.Equal(SourceEnumerationOptions.DefaultExcludedDirectories, options.ExcludedDirectories);
    }

    [Fact]
    public void The_default_exclusion_set_covers_every_excluded_category()
    {
        IReadOnlyList<string> defaults = SourceEnumerationOptions.DefaultExcludedDirectories;

        Assert.Contains("bin", defaults);
        Assert.Contains("obj", defaults);
        Assert.Contains("TestResults", defaults);
        Assert.Contains(".git", defaults);
        Assert.Contains(".svn", defaults);
        Assert.Contains(".hg", defaults);
        Assert.Contains(".vs", defaults);
        Assert.Contains(".idea", defaults);
        Assert.Contains("node_modules", defaults);
        Assert.Contains("packages", defaults);
        Assert.Contains("vendor", defaults);
    }

    [Fact]
    public void The_supplied_list_is_copied_on_construction()
    {
        var supplied = new List<string> { "bin", "obj" };
        var options = new SourceEnumerationOptions(supplied);

        supplied.Add("evil");
        supplied[0] = "other";

        Assert.Equal(new[] { "bin", "obj" }, options.ExcludedDirectories);
    }

    [Fact]
    public void Every_read_returns_a_fresh_copy()
    {
        var options = new SourceEnumerationOptions(new[] { "bin" });

        IReadOnlyList<string> firstRead = options.ExcludedDirectories;
        IReadOnlyList<string> secondRead = options.ExcludedDirectories;

        Assert.NotSame(firstRead, secondRead);
    }

    [Fact]
    public void Mutating_a_returned_list_does_not_corrupt_the_instance()
    {
        var options = new SourceEnumerationOptions(new[] { "bin" });

        ((string[])options.ExcludedDirectories)[0] = "evil";

        Assert.Equal(new[] { "bin" }, options.ExcludedDirectories);
    }

    [Fact]
    public void Mutating_a_returned_default_copy_leaves_later_defaults_intact()
    {
        ((string[])SourceEnumerationOptions.DefaultExcludedDirectories)[0] = "evil";

        Assert.Equal(
            SourceEnumerationOptions.DefaultExcludedDirectories,
            new SourceEnumerationOptions().ExcludedDirectories);
        Assert.DoesNotContain("evil", new SourceEnumerationOptions().ExcludedDirectories);
    }
}
