namespace ArchUnitSharp.Extraction.Tests;

public class SourceFileTests
{
    [Fact]
    public void Constructor_stores_identifier_and_absolute_path()
    {
        var file = new SourceFile("src/Models/Car.cs", "/repo/src/Models/Car.cs");

        Assert.Equal("src/Models/Car.cs", file.Identifier);
        Assert.Equal("/repo/src/Models/Car.cs", file.AbsolutePath);
    }

    [Fact]
    public void Constructor_normalises_backslash_separated_paths()
    {
        var file = new SourceFile(@"src\Models\Car.cs", @"C:\repo\src\Models\Car.cs");

        Assert.Equal("src/Models/Car.cs", file.Identifier);
        Assert.Equal("C:/repo/src/Models/Car.cs", file.AbsolutePath);
    }

    [Fact]
    public void A_with_expression_normalises_backslash_separated_paths()
    {
        var file = new SourceFile("src/Models/Car.cs", "/repo/src/Models/Car.cs");

        var changed = file with { Identifier = @"src\Extra.cs", AbsolutePath = @"C:\repo\src\Extra.cs" };

        Assert.Equal("src/Extra.cs", changed.Identifier);
        Assert.Equal("C:/repo/src/Extra.cs", changed.AbsolutePath);
        Assert.Equal("src/Models/Car.cs", file.Identifier);
        Assert.Equal("/repo/src/Models/Car.cs", file.AbsolutePath);
    }

    [Fact]
    public void Two_files_with_the_same_values_are_equal()
    {
        var left = new SourceFile("src/Models/Car.cs", "/repo/src/Models/Car.cs");
        var right = new SourceFile("src/Models/Car.cs", "/repo/src/Models/Car.cs");

        Assert.Equal(left, right);
        Assert.Equal(left.GetHashCode(), right.GetHashCode());
    }

    [Fact]
    public void Two_files_with_different_identifiers_are_unequal()
    {
        var left = new SourceFile("src/Models/Car.cs", "/repo/src/Models/Car.cs");
        var right = new SourceFile("src/Models/Other.cs", "/repo/src/Models/Other.cs");

        Assert.NotEqual(left, right);
    }

    [Fact]
    public void Null_identifier_is_rejected()
    {
        Assert.Throws<ArgumentNullException>(() => new SourceFile(null!, "/repo/a.cs"));
    }

    [Fact]
    public void Empty_identifier_is_rejected()
    {
        Assert.Throws<ArgumentException>(() => new SourceFile(string.Empty, "/repo/a.cs"));
    }

    [Fact]
    public void Null_absolute_path_is_rejected()
    {
        Assert.Throws<ArgumentNullException>(() => new SourceFile("a.cs", null!));
    }

    [Fact]
    public void Empty_absolute_path_is_rejected()
    {
        Assert.Throws<ArgumentException>(() => new SourceFile("a.cs", string.Empty));
    }
}
