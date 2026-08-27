namespace ArchUnitSharp.Files.Tests;

public class FileDetailTests
{
    [Fact]
    public void Carries_the_files_identity_and_source()
    {
        var detail = new FileDetail(
            "src/Models/Car.cs",
            "Car",
            ".cs",
            "src/Models",
            "namespace App.Models { }",
            nonBlankLineCount: 1);

        Assert.Equal("src/Models/Car.cs", detail.Path);
        Assert.Equal("Car", detail.NameWithoutExtension);
        Assert.Equal(".cs", detail.Extension);
        Assert.Equal("src/Models", detail.Directory);
        Assert.Equal("namespace App.Models { }", detail.SourceText);
        Assert.Equal(1, detail.NonBlankLineCount);
    }

    [Fact]
    public void Two_details_with_the_same_values_are_equal()
    {
        var first = new FileDetail(
            "src/Models/Car.cs", "Car", ".cs", "src/Models", "text", nonBlankLineCount: 1);
        var second = new FileDetail(
            "src/Models/Car.cs", "Car", ".cs", "src/Models", "text", nonBlankLineCount: 1);

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Null_path_is_rejected()
    {
        Assert.Throws<ArgumentNullException>(() => new FileDetail(
            null!, "Car", ".cs", "src/Models", "text", nonBlankLineCount: 1));
    }

    [Fact]
    public void Empty_path_is_rejected()
    {
        Assert.Throws<ArgumentException>(() => new FileDetail(
            string.Empty, "Car", ".cs", "src/Models", "text", nonBlankLineCount: 1));
    }

    [Fact]
    public void Null_name_without_extension_is_rejected()
    {
        Assert.Throws<ArgumentNullException>(() => new FileDetail(
            "src/Models/Car.cs", null!, ".cs", "src/Models", "text", nonBlankLineCount: 1));
    }

    [Fact]
    public void Null_extension_is_rejected()
    {
        Assert.Throws<ArgumentNullException>(() => new FileDetail(
            "src/Models/Car.cs", "Car", null!, "src/Models", "text", nonBlankLineCount: 1));
    }

    [Fact]
    public void Null_directory_is_rejected()
    {
        Assert.Throws<ArgumentNullException>(() => new FileDetail(
            "src/Models/Car.cs", "Car", ".cs", null!, "text", nonBlankLineCount: 1));
    }

    [Fact]
    public void Null_source_text_is_rejected()
    {
        Assert.Throws<ArgumentNullException>(() => new FileDetail(
            "src/Models/Car.cs", "Car", ".cs", "src/Models", null!, nonBlankLineCount: 1));
    }

    [Fact]
    public void A_negative_non_blank_line_count_is_rejected()
    {
        Assert.Throws<ArgumentException>(() => new FileDetail(
            "src/Models/Car.cs", "Car", ".cs", "src/Models", "text", nonBlankLineCount: -1));
    }

    [Fact]
    public void An_empty_directory_is_allowed_for_a_root_level_file()
    {
        var detail = new FileDetail(
            "Car.cs", "Car", ".cs", string.Empty, "text", nonBlankLineCount: 1);

        Assert.Equal(string.Empty, detail.Directory);
    }

    [Fact]
    public void An_empty_extension_is_allowed_for_a_dotless_file()
    {
        var detail = new FileDetail(
            "Makefile", "Makefile", string.Empty, string.Empty, "text", nonBlankLineCount: 1);

        Assert.Equal(string.Empty, detail.Extension);
    }

    [Fact]
    public void An_empty_source_text_is_allowed_for_an_empty_file()
    {
        var detail = new FileDetail(
            "src/Models/Car.cs", "Car", ".cs", "src/Models", string.Empty, nonBlankLineCount: 0);

        Assert.Equal(string.Empty, detail.SourceText);
        Assert.Equal(0, detail.NonBlankLineCount);
    }

    [Fact]
    public void A_with_expression_routes_through_the_same_validation()
    {
        var detail = new FileDetail(
            "src/Models/Car.cs", "Car", ".cs", "src/Models", "text", nonBlankLineCount: 1);

        var rewritten = detail with { NameWithoutExtension = "Truck" };
        Assert.Equal("Truck", rewritten.NameWithoutExtension);
        Assert.Equal("Car", detail.NameWithoutExtension);

        Assert.Throws<ArgumentException>(() => detail with { Path = string.Empty });
        Assert.Throws<ArgumentException>(() => detail with { NonBlankLineCount = -2 });
        Assert.Throws<ArgumentNullException>(() => detail with { SourceText = null! });
    }
}
