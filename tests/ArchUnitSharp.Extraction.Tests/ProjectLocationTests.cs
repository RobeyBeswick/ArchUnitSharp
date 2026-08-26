namespace ArchUnitSharp.Extraction.Tests;

public class ProjectLocationTests
{
    [Fact]
    public void Constructor_stores_the_root_and_the_file_that_located_it()
    {
        var location = new ProjectLocation("/repo", "/repo/App.sln", null);

        Assert.Equal("/repo", location.Root);
        Assert.Equal("/repo/App.sln", location.SolutionFile);
        Assert.Null(location.ProjectFile);
    }

    [Fact]
    public void A_project_file_location_has_no_solution_file()
    {
        var location = new ProjectLocation("/repo", null, "/repo/App.csproj");

        Assert.Equal("/repo", location.Root);
        Assert.Null(location.SolutionFile);
        Assert.Equal("/repo/App.csproj", location.ProjectFile);
    }

    [Fact]
    public void Constructor_normalises_backslash_separated_paths()
    {
        var location = new ProjectLocation(@"C:\repo", @"C:\repo\App.sln", null);

        Assert.Equal("C:/repo", location.Root);
        Assert.Equal("C:/repo/App.sln", location.SolutionFile);
        Assert.Null(location.ProjectFile);
    }

    [Fact]
    public void A_with_expression_normalises_a_backslash_separated_path()
    {
        var location = new ProjectLocation("/repo", "/repo/App.sln", null);

        var child = location with { ProjectFile = @"C:\repo\App.csproj" };

        Assert.Equal("C:/repo/App.csproj", child.ProjectFile);
        Assert.Equal("/repo", location.Root);
        Assert.Equal("/repo/App.sln", location.SolutionFile);
    }

    [Fact]
    public void Two_locations_with_the_same_values_are_equal()
    {
        var left = new ProjectLocation("/repo", "/repo/App.sln", null);
        var right = new ProjectLocation("/repo", "/repo/App.sln", null);

        Assert.Equal(left, right);
        Assert.Equal(left.GetHashCode(), right.GetHashCode());
    }

    [Fact]
    public void Two_locations_with_different_roots_are_unequal()
    {
        var left = new ProjectLocation("/repo", "/repo/App.sln", null);
        var right = new ProjectLocation("/other", "/other/App.sln", null);

        Assert.NotEqual(left, right);
    }

    [Fact]
    public void Branching_off_one_parent_does_not_change_the_parent()
    {
        var parent = new ProjectLocation("/repo", "/repo/App.sln", null);

        var child = parent with { ProjectFile = "/repo/App.csproj" };

        Assert.Equal("/repo", parent.Root);
        Assert.Equal("/repo/App.sln", parent.SolutionFile);
        Assert.Null(parent.ProjectFile);
        Assert.Equal("/repo/App.csproj", child.ProjectFile);
    }

    [Fact]
    public void Null_root_is_rejected()
    {
        Assert.Throws<ArgumentNullException>(() => new ProjectLocation(null!, null, "/repo/App.csproj"));
    }

    [Fact]
    public void Empty_root_is_rejected()
    {
        Assert.Throws<ArgumentException>(() => new ProjectLocation(string.Empty, null, "/repo/App.csproj"));
    }

    [Fact]
    public void A_location_with_no_file_is_rejected()
    {
        Assert.Throws<ArgumentException>(() => new ProjectLocation("/repo", null, null));
    }

    [Fact]
    public void An_empty_solution_file_path_is_rejected_when_set()
    {
        Assert.Throws<ArgumentException>(() => new ProjectLocation("/repo", string.Empty, null));
    }

    [Fact]
    public void An_empty_project_file_path_is_rejected_when_set()
    {
        Assert.Throws<ArgumentException>(() => new ProjectLocation("/repo", null, string.Empty));
    }

    [Fact]
    public void A_with_expression_cannot_clear_the_solution_file()
    {
        var location = new ProjectLocation("/repo", "/repo/App.sln", null);

        Assert.Throws<ArgumentException>(() => location with { SolutionFile = null });
    }

    [Fact]
    public void A_with_expression_cannot_clear_the_project_file()
    {
        var location = new ProjectLocation("/repo", null, "/repo/App.csproj");

        Assert.Throws<ArgumentException>(() => location with { ProjectFile = null });
    }

    [Fact]
    public void Swapping_the_locating_file_is_rejected_regardless_of_initialiser_order()
    {
        var location = new ProjectLocation("/repo", "/repo/App.sln", null);

        Assert.Throws<ArgumentException>(() => location with { SolutionFile = null, ProjectFile = "/repo/App.csproj" });
        Assert.Throws<ArgumentException>(() => location with { ProjectFile = "/repo/App.csproj", SolutionFile = null });
    }

    [Fact]
    public void Adding_the_other_locating_file_is_independent_of_initialiser_order()
    {
        var location = new ProjectLocation("/repo", "/repo/App.sln", null);

        var forward = location with { SolutionFile = "/repo/App.sln", ProjectFile = "/repo/App.csproj" };
        var reversed = location with { ProjectFile = "/repo/App.csproj", SolutionFile = "/repo/App.sln" };

        Assert.Equal(forward, reversed);
        Assert.Equal("/repo/App.sln", forward.SolutionFile);
        Assert.Equal("/repo/App.csproj", forward.ProjectFile);
    }
}
