using ArchUnitSharp.Common.Extraction;
using ArchUnitSharp.Extraction;
using ArchUnitSharp.Files;

namespace ArchUnitSharp.Tests;

public class ProjectTests
{
    [Fact]
    public void ProjectFiles_returns_every_file_of_the_given_project()
    {
        using var project = new TempProject();
        project.WriteFile("App.sln", "");
        project.WriteFile("src/App/Program.cs", "namespace App { public class Program { } }");
        project.WriteFile("src/Models/Car.cs", "namespace App.Models { public class Car { } }");

        var location = ProjectLocator.Locate(project.Root);

        IReadOnlyList<string> files = Project.ProjectFiles(location).Select();

        Assert.Equal(new[] { "src/App/Program.cs", "src/Models/Car.cs" }, files);
    }

    [Fact]
    public void ProjectFiles_with_selectors_narrows_the_selection()
    {
        using var project = new TempProject();
        project.WriteFile("App.sln", "");
        project.WriteFile("src/App/Program.cs", "namespace App { public class Program { } }");
        project.WriteFile("src/Models/Car.cs", "namespace App.Models { public class Car { } }");
        project.WriteFile("src/Models/Truck.cs", "namespace App.Models { public class Truck { } }");

        var location = ProjectLocator.Locate(project.Root);

        var cars = Project.ProjectFiles(location).WithName("Car.cs");
        var models = Project.ProjectFiles(location).InFolder("src/Models");

        Assert.Equal(new[] { "src/Models/Car.cs" }, cars.Select());
        Assert.Equal(new[] { "src/Models/Car.cs", "src/Models/Truck.cs" }, models.Select());
    }

    [Fact]
    public void Files_alias_selects_the_same_files_as_project_files()
    {
        using var project = new TempProject();
        project.WriteFile("App.sln", "");
        project.WriteFile("src/App/Program.cs", "namespace App { public class Program { } }");

        var location = ProjectLocator.Locate(project.Root);

        IReadOnlyList<string> canonical = Project.ProjectFiles(location).Select();
        IReadOnlyList<string> alias = Project.Files(location).Select();

        Assert.Equal(canonical, alias);
    }

    [Fact]
    public void ProjectFiles_without_arguments_locates_from_the_current_working_directory()
    {
        using var project = new TempProject();
        project.WriteFile("App.sln", "");
        project.WriteFile("src/App/Program.cs", "namespace App { public class Program { } }");

        string original = Environment.CurrentDirectory;
        try
        {
            Environment.CurrentDirectory = project.Root;

            IReadOnlyList<string> files = Project.ProjectFiles().Select();
            IReadOnlyList<string> alias = Project.Files().Select();

            Assert.Equal(new[] { "src/App/Program.cs" }, files);
            Assert.Equal(files, alias);
        }
        finally
        {
            Environment.CurrentDirectory = original;
        }
    }

    [Fact]
    public void ProjectFiles_rejects_a_null_location()
    {
        Assert.Throws<ArgumentNullException>(() => Project.ProjectFiles(null!));
    }

    [Fact]
    public void Files_rejects_a_null_location()
    {
        Assert.Throws<ArgumentNullException>(() => Project.Files(null!));
    }

    [Fact]
    public void ProjectFiles_throws_technical_error_when_the_project_cannot_be_read()
    {
        var missing = new ProjectLocation("/nonexistent/root", "/nonexistent/root/App.sln", null);

        Assert.Throws<TechnicalError>(() => Project.ProjectFiles(missing));
    }

    [Fact]
    public void ProjectFiles_should_exist_passes_for_a_real_project()
    {
        using var project = new TempProject();
        project.WriteFile("App.sln", "");
        project.WriteFile("src/App/Program.cs", "namespace App { public class Program { } }");
        project.WriteFile("src/Models/Car.cs", "namespace App.Models { public class Car { } }");

        var location = ProjectLocator.Locate(project.Root);

        IReadOnlyList<Violation> violations = Project.ProjectFiles(location).Should().Exist().Check();

        Assert.Empty(violations);
    }

    [Fact]
    public void Files_should_not_exist_flags_the_matching_files()
    {
        using var project = new TempProject();
        project.WriteFile("App.sln", "");
        project.WriteFile("src/App/Program.cs", "namespace App { public class Program { } }");
        project.WriteFile("src/Models/Car.cs", "namespace App.Models { public class Car { } }");

        var location = ProjectLocator.Locate(project.Root);

        IReadOnlyList<Violation> violations = Project.Files(location)
            .InFolder("src/Models")
            .ShouldNot()
            .Exist()
            .Check();

        Assert.Equal(new[] { new FileViolation("src/Models/Car.cs") }, violations);
    }

    [Fact]
    public void Files_should_exist_guards_a_selection_that_matches_nothing()
    {
        using var project = new TempProject();
        project.WriteFile("App.sln", "");
        project.WriteFile("src/App/Program.cs", "namespace App { public class Program { } }");

        var location = ProjectLocator.Locate(project.Root);

        IReadOnlyList<Violation> violations = Project.ProjectFiles(location)
            .WithName("Car.cs")
            .Should()
            .Exist()
            .Check();

        Assert.Equal(
            new Violation[] { new EmptyTestViolation("project files with name 'Car.cs' should exist") },
            violations);
    }

    [Fact]
    public void ProjectFiles_should_have_no_cycles_passes_for_an_acyclic_project()
    {
        using var project = new TempProject();
        project.WriteFile("App.sln", "");
        project.WriteFile("src/A.cs", "namespace App { public class A { } }");
        project.WriteFile("src/B.cs", "using App; namespace Models.Car { public class B { } }");

        var location = ProjectLocator.Locate(project.Root);

        IReadOnlyList<Violation> violations = Project.ProjectFiles(location)
            .Should()
            .HaveNoCycles()
            .Check();

        Assert.Empty(violations);
    }

    [Fact]
    public void ProjectFiles_should_have_no_cycles_flags_each_cycle_as_a_readable_path()
    {
        using var project = new TempProject();
        project.WriteFile("App.sln", "");
        project.WriteFile("src/A.cs", "using Models.Car; namespace App { public class A { } }");
        project.WriteFile("src/B.cs", "using App; namespace Models.Car { public class B { } }");

        var location = ProjectLocator.Locate(project.Root);

        IReadOnlyList<Violation> violations = Project.ProjectFiles(location)
            .Should()
            .HaveNoCycles()
            .Check();

        var cycle = Assert.Single(violations);
        Assert.Equal("src/A.cs → src/B.cs → src/A.cs", Assert.IsType<CycleViolation>(cycle).Path);
    }
}
