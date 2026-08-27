namespace ArchUnitSharp.Extraction.Tests;

public class SourceFileFilterTests
{
    private static SourceFile File(string identifier) => new(identifier, "/project/" + identifier);

    [Fact]
    public void Apply_keeps_every_file_when_no_toggle_is_set()
    {
        SourceFile[] files =
        {
            File("src/App/Program.cs"),
            File("src/tests/ProgramTests.cs"),
            File("src/App/Form.designer.cs"),
        };

        IReadOnlyList<SourceFile> kept = SourceFileFilter.Apply(
            files,
            ignoreTestCode: false,
            ignoreGeneratedCode: false);

        Assert.Equal(files, kept);
    }

    [Fact]
    public void Apply_excludes_files_in_test_folders_when_ignore_test_code_is_set()
    {
        SourceFile[] files =
        {
            File("src/App/Program.cs"),
            File("src/tests/ProgramTests.cs"),
            File("test/Fixtures.cs"),
        };

        IReadOnlyList<SourceFile> kept = SourceFileFilter.Apply(
            files,
            ignoreTestCode: true,
            ignoreGeneratedCode: false);

        Assert.Equal(new[] { files[0] }, kept);
    }

    [Fact]
    public void Apply_does_not_exclude_a_file_named_test_when_ignore_test_code_is_set()
    {
        SourceFile[] files =
        {
            File("src/App/test.cs"),
            File("src/App/Program.cs"),
        };

        IReadOnlyList<SourceFile> kept = SourceFileFilter.Apply(
            files,
            ignoreTestCode: true,
            ignoreGeneratedCode: false);

        Assert.Equal(files, kept);
    }

    [Fact]
    public void Apply_excludes_generated_files_when_ignore_generated_code_is_set()
    {
        SourceFile[] files =
        {
            File("src/App/Program.cs"),
            File("src/App/Designer.g.cs"),
            File("src/App/Form.designer.cs"),
        };

        IReadOnlyList<SourceFile> kept = SourceFileFilter.Apply(
            files,
            ignoreTestCode: false,
            ignoreGeneratedCode: true);

        Assert.Equal(new[] { files[0] }, kept);
    }

    [Fact]
    public void Apply_excludes_a_file_that_is_both_when_either_toggle_is_set()
    {
        SourceFile[] files =
        {
            File("src/tests/Fixtures.g.cs"),
            File("src/App/Program.cs"),
        };

        IReadOnlyList<SourceFile> kept = SourceFileFilter.Apply(
            files,
            ignoreTestCode: true,
            ignoreGeneratedCode: true);

        Assert.Equal(new[] { files[1] }, kept);
    }

    [Fact]
    public void Apply_preserves_the_input_order()
    {
        SourceFile[] files =
        {
            File("src/tests/A.cs"),
            File("src/App/B.cs"),
            File("src/tests/C.cs"),
            File("src/App/D.cs"),
        };

        IReadOnlyList<SourceFile> kept = SourceFileFilter.Apply(
            files,
            ignoreTestCode: true,
            ignoreGeneratedCode: false);

        Assert.Equal(new[] { files[1], files[3] }, kept);
    }

    [Fact]
    public void Apply_rejects_a_null_file_list()
    {
        Assert.Throws<ArgumentNullException>(() => SourceFileFilter.Apply(null!, false, false));
    }
}
