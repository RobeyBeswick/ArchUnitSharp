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
    public void A_matching_exclusion_vetoes_the_parent_match()
    {
        var filter = new Filter(
            new Pattern("src/Models**"),
            MatchTarget.PathWithoutFilename,
            new[] { new Filter(new Pattern("src/Models/Generated"), MatchTarget.PathWithoutFilename) });

        Assert.True(filter.Matches("src/Models/Car.cs"));
        Assert.False(filter.Matches("src/Models/Generated/Gen.cs"));
        Assert.False(filter.Matches("src/Other/Car.cs"));
    }

    [Fact]
    public void An_exclusion_is_matched_against_its_own_target()
    {
        var filter = new Filter(
            new Pattern("**"),
            MatchTarget.Path,
            new[] { new Filter(new Pattern("Car.cs"), MatchTarget.Filename) });

        Assert.True(filter.Matches("src/Models/Truck.cs"));
        Assert.False(filter.Matches("src/Models/Car.cs"));
        Assert.False(filter.Matches("Car.cs"));
    }

    [Fact]
    public void An_exclusion_that_matches_nothing_leaves_the_filter_unchanged()
    {
        var filter = new Filter(
            new Pattern("src/Models"),
            MatchTarget.PathWithoutFilename,
            new[] { new Filter(new Pattern("src/App"), MatchTarget.PathWithoutFilename) });

        Assert.True(filter.Matches("src/Models/Car.cs"));
        Assert.False(filter.Matches("src/Models/Generated/Gen.cs"));
    }

    [Fact]
    public void WithExclusion_returns_a_filter_that_carries_the_exclusion()
    {
        var filter = CreateFilter(new Pattern("src/Models**"), MatchTarget.PathWithoutFilename);

        var narrowed = filter.WithExclusion(
            new Filter(new Pattern("src/Models/Generated"), MatchTarget.PathWithoutFilename));

        Assert.True(filter.Matches("src/Models/Generated/Gen.cs"));
        Assert.False(narrowed.Matches("src/Models/Generated/Gen.cs"));
        Assert.True(narrowed.Matches("src/Models/Car.cs"));
        Assert.Empty(filter.Exclusions);
        Assert.Single(narrowed.Exclusions);
    }

    [Fact]
    public void A_filter_with_different_exclusions_is_unequal()
    {
        var plain = new Filter(new Pattern("src/Models"), MatchTarget.PathWithoutFilename);
        var excluded = new Filter(
            new Pattern("src/Models"),
            MatchTarget.PathWithoutFilename,
            new[] { new Filter(new Pattern("src/Models/Generated"), MatchTarget.PathWithoutFilename) });
        var same = new Filter(
            new Pattern("src/Models"),
            MatchTarget.PathWithoutFilename,
            new[] { new Filter(new Pattern("src/Models/Generated"), MatchTarget.PathWithoutFilename) });

        Assert.NotEqual(plain, excluded);
        Assert.Equal(excluded, same);
        Assert.Equal(excluded.GetHashCode(), same.GetHashCode());
    }

    [Fact]
    public void The_exclusion_list_is_copied_on_construction()
    {
        var list = new List<Filter> { new Filter(new Pattern("Car.cs"), MatchTarget.Filename) };

        var filter = new Filter(new Pattern("**"), MatchTarget.Path, list);
        list.Add(new Filter(new Pattern("Truck.cs"), MatchTarget.Filename));
        list.Clear();

        Assert.True(filter.Matches("src/Models/Truck.cs"));
        Assert.False(filter.Matches("src/Models/Car.cs"));
    }

    [Fact]
    public void The_exclusion_list_getter_returns_a_copy()
    {
        var filter = new Filter(
            new Pattern("**"),
            MatchTarget.Path,
            new[] { new Filter(new Pattern("Car.cs"), MatchTarget.Filename) });

        IReadOnlyList<Filter> first = filter.Exclusions;
        IReadOnlyList<Filter> second = filter.Exclusions;

        Assert.NotSame(first, second);
    }

    [Fact]
    public void Null_exclusions_are_rejected()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new Filter(new Pattern("*.cs"), MatchTarget.Path, null!));
    }

    [Fact]
    public void Null_exclusion_is_rejected_by_WithExclusion()
    {
        var filter = CreateFilter(new Pattern("*.cs"), MatchTarget.Path);

        Assert.Throws<ArgumentNullException>(() => filter.WithExclusion(null!));
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
