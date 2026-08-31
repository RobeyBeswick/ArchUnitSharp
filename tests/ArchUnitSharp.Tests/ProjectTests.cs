using ArchUnitSharp.Common.Extraction;
using ArchUnitSharp.Extraction;
using ArchUnitSharp.Files;
using ArchUnitSharp.Layers;
using ArchUnitSharp.Metrics;
using ArchUnitSharp.Slices;

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

    [Fact]
    public void ProjectFiles_should_depend_on_files_passes_when_the_dependency_exists()
    {
        using var project = new TempProject();
        project.WriteFile("App.sln", "");
        project.WriteFile("src/App/Program.cs", "using App.Models; namespace App { public class Program { } }");
        project.WriteFile("src/Models/Car.cs", "namespace App.Models { public class Car { } }");

        var location = ProjectLocator.Locate(project.Root);

        IReadOnlyList<Violation> violations = Project.ProjectFiles(location)
            .InFolder("src/App")
            .Should()
            .DependOn()
            .InFolder("src/Models")
            .Check();

        Assert.Empty(violations);
    }

    [Fact]
    public void ProjectFiles_should_not_depend_on_files_flags_each_forbidden_dependency()
    {
        using var project = new TempProject();
        project.WriteFile("App.sln", "");
        project.WriteFile("src/App/Program.cs", "using App.Models; namespace App { public class Program { } }");
        project.WriteFile("src/Models/Car.cs", "namespace App.Models { public class Car { } }");

        var location = ProjectLocator.Locate(project.Root);

        IReadOnlyList<Violation> violations = Project.ProjectFiles(location)
            .InFolder("src/App")
            .ShouldNot()
            .DependOn()
            .InFolder("src/Models")
            .Check();

        Assert.Equal(
            new Violation[] { new DependencyViolation("src/App/Program.cs", "src/Models/Car.cs") },
            violations);
    }

    [Fact]
    public void ProjectFiles_should_depend_on_external_modules_passes_when_the_dependency_exists()
    {
        using var project = new TempProject();
        project.WriteFile("App.sln", "");
        project.WriteFile("src/App/Program.cs", "using System.Linq; namespace App { public class Program { } }");
        project.WriteFile("src/Models/Car.cs", "namespace App.Models { public class Car { } }");

        var location = ProjectLocator.Locate(project.Root);

        IReadOnlyList<Violation> violations = Project.ProjectFiles(location)
            .InFolder("src/App")
            .Should()
            .DependOnExternalModules()
            .Matching("System.*")
            .Check();

        Assert.Empty(violations);
    }

    [Fact]
    public void ProjectFiles_should_not_depend_on_external_modules_flags_each_forbidden_dependency()
    {
        using var project = new TempProject();
        project.WriteFile("App.sln", "");
        project.WriteFile("src/App/Program.cs", "using System.Linq; namespace App { public class Program { } }");
        project.WriteFile("src/Models/Car.cs", "namespace App.Models { public class Car { } }");

        var location = ProjectLocator.Locate(project.Root);

        IReadOnlyList<Violation> violations = Project.ProjectFiles(location)
            .InFolder("src/App")
            .ShouldNot()
            .DependOnExternalModules()
            .Matching("System.*")
            .Check();

        Assert.Equal(
            new Violation[] { new DependencyViolation("src/App/Program.cs", "System.Linq") },
            violations);
    }

    [Fact]
    public void ProjectFiles_should_have_name_passes_when_every_file_matches()
    {
        using var project = new TempProject();
        project.WriteFile("App.sln", "");
        project.WriteFile("src/App/Program.cs", "namespace App { public class Program { } }");
        project.WriteFile("src/Models/Car.cs", "namespace App.Models { public class Car { } }");

        var location = ProjectLocator.Locate(project.Root);

        IReadOnlyList<Violation> violations = Project.ProjectFiles(location)
            .Should()
            .HaveName("*.cs")
            .Check();

        Assert.Empty(violations);
    }

    [Fact]
    public void ProjectFiles_should_be_in_folder_passes_when_every_file_is_in_the_folder()
    {
        using var project = new TempProject();
        project.WriteFile("App.sln", "");
        project.WriteFile("src/Models/Car.cs", "namespace App.Models { public class Car { } }");
        project.WriteFile("src/Models/Truck.cs", "namespace App.Models { public class Truck { } }");

        var location = ProjectLocator.Locate(project.Root);

        IReadOnlyList<Violation> violations = Project.ProjectFiles(location)
            .Should()
            .BeInFolder("src/Models")
            .Check();

        Assert.Empty(violations);
    }

    [Fact]
    public void ProjectFiles_should_be_in_path_flags_a_file_outside_the_path()
    {
        using var project = new TempProject();
        project.WriteFile("App.sln", "");
        project.WriteFile("src/App/Program.cs", "namespace App { public class Program { } }");
        project.WriteFile("src/Models/Car.cs", "namespace App.Models { public class Car { } }");

        var location = ProjectLocator.Locate(project.Root);

        IReadOnlyList<Violation> violations = Project.ProjectFiles(location)
            .Should()
            .BeInPath("src/Models/Car.cs")
            .Check();

        Assert.Equal(new[] { new FileViolation("src/App/Program.cs") }, violations);
    }

    [Fact]
    public void Files_should_not_have_name_flags_the_matching_files()
    {
        using var project = new TempProject();
        project.WriteFile("App.sln", "");
        project.WriteFile("src/App/Program.cs", "namespace App { public class Program { } }");
        project.WriteFile("src/Models/Car.cs", "namespace App.Models { public class Car { } }");

        var location = ProjectLocator.Locate(project.Root);

        IReadOnlyList<Violation> violations = Project.Files(location)
            .ShouldNot()
            .HaveName("Car.cs")
            .Check();

        Assert.Equal(new[] { new FileViolation("src/Models/Car.cs") }, violations);
    }

    [Fact]
    public void ProjectFiles_should_adhere_to_passes_when_every_file_satisfies_the_predicate()
    {
        using var project = new TempProject();
        project.WriteFile("App.sln", "");
        project.WriteFile("src/App/Program.cs", "namespace App { public class Program { } }");
        project.WriteFile("src/Models/Car.cs", "namespace App.Models { public class Car { } }");

        var location = ProjectLocator.Locate(project.Root);

        IReadOnlyList<Violation> violations = Project.ProjectFiles(location)
            .Should()
            .AdhereTo(static detail => detail.NonBlankLineCount <= 2, "every file is short")
            .Check();

        Assert.Empty(violations);
    }

    [Fact]
    public void ProjectFiles_should_adhere_to_flags_files_the_predicate_rejects()
    {
        using var project = new TempProject();
        project.WriteFile("App.sln", "");
        project.WriteFile("src/App/Program.cs", "using System;\nnamespace App { public class Program { } }");
        project.WriteFile("src/Models/Car.cs", "namespace App.Models { public class Car { } }");

        var location = ProjectLocator.Locate(project.Root);

        IReadOnlyList<Violation> violations = Project.ProjectFiles(location)
            .Should()
            .AdhereTo(static detail => detail.NonBlankLineCount <= 1, "every file is one line")
            .Check();

        Assert.Equal(
            new Violation[] { new AdhereToViolation("src/App/Program.cs", "every file is one line") },
            violations);
    }

    [Fact]
    public void ProjectFiles_should_not_adhere_to_flags_files_the_predicate_accepts()
    {
        using var project = new TempProject();
        project.WriteFile("App.sln", "");
        project.WriteFile("src/App/Program.cs", "using System;\nnamespace App { public class Program { } }");
        project.WriteFile("src/Models/Car.cs", "namespace App.Models { public class Car { } }");

        var location = ProjectLocator.Locate(project.Root);

        IReadOnlyList<Violation> violations = Project.ProjectFiles(location)
            .ShouldNot()
            .AdhereTo(static detail => detail.NonBlankLineCount > 1, "no file has more than one line")
            .Check();

        Assert.Equal(
            new Violation[] { new AdhereToViolation("src/App/Program.cs", "no file has more than one line") },
            violations);
    }

    [Fact]
    public void ProjectFiles_should_adhere_to_sees_the_full_source_text()
    {
        using var project = new TempProject();
        project.WriteFile("App.sln", "");
        project.WriteFile("src/App/Program.cs", "// archunit: marker\nnamespace App { public class Program { } }");
        project.WriteFile("src/Models/Car.cs", "namespace App.Models { public class Car { } }");

        var location = ProjectLocator.Locate(project.Root);

        IReadOnlyList<Violation> violations = Project.ProjectFiles(location)
            .Should()
            .AdhereTo(static detail => detail.SourceText.Contains("marker"), "every file carries the marker")
            .Check();

        Assert.Equal(
            new Violation[] { new AdhereToViolation("src/Models/Car.cs", "every file carries the marker") },
            violations);
    }

    [Fact]
    public void ProjectFiles_should_adhere_to_guards_a_selection_that_matches_nothing()
    {
        using var project = new TempProject();
        project.WriteFile("App.sln", "");
        project.WriteFile("src/App/Program.cs", "namespace App { public class Program { } }");

        var location = ProjectLocator.Locate(project.Root);

        IReadOnlyList<Violation> violations = Project.ProjectFiles(location)
            .WithName("Car.cs")
            .Should()
            .AdhereTo(static _ => true, "message")
            .Check();

        Assert.Equal(
            new Violation[] { new EmptyTestViolation("project files with name 'Car.cs' should adhere to 'message'") },
            violations);
    }

    [Fact]
    public void ProjectLayers_checks_a_named_layer_policy()
    {
        using var project = new TempProject();
        project.WriteFile("App.sln", "");
        project.WriteFile("src/App/Program.cs", "using App.Models; namespace App { public class Program { } }");
        project.WriteFile("src/Models/Car.cs", "namespace App.Models { public class Car { } }");

        var location = ProjectLocator.Locate(project.Root);

        IReadOnlyList<Violation> violations = Project.Layers(location)
            .Layer("App").DefinedByFolder("src/App")
            .Layer("Models").DefinedByFolder("src/Models")
            .WhereLayer("App").MayOnlyDependOnLayers("Models")
            .Check();

        Assert.Empty(violations);
    }

    [Fact]
    public void ProjectLayers_reports_a_forbidden_cross_layer_dependency()
    {
        using var project = new TempProject();
        project.WriteFile("App.sln", "");
        project.WriteFile("src/App/Program.cs", "using App.Infra; namespace App { public class Program { } }");
        project.WriteFile("src/Infra/Db.cs", "namespace App.Infra { public class Db { } }");

        var location = ProjectLocator.Locate(project.Root);

        IReadOnlyList<Violation> violations = Project.Layers(location)
            .Layer("App").DefinedByFolder("src/App")
            .Layer("Infra").DefinedByFolder("src/Infra")
            .WhereLayer("App").MayNotDependOnLayers("Infra")
            .Check();

        Assert.Equal(
            new Violation[] { new LayerViolation("App", "Infra", "src/App/Program.cs", "src/Infra/Db.cs") },
            violations);
    }

    [Fact]
    public void Layers_alias_returns_a_policy_like_project_layers()
    {
        using var project = new TempProject();
        project.WriteFile("App.sln", "");
        project.WriteFile("src/App/Program.cs", "using App.Models; namespace App { public class Program { } }");
        project.WriteFile("src/Models/Car.cs", "namespace App.Models { public class Car { } }");

        var location = ProjectLocator.Locate(project.Root);

        IReadOnlyList<Violation> canonical = Project.ProjectLayers(location)
            .Layer("App").DefinedByFolder("src/App")
            .Layer("Models").DefinedByFolder("src/Models")
            .WhereLayer("App").MayOnlyDependOnLayers("Models")
            .Check();
        IReadOnlyList<Violation> alias = Project.Layers(location)
            .Layer("App").DefinedByFolder("src/App")
            .Layer("Models").DefinedByFolder("src/Models")
            .WhereLayer("App").MayOnlyDependOnLayers("Models")
            .Check();

        Assert.Equal(canonical, alias);
    }

    [Fact]
    public void ProjectLayers_rejects_a_null_location()
    {
        Assert.Throws<ArgumentNullException>(() => Project.ProjectLayers(null!));
    }

    [Fact]
    public void Layers_rejects_a_null_location()
    {
        Assert.Throws<ArgumentNullException>(() => Project.Layers(null!));
    }

    [Fact]
    public void ProjectSlices_checks_a_sliced_dependency_rule()
    {
        using var project = new TempProject();
        project.WriteFile("App.sln", "");
        project.WriteFile("src/features/billing/order.cs", "using App.Legacy; namespace App.Features.Billing { public class Order { } }");
        project.WriteFile("src/legacy/Old.cs", "namespace App.Legacy { public class Old { } }");

        var location = ProjectLocator.Locate(project.Root);

        IReadOnlyList<Violation> violations = Project.ProjectSlices(location)
            .DefinedBy("src/features/(**)/*.cs")
            .ShouldNot()
            .ContainDependency("src/features/**", "src/legacy/**")
            .Check();

        Assert.Equal(
            new Violation[]
            {
                new ForbiddenDependencyViolation(
                    "billing",
                    "src/features/billing/order.cs",
                    "src/legacy/Old.cs"),
            },
            violations);
    }

    [Fact]
    public void ProjectSlices_checks_that_no_forbidden_dependency_exists()
    {
        using var project = new TempProject();
        project.WriteFile("App.sln", "");
        project.WriteFile("src/features/billing/order.cs", "namespace App.Features.Billing { public class Order { } }");
        project.WriteFile("src/legacy/Old.cs", "namespace App.Legacy { public class Old { } }");

        var location = ProjectLocator.Locate(project.Root);

        IReadOnlyList<Violation> violations = Project.ProjectSlices(location)
            .DefinedBy("src/features/(**)/*.cs")
            .ShouldNot()
            .ContainDependency("src/features/**", "src/legacy/**")
            .Check();

        Assert.Empty(violations);
    }

    [Fact]
    public void Slices_alias_returns_a_policy_like_project_slices()
    {
        using var project = new TempProject();
        project.WriteFile("App.sln", "");
        project.WriteFile("src/features/billing/order.cs", "using App.Legacy; namespace App.Features.Billing { public class Order { } }");
        project.WriteFile("src/legacy/Old.cs", "namespace App.Legacy { public class Old { } }");

        var location = ProjectLocator.Locate(project.Root);

        IReadOnlyList<Violation> canonical = Project.ProjectSlices(location)
            .DefinedBy("src/features/(**)/*.cs")
            .ShouldNot()
            .ContainDependency("src/features/**", "src/legacy/**")
            .Check();
        IReadOnlyList<Violation> alias = Project.Slices(location)
            .DefinedBy("src/features/(**)/*.cs")
            .ShouldNot()
            .ContainDependency("src/features/**", "src/legacy/**")
            .Check();

        Assert.Equal(canonical, alias);
    }

    [Fact]
    public void ProjectSlices_rejects_a_null_location()
    {
        Assert.Throws<ArgumentNullException>(() => Project.ProjectSlices(null!));
    }

    [Fact]
    public void Slices_rejects_a_null_location()
    {
        Assert.Throws<ArgumentNullException>(() => Project.Slices(null!));
    }

    [Fact]
    public void ProjectSlices_adheres_to_a_diagram_file()
    {
        using var project = new TempProject();
        project.WriteFile("App.sln", "");
        project.WriteFile("src/features/billing/order.cs", "using App.Shared; namespace App.Features.Billing { public class Order { } }");
        project.WriteFile("src/shared/Util.cs", "namespace App.Shared { public class Util { } }");
        project.WriteFile("docs/architecture.puml", "component [features/billing]\ncomponent [shared]\n[features/billing] --> [shared]");

        var location = ProjectLocator.Locate(project.Root);

        IReadOnlyList<Violation> violations = Project.ProjectSlices(location)
            .DefinedBy("src/(**)/*.cs")
            .Should()
            .AdhereToDiagramInFile(project.Root + "/docs/architecture.puml")
            .Check();

        Assert.Empty(violations);
    }

    [Fact]
    public void ProjectSlices_reports_a_dependency_a_diagram_file_does_not_allow()
    {
        using var project = new TempProject();
        project.WriteFile("App.sln", "");
        project.WriteFile("src/features/billing/order.cs", "using App.Shared; namespace App.Features.Billing { public class Order { } }");
        project.WriteFile("src/shared/Util.cs", "namespace App.Shared { public class Util { } }");
        project.WriteFile("docs/architecture.puml", "component [features/billing]\ncomponent [shared]");

        var location = ProjectLocator.Locate(project.Root);

        IReadOnlyList<Violation> violations = Project.ProjectSlices(location)
            .DefinedBy("src/(**)/*.cs")
            .Should()
            .AdhereToDiagramInFile(project.Root + "/docs/architecture.puml")
            .Check();

        Assert.Equal(
            new Violation[] { new DiagramAdherenceViolation("features/billing", "shared") },
            violations);
    }

    [Fact]
    public void ProjectGraph_renders_a_dot_report_of_the_project()
    {
        using var project = new TempProject();
        project.WriteFile("App.sln", "");
        project.WriteFile("src/App/Program.cs", "using App.Models; namespace App { public class Program { } }");
        project.WriteFile("src/Models/Car.cs", "namespace App.Models { public class Car { } }");

        var location = ProjectLocator.Locate(project.Root);

        string dot = Project.ProjectGraph(location).ToDot();

        Assert.Contains("  \"src/App/Program.cs\";", dot);
        Assert.Contains("  \"src/Models/Car.cs\";", dot);
        Assert.Contains("  \"src/App/Program.cs\" -> \"src/Models/Car.cs\";", dot);
    }

    [Fact]
    public void Graph_alias_renders_a_report_like_project_graph()
    {
        using var project = new TempProject();
        project.WriteFile("App.sln", "");
        project.WriteFile("src/App/Program.cs", "namespace App { public class Program { } }");

        var location = ProjectLocator.Locate(project.Root);

        string canonical = Project.ProjectGraph(location).ToDot();
        string alias = Project.Graph(location).ToDot();

        Assert.Equal(canonical, alias);
    }

    [Fact]
    public void ProjectGraph_can_export_every_format_to_disk()
    {
        using var project = new TempProject();
        project.WriteFile("App.sln", "");
        project.WriteFile("src/App/Program.cs", "namespace App { public class Program { } }");

        var location = ProjectLocator.Locate(project.Root);

        string dot = Project.ProjectGraph(location).ExportAsDot(project.Root + "/graph.dot");
        string html = Project.ProjectGraph(location).ExportAsHtml(project.Root + "/graph.html");

        Assert.StartsWith("digraph {", File.ReadAllText(dot));
        Assert.StartsWith("<!DOCTYPE html>", File.ReadAllText(html));
    }

    [Fact]
    public void ProjectGraph_rejects_a_null_location()
    {
        Assert.Throws<ArgumentNullException>(() => Project.ProjectGraph(null!));
    }

    [Fact]
    public void Graph_rejects_a_null_location()
    {
        Assert.Throws<ArgumentNullException>(() => Project.Graph(null!));
    }

    [Fact]
    public void ProjectMetrics_checks_a_count_metric_rule()
    {
        using var project = new TempProject();
        project.WriteFile("App.sln", "");
        project.WriteFile("src/App/Program.cs", "namespace App;\npublic class Program { public void A() { } public void B() { } }\n");

        var location = ProjectLocator.Locate(project.Root);

        IReadOnlyList<Violation> violations = Project.ProjectMetrics(location)
            .Count()
            .MethodCount()
            .ShouldBe(2)
            .Check();

        Assert.Empty(violations);
    }

    [Fact]
    public void ProjectMetrics_flags_a_class_whose_method_count_misses_the_threshold()
    {
        using var project = new TempProject();
        project.WriteFile("App.sln", "");
        project.WriteFile("src/App/Program.cs", "namespace App;\npublic class Program { public void A() { } public void B() { } }\n");

        var location = ProjectLocator.Locate(project.Root);

        IReadOnlyList<Violation> violations = Project.ProjectMetrics(location)
            .Count()
            .MethodCount()
            .ShouldBe(1)
            .Check();

        Assert.Equal(
            new Violation[]
            {
                new MetricViolation(
                    "src/App/Program.cs",
                    "App.Program",
                    CountMetricKind.MethodCount,
                    value: 2,
                    MetricComparison.Equal,
                    threshold: 1),
            },
            violations);
    }

    [Fact]
    public void ProjectMetrics_measures_the_file_level_counts_from_source()
    {
        using var project = new TempProject();
        project.WriteFile("App.sln", "");
        project.WriteFile("src/App/Program.cs", "using System;\nnamespace App { public class Program { } }\n");

        var location = ProjectLocator.Locate(project.Root);

        IReadOnlyList<Violation> violations = Project.ProjectMetrics(location)
            .Count()
            .Imports()
            .ShouldBe(1)
            .Check();

        Assert.Empty(violations);
    }

    [Fact]
    public void ProjectMetrics_with_selectors_narrows_the_scope()
    {
        using var project = new TempProject();
        project.WriteFile("App.sln", "");
        project.WriteFile("src/App/Program.cs", "namespace App;\npublic class Program { }\n");
        project.WriteFile("src/Models/Car.cs", "namespace App.Models;\npublic class Car { }\n");

        var location = ProjectLocator.Locate(project.Root);

        IReadOnlyList<string> models = Project.ProjectMetrics(location).InFolder("src/Models").SelectFiles();
        IReadOnlyList<string> cars = Project.ProjectMetrics(location).InFolder("src/Models").SelectFiles();

        Assert.Equal(new[] { "src/Models/Car.cs" }, models);
        Assert.Equal(models, cars);
    }

    [Fact]
    public void ProjectMetrics_guards_a_scope_that_matches_nothing()
    {
        using var project = new TempProject();
        project.WriteFile("App.sln", "");
        project.WriteFile("src/App/Program.cs", "namespace App;\npublic class Program { }\n");

        var location = ProjectLocator.Locate(project.Root);

        IReadOnlyList<Violation> violations = Project.ProjectMetrics(location)
            .WithName("Car.cs")
            .Count()
            .Classes()
            .ShouldBe(1)
            .Check();

        Assert.Equal(
            new Violation[] { new EmptyTestViolation("project metrics with name 'Car.cs' classes should be 1") },
            violations);
    }

    [Fact]
    public void Metrics_alias_returns_a_scope_like_project_metrics()
    {
        using var project = new TempProject();
        project.WriteFile("App.sln", "");
        project.WriteFile("src/App/Program.cs", "namespace App;\npublic class Program { public void A() { } }\n");

        var location = ProjectLocator.Locate(project.Root);

        IReadOnlyList<Violation> canonical = Project.ProjectMetrics(location)
            .Count()
            .MethodCount()
            .ShouldBe(1)
            .Check();
        IReadOnlyList<Violation> alias = Project.Metrics(location)
            .Count()
            .MethodCount()
            .ShouldBe(1)
            .Check();

        Assert.Equal(canonical, alias);
    }

    [Fact]
    public void ProjectMetrics_rejects_a_null_location()
    {
        Assert.Throws<ArgumentNullException>(() => Project.ProjectMetrics(null!));
    }

    [Fact]
    public void Metrics_rejects_a_null_location()
    {
        Assert.Throws<ArgumentNullException>(() => Project.Metrics(null!));
    }
}
