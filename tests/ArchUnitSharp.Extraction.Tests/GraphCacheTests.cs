namespace ArchUnitSharp.Extraction.Tests;

using ArchUnitSharp.Common.Extraction;

public class GraphCacheTests
{
    public GraphCacheTests()
    {
        GraphCache.Clear();
    }

    private static ProjectLocation Location(TempProject project) =>
        new(project.Root, Path.Combine(project.Root, "App.sln"), null);

    [Fact]
    public void Get_returns_the_same_graph_for_the_same_inputs()
    {
        using var project = new TempProject();
        project.WriteFile("App.sln", "");
        project.WriteFile("src/App/Program.cs", "namespace App { public class Program { } }");

        Graph first = GraphCache.Get(Location(project));
        Graph second = GraphCache.Get(Location(project));

        Assert.Same(first, second);
    }

    [Fact]
    public void Get_does_not_reread_the_source_until_the_cache_is_bypassed()
    {
        using var project = new TempProject();
        project.WriteFile("App.sln", "");
        project.WriteFile("src/Models/Car.cs", "namespace App.Models { public class Car { } }");
        project.WriteFile("src/App/Program.cs", "using App.Models; namespace App { public class Program { } }");

        Graph cached = GraphCache.Get(Location(project));
        Assert.Single(cached.Edges, edge => edge.Source != edge.Target);

        project.WriteFile("src/App/Program.cs", "namespace App { public class Program { } }");

        Graph again = GraphCache.Get(Location(project));
        Assert.Same(cached, again);
        Assert.Single(again.Edges, edge => edge.Source != edge.Target);

        Graph refreshed = GraphCache.Get(Location(project), checkOptions: new CheckOptions { ClearCache = true });
        Assert.NotSame(cached, refreshed);
        Assert.DoesNotContain(refreshed.Edges, edge => edge.Source != edge.Target);

        Assert.Same(refreshed, GraphCache.Get(Location(project)));
    }

    [Fact]
    public void Clear_empties_the_cache_so_the_next_get_rebuilds()
    {
        using var project = new TempProject();
        project.WriteFile("App.sln", "");
        project.WriteFile("src/App/Program.cs", "using System; namespace App { public class Program { } }");

        Graph cached = GraphCache.Get(Location(project));

        GraphCache.Clear();

        project.WriteFile("src/App/Program.cs", "namespace App { public class Program { } }");
        Graph rebuilt = GraphCache.Get(Location(project));

        Assert.NotSame(cached, rebuilt);
        Assert.DoesNotContain(rebuilt.Edges, edge => edge.Source != edge.Target);
    }

    [Fact]
    public void Get_keys_on_the_project_location()
    {
        using var firstProject = new TempProject();
        firstProject.WriteFile("App.sln", "");
        firstProject.WriteFile("src/App/Program.cs", "using System; namespace App { public class Program { } }");

        using var secondProject = new TempProject();
        secondProject.WriteFile("App.sln", "");
        secondProject.WriteFile("src/App/Program.cs", "using System.Text; namespace App { public class Program { } }");

        Graph first = GraphCache.Get(Location(firstProject));
        Graph second = GraphCache.Get(Location(secondProject));

        Assert.NotSame(first, second);
        Assert.Contains(first.Edges, edge => edge.Target == "System");
        Assert.Contains(second.Edges, edge => edge.Target == "System.Text");
        Assert.Same(first, GraphCache.Get(Location(firstProject)));
        Assert.Same(second, GraphCache.Get(Location(secondProject)));
    }

    [Fact]
    public void Get_keys_on_the_excluded_directories()
    {
        using var project = new TempProject();
        project.WriteFile("App.sln", "");
        project.WriteFile("src/App/Program.cs", "namespace App { public class Program { } }");
        project.WriteFile("vendor/Lib.cs", "namespace Vendor { public class Lib { } }");

        Graph withDefaults = GraphCache.Get(Location(project));
        Graph withoutExclusions = GraphCache.Get(Location(project), new SourceEnumerationOptions(Array.Empty<string>()));

        Assert.NotSame(withDefaults, withoutExclusions);
        Assert.DoesNotContain(withDefaults.Edges, edge => edge.Source == "vendor/Lib.cs");
        Assert.Contains(withoutExclusions.Edges, edge => edge.Source == "vendor/Lib.cs" && edge.Target == "vendor/Lib.cs");
    }

    [Fact]
    public void Get_keys_on_the_analysis_toggles()
    {
        using var project = new TempProject();
        project.WriteFile("App.sln", "");
        project.WriteFile("src/App/Program.cs", "namespace App { public class Program { } }");
        project.WriteFile("src/tests/ProgramTests.cs", "namespace App.Tests { public class ProgramTests { } }");

        Graph includingTests = GraphCache.Get(Location(project));
        Graph ignoringTests = GraphCache.Get(Location(project), checkOptions: new CheckOptions { IgnoreTestCode = true });

        Assert.NotSame(includingTests, ignoringTests);
        Assert.Contains(includingTests.Edges, edge => edge.Source == "src/tests/ProgramTests.cs");
        Assert.DoesNotContain(ignoringTests.Edges, edge => edge.Source == "src/tests/ProgramTests.cs");
    }

    [Fact]
    public void Get_keys_on_the_ignore_generated_code_toggle()
    {
        using var project = new TempProject();
        project.WriteFile("App.sln", "");
        project.WriteFile("src/App/Program.cs", "namespace App { public class Program { } }");
        project.WriteFile("src/App/Designer.g.cs", "namespace App { public partial class Designer { } }");

        Graph includingGenerated = GraphCache.Get(Location(project));
        Graph ignoringGenerated = GraphCache.Get(Location(project), checkOptions: new CheckOptions { IgnoreGeneratedCode = true });

        Assert.NotSame(includingGenerated, ignoringGenerated);
        Assert.Contains(includingGenerated.Edges, edge => edge.Source == "src/App/Designer.g.cs");
        Assert.DoesNotContain(ignoringGenerated.Edges, edge => edge.Source == "src/App/Designer.g.cs");
    }

    [Fact]
    public void Get_rejects_a_null_location()
    {
        Assert.Throws<ArgumentNullException>(() => GraphCache.Get(null!));
    }

    [Fact]
    public void Get_throws_technical_error_when_the_project_root_does_not_exist()
    {
        var missing = new ProjectLocation("/nonexistent/root", "/nonexistent/root/App.sln", null);

        Assert.Throws<TechnicalError>(() => GraphCache.Get(missing));
    }
}
