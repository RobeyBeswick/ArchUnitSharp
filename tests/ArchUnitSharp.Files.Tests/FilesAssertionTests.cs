using ArchUnitSharp.Common.Extraction;
using ArchUnitSharp.Files.Assertion;

namespace ArchUnitSharp.Files.Tests;

public class FilesAssertionTests
{
    [Fact]
    public void Exist_passes_a_non_empty_selection_with_the_positive_mood()
    {
        var files = new Files(Graph(Self("a.cs"), Self("b.cs")));

        IReadOnlyList<Violation> violations = FilesAssertion.Exist(files, negate: false, options: null);

        Assert.Empty(violations);
    }

    [Fact]
    public void Exist_flags_every_selected_file_with_the_negated_mood()
    {
        var files = new Files(Graph(Self("a.cs"), Self("b.cs")));

        IReadOnlyList<Violation> violations = FilesAssertion.Exist(files, negate: true, options: null);

        Assert.Equal(
            new Violation[] { new FileViolation("a.cs"), new FileViolation("b.cs") },
            violations);
    }

    [Fact]
    public void Exist_returns_violations_in_selection_order()
    {
        var files = new Files(Graph(Self("Z/z.cs"), Self("A/a.cs")));

        IReadOnlyList<Violation> violations = FilesAssertion.Exist(files, negate: true, options: null);

        Assert.Equal(new[] { "A/a.cs", "Z/z.cs" }, violations.Select(static v => ((FileViolation)v).File));
    }

    [Fact]
    public void The_mood_flag_is_the_only_difference_between_the_two_moods()
    {
        var files = new Files(Graph(Self("a.cs")));

        IReadOnlyList<Violation> positive = FilesAssertion.Exist(files, negate: false, options: null);
        IReadOnlyList<Violation> negated = FilesAssertion.Exist(files, negate: true, options: null);

        Assert.Empty(positive);
        Assert.Equal(new[] { new FileViolation("a.cs") }, negated);
    }

    [Fact]
    public void Exist_guards_an_empty_selection_with_the_positive_mood()
    {
        IReadOnlyList<Violation> violations = FilesAssertion.Exist(new Files(Graph()), negate: false, options: null);

        var empty = Assert.Single(violations);
        Assert.IsType<EmptyTestViolation>(empty);
        Assert.Equal("project files should exist", Assert.IsType<EmptyTestViolation>(empty).RuleDescription);
    }

    [Fact]
    public void Exist_guards_an_empty_selection_with_the_negated_mood()
    {
        IReadOnlyList<Violation> violations = FilesAssertion.Exist(new Files(Graph()), negate: true, options: null);

        var empty = Assert.Single(violations);
        Assert.Equal("project files should not exist", Assert.IsType<EmptyTestViolation>(empty).RuleDescription);
    }

    [Fact]
    public void The_guard_names_the_selectors_that_left_the_selection_empty()
    {
        var files = new Files(Graph(Self("a.cs"), Self("b.cs"))).WithName("Car.cs");

        IReadOnlyList<Violation> violations = FilesAssertion.Exist(files, negate: true, options: null);

        var empty = Assert.Single(violations);
        Assert.Equal("project files with name 'Car.cs' should not exist", Assert.IsType<EmptyTestViolation>(empty).RuleDescription);
    }

    [Fact]
    public void The_guard_names_every_selector_in_its_own_words()
    {
        var files = new Files(Graph(Self("a.cs"))).InFolder("src/Models").InPath("src/Models/Car.cs").InFile("src.Models.Car");

        IReadOnlyList<Violation> violations = FilesAssertion.Exist(files, negate: false, options: null);

        var empty = Assert.Single(violations);
        Assert.Equal(
            "project files in folder 'src/Models' in path 'src/Models/Car.cs' in file 'src.Models.Car' should exist",
            Assert.IsType<EmptyTestViolation>(empty).RuleDescription);
    }

    [Fact]
    public void Exist_honours_allow_empty_tests_with_the_positive_mood()
    {
        IReadOnlyList<Violation> violations = FilesAssertion.Exist(
            new Files(Graph()),
            negate: false,
            options: new CheckOptions { AllowEmptyTests = true });

        Assert.Empty(violations);
    }

    [Fact]
    public void Exist_honours_allow_empty_tests_with_the_negated_mood()
    {
        IReadOnlyList<Violation> violations = FilesAssertion.Exist(
            new Files(Graph()),
            negate: true,
            options: new CheckOptions { AllowEmptyTests = true });

        Assert.Empty(violations);
    }

    [Fact]
    public void Exist_rejects_a_null_files_selection()
    {
        Assert.Throws<ArgumentNullException>(() => FilesAssertion.Exist(null!, negate: false, options: null));
    }

    private static Graph Graph(params Edge[] edges) => new(edges);

    private static Edge Self(string file) => new(file, file, external: false, ImportKind.None);
}
