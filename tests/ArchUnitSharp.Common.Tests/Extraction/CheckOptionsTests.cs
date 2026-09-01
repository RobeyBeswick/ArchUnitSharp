using System.Reflection;
using System.Runtime.CompilerServices;
using ArchUnitSharp.Common.Extraction;

namespace ArchUnitSharp.Common.Tests.Extraction;

public class CheckOptionsTests
{
    [Fact]
    public void Defaults_turn_everything_off_or_to_its_zero_value()
    {
        var options = new CheckOptions();

        Assert.False(options.AllowEmptyTests);
        Assert.Equal(LoggingLevel.None, options.Logging);
        Assert.Null(options.LogFile);
        Assert.False(options.ClearCache);
        Assert.False(options.IgnoreTestCode);
        Assert.False(options.IgnoreGeneratedCode);
    }

    [Fact]
    public void Every_option_can_be_set()
    {
        var options = new CheckOptions
        {
            AllowEmptyTests = true,
            Logging = LoggingLevel.Warn,
            LogFile = new LogFileOptions { Directory = "logs", FileNamePrefix = "suite", Append = true },
            ClearCache = true,
            IgnoreTestCode = true,
            IgnoreGeneratedCode = true,
        };

        Assert.True(options.AllowEmptyTests);
        Assert.Equal(LoggingLevel.Warn, options.Logging);
        Assert.Equal("logs", options.LogFile!.Directory);
        Assert.True(options.LogFile.Append);
        Assert.True(options.ClearCache);
        Assert.True(options.IgnoreTestCode);
        Assert.True(options.IgnoreGeneratedCode);
    }

    [Fact]
    public void Branching_off_one_parent_does_not_leak_options_between_branches()
    {
        var parent = new CheckOptions();

        var firstBranch = parent with { AllowEmptyTests = true };
        var secondBranch = parent with { ClearCache = true };

        Assert.False(parent.AllowEmptyTests);
        Assert.False(parent.ClearCache);
        Assert.True(firstBranch.AllowEmptyTests);
        Assert.False(firstBranch.ClearCache);
        Assert.False(secondBranch.AllowEmptyTests);
        Assert.True(secondBranch.ClearCache);
    }

    [Fact]
    public void Two_bags_with_the_same_values_are_equal()
    {
        var first = new CheckOptions { AllowEmptyTests = true, ClearCache = true };
        var second = new CheckOptions { AllowEmptyTests = true, ClearCache = true };

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Two_bags_with_different_values_are_unequal()
    {
        var first = new CheckOptions { AllowEmptyTests = true };
        var second = new CheckOptions();

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void Every_property_is_init_only_so_the_bag_cannot_be_mutated()
    {
        Assert.All(
            typeof(CheckOptions).GetProperties(),
            property => Assert.True(
                property.SetMethod is null
                || property.SetMethod.ReturnParameter.GetRequiredCustomModifiers()
                    .Any(modifier => modifier == typeof(IsExternalInit))));
    }
}
