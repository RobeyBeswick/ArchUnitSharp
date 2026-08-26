using ArchUnitSharp.Common.Extraction;

namespace ArchUnitSharp.Extraction.Tests;

public class SourceEnumeratorTests
{
    private static ProjectLocation CreateLocation(TempProject project) =>
        new(project.Root, null, Path.Combine(project.Root, "Project.csproj"));

    [Fact]
    public void Enumerate_returns_the_cs_files_under_the_root_sorted_by_identifier()
    {
        using var project = new TempProject();
        project.WriteFile("src/zeta.cs");
        project.WriteFile("src/alpha.cs");
        project.WriteFile("top.cs");

        IReadOnlyList<SourceFile> files = SourceEnumerator.Enumerate(CreateLocation(project));

        Assert.Equal(
            new[] { "src/alpha.cs", "src/zeta.cs", "top.cs" },
            files.Select(file => file.Identifier));
    }

    [Fact]
    public void Enumerate_carries_the_absolute_path_of_each_file()
    {
        using var project = new TempProject();
        project.WriteFile("src/alpha.cs");

        IReadOnlyList<SourceFile> files = SourceEnumerator.Enumerate(CreateLocation(project));

        SourceFile file = Assert.Single(files);
        Assert.Equal("src/alpha.cs", file.Identifier);
        Assert.Equal(Normalise(Path.Combine(project.Root, "src", "alpha.cs")), file.AbsolutePath);
    }

    [Fact]
    public void Enumerate_ignores_non_cs_files()
    {
        using var project = new TempProject();
        project.WriteFile("src/alpha.cs");
        project.WriteFile("src/note.txt");
        project.WriteFile("README.md");

        IReadOnlyList<SourceFile> files = SourceEnumerator.Enumerate(CreateLocation(project));

        Assert.Equal(new[] { "src/alpha.cs" }, files.Select(file => file.Identifier));
    }

    [Fact]
    public void Enumerate_skips_the_default_excluded_directories_at_any_depth()
    {
        using var project = new TempProject();
        project.WriteFile("src/kept.cs");
        foreach (string name in SourceEnumerationOptions.DefaultExcludedDirectories)
        {
            project.WriteFile($"{name}/hidden.cs");
        }

        IReadOnlyList<SourceFile> files = SourceEnumerator.Enumerate(CreateLocation(project));

        Assert.Equal(new[] { "src/kept.cs" }, files.Select(file => file.Identifier));
    }

    [Fact]
    public void Enumerate_skips_an_excluded_directory_nested_under_source()
    {
        using var project = new TempProject();
        project.WriteFile("src/kept.cs");
        project.WriteFile("src/bin/nested.cs");
        project.WriteFile("src/obj/deeper/nested.cs");

        IReadOnlyList<SourceFile> files = SourceEnumerator.Enumerate(CreateLocation(project));

        Assert.Equal(new[] { "src/kept.cs" }, files.Select(file => file.Identifier));
    }

    [Fact]
    public void Enumerate_matches_excluded_directory_names_case_insensitively()
    {
        using var project = new TempProject();
        project.WriteFile("src/kept.cs");
        project.WriteFile("BIN/hidden.cs");
        project.WriteFile("Obj/hidden.cs");

        IReadOnlyList<SourceFile> files = SourceEnumerator.Enumerate(CreateLocation(project));

        Assert.Equal(new[] { "src/kept.cs" }, files.Select(file => file.Identifier));
    }

    [Fact]
    public void Enumerate_walks_a_dot_directory_not_in_the_exclusion_set()
    {
        using var project = new TempProject();
        project.WriteFile(".config/settings.cs");
        project.WriteFile("src/kept.cs");

        IReadOnlyList<SourceFile> files = SourceEnumerator.Enumerate(CreateLocation(project));

        Assert.Equal(
            new[] { ".config/settings.cs", "src/kept.cs" },
            files.Select(file => file.Identifier));
    }

    [Fact]
    public void Enumerate_skips_custom_excluded_directories()
    {
        using var project = new TempProject();
        project.WriteFile("src/kept.cs");
        project.WriteFile("generated/hidden.cs");
        var options = new SourceEnumerationOptions(new[] { "bin", "obj", "generated" });

        IReadOnlyList<SourceFile> files = SourceEnumerator.Enumerate(CreateLocation(project), options);

        Assert.Equal(new[] { "src/kept.cs" }, files.Select(file => file.Identifier));
    }

    [Fact]
    public void Enumerate_does_not_descend_into_a_directory_symlink_that_loops_to_an_ancestor()
    {
        using var project = new TempProject();
        project.WriteFile("src/kept.cs");
        if (!project.TryCreateDirectoryLink("src/loop", project.Root))
        {
            return;
        }

        IReadOnlyList<SourceFile> files = SourceEnumerator.Enumerate(CreateLocation(project));

        Assert.Equal(new[] { "src/kept.cs" }, files.Select(file => file.Identifier));
    }

    [Fact]
    public void Enumerate_does_not_pull_files_from_a_directory_symlink_outside_the_project()
    {
        using var project = new TempProject();
        string outside = Path.Combine(Path.GetTempPath(), "ArchUnitSharp.Extraction.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(outside);
        try
        {
            File.WriteAllText(Path.Combine(outside, "foreign.cs"), "");
            project.WriteFile("src/kept.cs");
            if (!project.TryCreateDirectoryLink("src/outside", outside))
            {
                return;
            }

            IReadOnlyList<SourceFile> files = SourceEnumerator.Enumerate(CreateLocation(project));

            Assert.Equal(new[] { "src/kept.cs" }, files.Select(file => file.Identifier));
        }
        finally
        {
            Directory.Delete(outside, recursive: true);
        }
    }

    [Fact]
    public void Enumerate_does_not_pull_a_file_symlink_outside_the_project()
    {
        using var project = new TempProject();
        string outside = Path.Combine(Path.GetTempPath(), "ArchUnitSharp.Extraction.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(outside);
        try
        {
            File.WriteAllText(Path.Combine(outside, "foreign.cs"), "");
            project.WriteFile("src/kept.cs");
            if (!project.TryCreateFileLink("src/outside.cs", Path.Combine(outside, "foreign.cs")))
            {
                return;
            }

            IReadOnlyList<SourceFile> files = SourceEnumerator.Enumerate(CreateLocation(project));

            Assert.Equal(new[] { "src/kept.cs" }, files.Select(file => file.Identifier));
        }
        finally
        {
            Directory.Delete(outside, recursive: true);
        }
    }

    [Fact]
    public void Locate_then_enumerate_walks_the_located_project()
    {
        using var project = new TempProject();
        project.WriteFile("App.sln");
        project.WriteFile("src/alpha.cs");

        ProjectLocation location = ProjectLocator.Locate(project.Root);
        IReadOnlyList<SourceFile> files = SourceEnumerator.Enumerate(location);

        Assert.Equal(new[] { "src/alpha.cs" }, files.Select(file => file.Identifier));
    }

    [Fact]
    public void Enumerate_returns_a_fresh_list_on_every_call()
    {
        using var project = new TempProject();
        project.WriteFile("src/alpha.cs");

        IReadOnlyList<SourceFile> first = SourceEnumerator.Enumerate(CreateLocation(project));
        IReadOnlyList<SourceFile> second = SourceEnumerator.Enumerate(CreateLocation(project));

        Assert.NotSame(first, second);
    }

    [Fact]
    public void Mutating_a_returned_list_does_not_corrupt_a_later_call()
    {
        using var project = new TempProject();
        project.WriteFile("src/alpha.cs");

        IReadOnlyList<SourceFile> returned = SourceEnumerator.Enumerate(CreateLocation(project));
        ((SourceFile[])returned)[0] = new SourceFile("evil.cs", "/evil.cs");

        IReadOnlyList<SourceFile> again = SourceEnumerator.Enumerate(CreateLocation(project));

        Assert.Equal(new[] { "src/alpha.cs" }, again.Select(file => file.Identifier));
    }

    [Fact]
    public void Enumerate_rejects_a_null_location()
    {
        Assert.Throws<ArgumentNullException>(() => SourceEnumerator.Enumerate(null!));
    }

    [Fact]
    public void Enumerate_throws_technical_error_for_a_non_existent_root()
    {
        using var project = new TempProject();
        string missing = Path.Combine(project.Root, "missing");

        Assert.Throws<TechnicalError>(() =>
            SourceEnumerator.Enumerate(new ProjectLocation(missing, null, Path.Combine(missing, "App.csproj"))));
    }

    private static string Normalise(string path) => path.Replace('\\', '/');
}
