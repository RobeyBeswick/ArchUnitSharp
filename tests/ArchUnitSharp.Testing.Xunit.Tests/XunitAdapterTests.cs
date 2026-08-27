namespace ArchUnitSharp.Testing.Xunit.Tests;

public class XunitAdapterTests
{
    [Theory]
    [InlineData("xunit.runner.visualstudio.testadapter")]
    [InlineData("xunit.runner.utility.netcoreapp10")]
    [InlineData("xunit.runner.reporters.netcoreapp10")]
    [InlineData("xunit.execution.dotnet")]
    public void A_runner_or_execution_assembly_names_a_live_xunit_run(string assemblyName)
    {
        Assert.True(XunitAdapter.IsXunitRunner(assemblyName));
    }

    [Theory]
    [InlineData("xunit")]
    [InlineData("xunit.core")]
    [InlineData("xunit.assert")]
    [InlineData("xunit.abstractions")]
    [InlineData("ArchUnitSharp.Testing.Xunit")]
    [InlineData("ArchUnitSharp.Testing")]
    [InlineData("System.Linq")]
    [InlineData("")]
    public void A_package_reference_alone_does_not_name_a_live_xunit_run(string assemblyName)
    {
        Assert.False(XunitAdapter.IsXunitRunner(assemblyName));
    }

    [Fact]
    public void Detect_returns_true_when_any_loaded_assembly_is_a_runner_or_execution_assembly()
    {
        Assert.True(XunitAdapter.Detect(new[]
        {
            "ArchUnitSharp.Testing.Xunit",
            "ArchUnitSharp.Testing",
            "xunit.assert",
            "xunit.execution.dotnet",
        }));
    }

    [Fact]
    public void Detect_returns_false_when_only_package_references_are_loaded()
    {
        Assert.False(XunitAdapter.Detect(new[]
        {
            "ArchUnitSharp.Testing.Xunit",
            "ArchUnitSharp.Testing",
            "xunit.assert",
            "System.Linq",
        }));
    }

    [Fact]
    public void Detect_returns_false_when_nothing_is_loaded()
    {
        Assert.False(XunitAdapter.Detect(Array.Empty<string>()));
    }

    [Fact]
    public void Detect_rejects_a_null_name_list()
    {
        Assert.Throws<ArgumentNullException>(() => XunitAdapter.Detect(null!));
    }
}
