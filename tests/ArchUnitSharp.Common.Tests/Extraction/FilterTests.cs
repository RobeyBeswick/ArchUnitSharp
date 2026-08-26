using ArchUnitSharp.Common.Extraction;

namespace ArchUnitSharp.Common.Tests.Extraction;

public class FilterTests
{
    private static Filter CreateFilter(Pattern pattern, MatchTarget target) => new(pattern, target);

    [Fact]
    public void Filename_target_matches_the_file_name_only()
    {
        var filter = CreateFilter(new Pattern("*.cs"), MatchTarget.Filename);

        Assert.True(filter.Matches("src/Models/Car.cs"));
        Assert.True(filter.Matches("Car.cs"));
        Assert.False(filter.Matches("src/Models/Car.txt"));
    }

    [Fact]
    public void The_target_is_bound_to_the_filter_not_the_call_site()
    {
        var pathFilter = CreateFilter(new Pattern("src/**/*.cs"), MatchTarget.Path);
        var filenameFilter = CreateFilter(new Pattern("src/**/*.cs"), MatchTarget.Filename);

        Assert.True(pathFilter.Matches("src/Models/Car.cs"));
        Assert.False(filenameFilter.Matches("src/Models/Car.cs"));
    }

    [Fact]
    public void Path_target_matches_the_whole_identifier()
    {
        var all = CreateFilter(new Pattern("**/*.cs"), MatchTarget.Path);
        var rootOnly = CreateFilter(new Pattern("*.cs"), MatchTarget.Path);

        Assert.True(all.Matches("Car.cs"));
        Assert.True(all.Matches("src/Models/Car.cs"));
        Assert.True(rootOnly.Matches("Car.cs"));
        Assert.False(rootOnly.Matches("src/Models/Car.cs"));
    }

    [Fact]
    public void Path_without_filename_target_matches_the_directory_only()
    {
        var models = CreateFilter(new Pattern("src/Models"), MatchTarget.PathWithoutFilename);
        var any = CreateFilter(new Pattern("**"), MatchTarget.PathWithoutFilename);

        Assert.True(models.Matches("src/Models/Car.cs"));
        Assert.False(models.Matches("src/Other/Car.cs"));
        Assert.False(models.Matches("src/Models.cs"));
        Assert.True(any.Matches("Car.cs"));
        Assert.True(any.Matches("src/Models/Car.cs"));
    }

    [Fact]
    public void Classname_target_matches_the_derived_class_name()
    {
        var exact = CreateFilter(new Pattern("src.Models.Car"), MatchTarget.Classname);
        var controllers = CreateFilter(new Pattern("**/*Controller"), MatchTarget.Classname);

        Assert.True(exact.Matches("src/Models/Car.cs"));
        Assert.False(exact.Matches("src/Models/Other.cs"));
        Assert.True(controllers.Matches("src/Controllers/HomeController.cs"));
    }

    [Fact]
    public void Classname_strips_only_the_final_extension()
    {
        var filter = CreateFilter(new Pattern("src.Models.Car.g"), MatchTarget.Classname);

        Assert.True(filter.Matches("src/Models/Car.g.cs"));
    }

    [Fact]
    public void Classname_leaves_an_extensionless_file_unchanged()
    {
        var filter = CreateFilter(new Pattern("src.Models.Car"), MatchTarget.Classname);

        Assert.True(filter.Matches("src/Models/Car"));
    }

    [Fact]
    public void Backslash_separators_in_the_identifier_are_normalised()
    {
        var filter = CreateFilter(new Pattern("src/Models/**"), MatchTarget.Path);

        Assert.True(filter.Matches(@"src\Models\Car.cs"));
    }

    [Fact]
    public void Matching_is_case_sensitive()
    {
        var filter = CreateFilter(new Pattern("*.cs"), MatchTarget.Filename);

        Assert.False(filter.Matches("src/Models/CAR.CS"));
    }

    [Fact]
    public void Filters_with_the_same_pattern_and_target_are_equal()
    {
        var left = CreateFilter(new Pattern("*.cs"), MatchTarget.Filename);
        var right = CreateFilter(new Pattern("*.cs"), MatchTarget.Filename);

        Assert.Equal(left, right);
        Assert.Equal(left.GetHashCode(), right.GetHashCode());
    }

    [Fact]
    public void Filters_with_a_different_target_are_unequal()
    {
        var filename = CreateFilter(new Pattern("*.cs"), MatchTarget.Filename);
        var path = CreateFilter(new Pattern("*.cs"), MatchTarget.Path);

        Assert.NotEqual(filename, path);
    }

    [Fact]
    public void Null_pattern_is_rejected()
    {
        Assert.Throws<ArgumentNullException>(() => new Filter(null!, MatchTarget.Path));
    }

    [Fact]
    public void Null_identifier_is_rejected()
    {
        var filter = CreateFilter(new Pattern("*.cs"), MatchTarget.Path);

        Assert.Throws<ArgumentNullException>(() => filter.Matches(null!));
    }

    [Fact]
    public void An_undefined_target_is_rejected_when_matching()
    {
        var filter = CreateFilter(new Pattern("*.cs"), (MatchTarget)99);

        Assert.Throws<ArgumentOutOfRangeException>(() => filter.Matches("Car.cs"));
    }
}
