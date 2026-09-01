using System.Runtime.CompilerServices;
using ArchUnitSharp.Common.Extraction;

namespace ArchUnitSharp.Common.Tests.Extraction;

public class LogFileOptionsTests
{
    [Fact]
    public void Defaults_write_to_the_current_directory_with_the_default_prefix_and_overwrite()
    {
        var options = new LogFileOptions();

        Assert.Equal(".", options.Directory);
        Assert.Equal("archunit", options.FileNamePrefix);
        Assert.False(options.Append);
    }

    [Fact]
    public void Every_option_can_be_set()
    {
        var options = new LogFileOptions
        {
            Directory = "artifacts/logs",
            FileNamePrefix = "suite",
            Append = true,
        };

        Assert.Equal("artifacts/logs", options.Directory);
        Assert.Equal("suite", options.FileNamePrefix);
        Assert.True(options.Append);
    }

    [Fact]
    public void Two_options_with_the_same_values_are_equal()
    {
        var first = new LogFileOptions { Directory = "logs", Append = true };
        var second = new LogFileOptions { Directory = "logs", Append = true };

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Two_options_with_different_values_are_unequal()
    {
        var first = new LogFileOptions { Directory = "logs" };
        var second = new LogFileOptions();

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void Branching_off_one_parent_does_not_leak_options_between_branches()
    {
        var parent = new LogFileOptions();

        var firstBranch = parent with { Directory = "a" };
        var secondBranch = parent with { Append = true };

        Assert.Equal(".", parent.Directory);
        Assert.False(parent.Append);
        Assert.Equal("a", firstBranch.Directory);
        Assert.False(firstBranch.Append);
        Assert.Equal(".", secondBranch.Directory);
        Assert.True(secondBranch.Append);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Directory_rejects_a_null_or_empty_value(string? directory)
    {
        Assert.ThrowsAny<ArgumentException>(() => new LogFileOptions { Directory = directory! });
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void FileNamePrefix_rejects_a_null_or_empty_value(string? prefix)
    {
        Assert.ThrowsAny<ArgumentException>(() => new LogFileOptions { FileNamePrefix = prefix! });
    }

    [Fact]
    public void Every_property_is_init_only_so_the_options_cannot_be_mutated()
    {
        Assert.All(
            typeof(LogFileOptions).GetProperties(),
            property => Assert.True(
                property.SetMethod is null
                || property.SetMethod.ReturnParameter.GetRequiredCustomModifiers()
                    .Any(modifier => modifier == typeof(IsExternalInit))));
    }
}
