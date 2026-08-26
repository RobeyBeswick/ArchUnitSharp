namespace ArchUnitSharp.Extraction.Tests;

public class PathNormaliserTests
{
    [Theory]
    [InlineData(@"a\b\c", "a/b/c")]
    [InlineData(@"C:\repo\src\App.sln", "C:/repo/src/App.sln")]
    [InlineData(@"src\a\b.cs", "src/a/b.cs")]
    [InlineData("a/b\\c.cs", "a/b/c.cs")]
    [InlineData("already/forward/slashed", "already/forward/slashed")]
    public void Normalise_replaces_backslashes_with_forward_slashes(string input, string expected)
    {
        Assert.Equal(expected, PathNormaliser.Normalise(input));
    }
}
