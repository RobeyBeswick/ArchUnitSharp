using ArchUnitSharp.Common.Extraction;
using ArchUnitSharp.Extraction;
using ArchUnitSharp.Layers;

namespace ArchUnitSharp.Tests;

[Collection("cwd")]
public class ProjectLayersTests
{
    [Fact]
    public void Project_layers_may_not_depend_on_flags_each_forbidden_dependency()
    {
        using var project = new TempProject();
        project.WriteFile("App.sln", "");
        project.WriteFile("src/Models/Car.cs", "namespace App.Models { public class Car { } }");
        project.WriteFile(
            "src/Services/CarService.cs",
            "using App.Models; namespace App.Services { public class CarService { } }");

        var location = ProjectLocator.Locate(project.Root);

        IReadOnlyList<Violation> violations = Project.ProjectLayers(location)
            .Layer("Models").DefinedByFolder("src/Models")
            .Layer("Services").DefinedByFolder("src/Services")
            .WhereLayer("Services")
            .MayNotDependOnLayers("Models")
            .Check();

        Assert.Equal(
            new Violation[]
            {
                new LayerViolation(
                    "Services",
                    "src/Services/CarService.cs",
                    "src/Models/Car.cs",
                    "Models"),
            },
            violations);
    }

    [Fact]
    public void Project_layers_may_only_depend_on_passes_when_every_dependency_is_allowed()
    {
        using var project = new TempProject();
        project.WriteFile("App.sln", "");
        project.WriteFile("src/Models/Car.cs", "namespace App.Models { public class Car { } }");
        project.WriteFile(
            "src/Services/CarService.cs",
            "using App.Models; namespace App.Services { public class CarService { } }");

        var location = ProjectLocator.Locate(project.Root);

        IReadOnlyList<Violation> violations = Project.ProjectLayers(location)
            .Layer("Models").DefinedByFolder("src/Models")
            .Layer("Services").DefinedByFolder("src/Services")
            .WhereLayer("Services")
            .MayOnlyDependOnLayers("Models")
            .Check();

        Assert.Empty(violations);
    }

    [Fact]
    public void Project_layers_may_only_depend_on_with_no_arguments_seals_the_layer()
    {
        using var project = new TempProject();
        project.WriteFile("App.sln", "");
        project.WriteFile("src/Models/Car.cs", "namespace App.Models { public class Car { } }");
        project.WriteFile(
            "src/Services/CarService.cs",
            "using App.Models; namespace App.Services { public class CarService { } }");

        var location = ProjectLocator.Locate(project.Root);

        IReadOnlyList<Violation> violations = Project.ProjectLayers(location)
            .Layer("Models").DefinedByFolder("src/Models")
            .Layer("Services").DefinedByFolder("src/Services")
            .WhereLayer("Services")
            .MayOnlyDependOnLayers()
            .Check();

        Assert.Equal(
            new Violation[]
            {
                new LayerViolation(
                    "Services",
                    "src/Services/CarService.cs",
                    "src/Models/Car.cs",
                    "Models"),
            },
            violations);
    }

    [Fact]
    public void Layers_alias_builds_the_same_rule_as_project_layers()
    {
        using var project = new TempProject();
        project.WriteFile("App.sln", "");
        project.WriteFile("src/Models/Car.cs", "namespace App.Models { public class Car { } }");
        project.WriteFile(
            "src/Services/CarService.cs",
            "using App.Models; namespace App.Services { public class CarService { } }");

        var location = ProjectLocator.Locate(project.Root);

        IReadOnlyList<Violation> canonical = Project.ProjectLayers(location)
            .Layer("Models").DefinedByFolder("src/Models")
            .Layer("Services").DefinedByFolder("src/Services")
            .WhereLayer("Services")
            .MayNotDependOnLayers("Models")
            .Check();
        IReadOnlyList<Violation> alias = Project.Layers(location)
            .Layer("Models").DefinedByFolder("src/Models")
            .Layer("Services").DefinedByFolder("src/Services")
            .WhereLayer("Services")
            .MayNotDependOnLayers("Models")
            .Check();

        Assert.Equal(canonical, alias);
        Assert.NotEmpty(alias);
    }

    [Fact]
    public void Project_layers_without_arguments_locates_from_the_current_working_directory()
    {
        using var project = new TempProject();
        project.WriteFile("App.sln", "");
        project.WriteFile("src/Models/Car.cs", "namespace App.Models { public class Car { } }");
        project.WriteFile(
            "src/Services/CarService.cs",
            "using App.Models; namespace App.Services { public class CarService { } }");

        string original = Environment.CurrentDirectory;
        try
        {
            Environment.CurrentDirectory = project.Root;

            IReadOnlyList<Violation> canonical = Project.ProjectLayers()
                .Layer("Models").DefinedByFolder("src/Models")
                .Layer("Services").DefinedByFolder("src/Services")
                .WhereLayer("Services")
                .MayNotDependOnLayers("Models")
                .Check();
            IReadOnlyList<Violation> alias = Project.Layers()
                .Layer("Models").DefinedByFolder("src/Models")
                .Layer("Services").DefinedByFolder("src/Services")
                .WhereLayer("Services")
                .MayNotDependOnLayers("Models")
                .Check();

            Assert.Equal(canonical, alias);
            Assert.NotEmpty(alias);
        }
        finally
        {
            Environment.CurrentDirectory = original;
        }
    }

    [Fact]
    public void Project_layers_rejects_a_null_location()
    {
        Assert.Throws<ArgumentNullException>(() => Project.ProjectLayers(null!));
    }

    [Fact]
    public void Layers_rejects_a_null_location()
    {
        Assert.Throws<ArgumentNullException>(() => Project.Layers(null!));
    }

    [Fact]
    public void Project_layers_throws_technical_error_when_the_project_cannot_be_read()
    {
        var missing = new ProjectLocation("/nonexistent/root", "/nonexistent/root/App.sln", null);

        Assert.Throws<TechnicalError>(() => Project.ProjectLayers(missing));
    }
}
