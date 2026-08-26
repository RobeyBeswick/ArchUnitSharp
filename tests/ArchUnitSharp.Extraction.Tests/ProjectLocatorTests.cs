using ArchUnitSharp.Common.Extraction;

namespace ArchUnitSharp.Extraction.Tests;

public class ProjectLocatorTests
{
    private static string Normalise(string path) => path.Replace('\\', '/');

    [Fact]
    public void Locate_finds_an_ancestor_containing_the_solution()
    {
        using var project = new TempProject();
        project.WriteFile("App.sln", "");
        string start = project.CreateDirectory("src/App");

        ProjectLocation location = ProjectLocator.Locate(start);

        Assert.Equal(Normalise(project.Root), location.Root);
        Assert.Equal(Normalise(Path.Combine(project.Root, "App.sln")), location.SolutionFile);
        Assert.Null(location.ProjectFile);
    }

    [Fact]
    public void Locate_returns_the_start_directory_when_it_is_the_project_root()
    {
        using var project = new TempProject();
        project.WriteFile("App.sln", "");

        ProjectLocation location = ProjectLocator.Locate(project.Root);

        Assert.Equal(Normalise(project.Root), location.Root);
        Assert.Equal(Normalise(Path.Combine(project.Root, "App.sln")), location.SolutionFile);
    }

    [Fact]
    public void Locate_prefers_a_solution_over_a_project_file_at_the_same_level()
    {
        using var project = new TempProject();
        project.WriteFile("App.sln", "");
        project.WriteFile("App.csproj", "");

        ProjectLocation location = ProjectLocator.Locate(project.Root);

        Assert.Equal(Normalise(project.Root), location.Root);
        Assert.Equal(Normalise(Path.Combine(project.Root, "App.sln")), location.SolutionFile);
        Assert.Null(location.ProjectFile);
    }

    [Fact]
    public void Locate_prefers_a_solution_in_an_ancestor_over_a_nearer_project_file()
    {
        using var project = new TempProject();
        project.WriteFile("App.sln", "");
        project.WriteFile("src/App/App.csproj", "");

        ProjectLocation location = ProjectLocator.Locate(Path.Combine(project.Root, "src", "App"));

        Assert.Equal(Normalise(project.Root), location.Root);
        Assert.Equal(Normalise(Path.Combine(project.Root, "App.sln")), location.SolutionFile);
        Assert.Null(location.ProjectFile);
    }

    [Fact]
    public void Locate_does_not_let_a_sibling_solution_win_over_the_ancestor()
    {
        using var project = new TempProject();
        project.WriteFile("App.sln", "");
        project.WriteFile("sibling/Sibling.sln", "");
        string start = project.CreateDirectory("src/App");

        ProjectLocation location = ProjectLocator.Locate(start);

        Assert.Equal(Normalise(project.Root), location.Root);
        Assert.Equal(Normalise(Path.Combine(project.Root, "App.sln")), location.SolutionFile);
    }

    [Fact]
    public void Locate_falls_back_to_a_project_file_when_no_solution_exists()
    {
        using var project = new TempProject();
        project.WriteFile("src/App/App.csproj", "");
        string start = project.CreateDirectory("src/App/deep");

        ProjectLocation location = ProjectLocator.Locate(start);

        Assert.Equal(Normalise(Path.Combine(project.Root, "src", "App")), location.Root);
        Assert.Equal(Normalise(Path.Combine(project.Root, "src", "App", "App.csproj")), location.ProjectFile);
        Assert.Null(location.SolutionFile);
    }

    [Fact]
    public void Locate_from_a_file_starts_from_that_files_directory()
    {
        using var project = new TempProject();
        project.WriteFile("App.sln", "");
        string file = project.WriteFile("src/App/Program.cs", "");

        ProjectLocation location = ProjectLocator.Locate(file);

        Assert.Equal(Normalise(project.Root), location.Root);
    }

    [Fact]
    public void Locate_picks_the_ordinally_first_solution_when_several_exist()
    {
        using var project = new TempProject();
        project.WriteFile("Beta.sln", "");
        project.WriteFile("Alpha.sln", "");

        ProjectLocation location = ProjectLocator.Locate(project.Root);

        Assert.Equal(Normalise(Path.Combine(project.Root, "Alpha.sln")), location.SolutionFile);
    }

    [Fact]
    public void Locate_throws_technical_error_when_no_project_file_exists()
    {
        using var project = new TempProject();
        project.CreateDirectory("src");

        TechnicalError error = Assert.Throws<TechnicalError>(() => ProjectLocator.Locate(project.Root));

        Assert.Contains("no .sln or .csproj", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Locate_throws_technical_error_for_a_non_existent_start_path()
    {
        using var project = new TempProject();

        Assert.Throws<TechnicalError>(() => ProjectLocator.Locate(Path.Combine(project.Root, "missing")));
    }

    [Fact]
    public void Locate_without_arguments_uses_the_current_working_directory()
    {
        using var project = new TempProject();
        project.WriteFile("App.sln", "");

        string original = Environment.CurrentDirectory;
        try
        {
            Environment.CurrentDirectory = project.Root;
            string cwd = Environment.CurrentDirectory;

            ProjectLocation location = ProjectLocator.Locate();

            Assert.Equal(Normalise(cwd), location.Root);
            Assert.Equal(Normalise(Path.Combine(cwd, "App.sln")), location.SolutionFile);
        }
        finally
        {
            Environment.CurrentDirectory = original;
        }
    }

    [Fact]
    public void Locate_rejects_a_null_start_directory()
    {
        Assert.Throws<ArgumentNullException>(() => ProjectLocator.Locate(null!));
    }

    [Fact]
    public void Locate_rejects_an_empty_start_directory()
    {
        Assert.Throws<ArgumentException>(() => ProjectLocator.Locate(string.Empty));
    }
}
