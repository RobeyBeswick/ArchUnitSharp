namespace ArchUnitSharp.Extraction.Tests;

using ArchUnitSharp.Common.Extraction;

public class ImportExtractorTests
{
    private static IReadOnlyList<SourceFile> Enumerate(TempProject project)
    {
        var location = new ProjectLocation(project.Root, null, Path.Combine(project.Root, "App.csproj"));
        return SourceEnumerator.Enumerate(location);
    }

    [Fact]
    public void Extract_reads_files_and_returns_their_import_edges()
    {
        using var project = new TempProject();
        project.WriteFile("src/Models/Car.cs", "namespace MyApp.Models { public class Car { } }");
        project.WriteFile("src/App/Program.cs", "using MyApp.Models; namespace MyApp.App { public class Program { } }");

        IReadOnlyList<Edge> edges = ImportExtractor.Extract(Enumerate(project));

        Edge edge = Assert.Single(edges);
        Assert.Equal("src/App/Program.cs", edge.Source);
        Assert.Equal("src/Models/Car.cs", edge.Target);
        Assert.False(edge.External);
        Assert.Equal(ImportKind.Using, edge.ImportKinds);
    }

    [Fact]
    public void Extract_keeps_unresolved_directives_as_external_edges()
    {
        using var project = new TempProject();
        project.WriteFile("src/App/Program.cs", "using System; namespace MyApp.App { public class Program { } }");

        IReadOnlyList<Edge> edges = ImportExtractor.Extract(Enumerate(project));

        Edge edge = Assert.Single(edges);
        Assert.Equal("System", edge.Target);
        Assert.True(edge.External);
    }

    [Fact]
    public void Extract_skips_a_file_that_fails_to_parse_without_failing()
    {
        using var project = new TempProject();
        project.WriteFile("src/Broken/Broken.cs", "namespace MyApp.Models { public class Car { ");
        project.WriteFile("src/App/Program.cs", "using MyApp.Models; namespace MyApp.App { public class Program { } }");

        IReadOnlyList<Edge> edges = ImportExtractor.Extract(Enumerate(project));

        Edge edge = Assert.Single(edges);
        Assert.Equal("MyApp.Models", edge.Target);
        Assert.True(edge.External);
    }

    [Fact]
    public void Extract_returns_no_edges_for_a_project_with_no_usings()
    {
        using var project = new TempProject();
        project.WriteFile("src/Models/Car.cs", "namespace MyApp.Models { public class Car { } }");

        Assert.Empty(ImportExtractor.Extract(Enumerate(project)));
    }

    [Fact]
    public void Extract_throws_technical_error_when_a_file_cannot_be_read()
    {
        var missing = new SourceFile("src/missing.cs", "/nonexistent/src/missing.cs");

        TechnicalError error = Assert.Throws<TechnicalError>(() => ImportExtractor.Extract(new[] { missing }));

        Assert.Contains("src/missing.cs", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Extract_rejects_a_null_file_list()
    {
        Assert.Throws<ArgumentNullException>(() => ImportExtractor.Extract(null!));
    }
}
