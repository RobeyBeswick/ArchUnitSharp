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

    [Fact]
    public void Cycles_passes_an_acyclic_selection()
    {
        var files = new Files(Graph(
            Using("a.cs", "b.cs"),
            Using("b.cs", "c.cs")));

        IReadOnlyList<Violation> violations = FilesAssertion.Cycles(files, options: null);

        Assert.Empty(violations);
    }

    [Fact]
    public void Cycles_reports_each_cycle_of_the_selection_as_a_cycle_violation()
    {
        var files = new Files(Graph(
            Using("a.cs", "b.cs"),
            Using("b.cs", "a.cs")));

        IReadOnlyList<Violation> violations = FilesAssertion.Cycles(files, options: null);

        Assert.Equal(
            new Violation[] { new CycleViolation(new[] { "a.cs", "b.cs", "a.cs" }) },
            violations);
    }

    [Fact]
    public void Cycles_after_selectors_checks_only_the_selected_files()
    {
        var files = new Files(Graph(
            Using("src/Models/A.cs", "src/Models/B.cs"),
            Using("src/Models/B.cs", "src/Models/A.cs"),
            Using("src/App/X.cs", "src/App/Y.cs"),
            Using("src/App/Y.cs", "src/App/X.cs"))).InFolder("src/App");

        IReadOnlyList<Violation> violations = FilesAssertion.Cycles(files, options: null);

        Assert.Equal(
            new Violation[] { new CycleViolation(new[] { "src/App/X.cs", "src/App/Y.cs", "src/App/X.cs" }) },
            violations);
    }

    [Fact]
    public void Cycles_guards_an_empty_selection()
    {
        IReadOnlyList<Violation> violations = FilesAssertion.Cycles(new Files(Graph()), options: null);

        var empty = Assert.Single(violations);
        Assert.IsType<EmptyTestViolation>(empty);
        Assert.Equal("project files should have no cycles", Assert.IsType<EmptyTestViolation>(empty).RuleDescription);
    }

    [Fact]
    public void The_cycles_guard_names_the_selectors_that_left_the_selection_empty()
    {
        var files = new Files(Graph(Self("a.cs"), Self("b.cs"))).WithName("Car.cs");

        IReadOnlyList<Violation> violations = FilesAssertion.Cycles(files, options: null);

        var empty = Assert.Single(violations);
        Assert.Equal(
            "project files with name 'Car.cs' should have no cycles",
            Assert.IsType<EmptyTestViolation>(empty).RuleDescription);
    }

    [Fact]
    public void Cycles_honours_allow_empty_tests()
    {
        IReadOnlyList<Violation> violations = FilesAssertion.Cycles(
            new Files(Graph()),
            options: new CheckOptions { AllowEmptyTests = true });

        Assert.Empty(violations);
    }

    [Fact]
    public void Cycles_rejects_a_null_files_selection()
    {
        Assert.Throws<ArgumentNullException>(() => FilesAssertion.Cycles(null!, options: null));
    }

    [Fact]
    public void HaveName_passes_every_file_whose_name_matches()
    {
        var files = new Files(Graph(Self("src/Models/Car.cs"), Self("src/App/Program.cs")));

        IReadOnlyList<Violation> violations = FilesAssertion.HaveName(
            files, NameFilter("*.cs"), negate: false, options: null);

        Assert.Empty(violations);
    }

    [Fact]
    public void HaveName_flags_every_file_whose_name_does_not_match()
    {
        var files = new Files(Graph(Self("Car.cs"), Self("Truck.cs")));

        IReadOnlyList<Violation> violations = FilesAssertion.HaveName(
            files, NameFilter("Car.cs"), negate: false, options: null);

        Assert.Equal(new[] { new FileViolation("Truck.cs") }, violations);
    }

    [Fact]
    public void HaveName_matches_the_name_not_the_folder_or_path()
    {
        var files = new Files(Graph(
            Self("src/Models/Car.cs"),
            Self("src/App/Car.cs"),
            Self("src/App/Program.cs")));

        IReadOnlyList<Violation> violations = FilesAssertion.HaveName(
            files, NameFilter("Car.cs"), negate: false, options: null);

        Assert.Equal(new[] { new FileViolation("src/App/Program.cs") }, violations);
    }

    [Fact]
    public void HaveName_flags_every_file_whose_name_matches_with_the_negated_mood()
    {
        var files = new Files(Graph(Self("Car.cs"), Self("Truck.cs")));

        IReadOnlyList<Violation> violations = FilesAssertion.HaveName(
            files, NameFilter("Car.cs"), negate: true, options: null);

        Assert.Equal(new[] { new FileViolation("Car.cs") }, violations);
    }

    [Fact]
    public void The_mood_flag_is_the_only_difference_between_the_two_have_name_moods()
    {
        var files = new Files(Graph(Self("Car.cs"), Self("Truck.cs")));

        IReadOnlyList<Violation> positive = FilesAssertion.HaveName(
            files, NameFilter("Car.cs"), negate: false, options: null);
        IReadOnlyList<Violation> negated = FilesAssertion.HaveName(
            files, NameFilter("Car.cs"), negate: true, options: null);

        Assert.Equal(new[] { new FileViolation("Truck.cs") }, positive);
        Assert.Equal(new[] { new FileViolation("Car.cs") }, negated);
    }

    [Fact]
    public void HaveName_returns_violations_in_selection_order()
    {
        var files = new Files(Graph(Self("Z/z.cs"), Self("A/a.cs")));

        IReadOnlyList<Violation> violations = FilesAssertion.HaveName(
            files, NameFilter("Car.cs"), negate: false, options: null);

        Assert.Equal(
            new[] { "A/a.cs", "Z/z.cs" },
            violations.Select(static v => ((FileViolation)v).File));
    }

    [Fact]
    public void HaveName_guards_an_empty_selection_with_the_positive_mood()
    {
        IReadOnlyList<Violation> violations = FilesAssertion.HaveName(
            new Files(Graph()), NameFilter("Car.cs"), negate: false, options: null);

        var empty = Assert.Single(violations);
        Assert.IsType<EmptyTestViolation>(empty);
        Assert.Equal("project files should have name 'Car.cs'", Assert.IsType<EmptyTestViolation>(empty).RuleDescription);
    }

    [Fact]
    public void HaveName_guards_an_empty_selection_with_the_negated_mood()
    {
        IReadOnlyList<Violation> violations = FilesAssertion.HaveName(
            new Files(Graph()), NameFilter("Car.cs"), negate: true, options: null);

        var empty = Assert.Single(violations);
        Assert.Equal("project files should not have name 'Car.cs'", Assert.IsType<EmptyTestViolation>(empty).RuleDescription);
    }

    [Fact]
    public void The_have_name_guard_names_the_selectors_that_left_the_selection_empty()
    {
        var files = new Files(Graph(Self("a.cs"), Self("b.cs"))).WithName("Truck.cs");

        IReadOnlyList<Violation> violations = FilesAssertion.HaveName(
            files, NameFilter("Car.cs"), negate: false, options: null);

        var empty = Assert.Single(violations);
        Assert.Equal(
            "project files with name 'Truck.cs' should have name 'Car.cs'",
            Assert.IsType<EmptyTestViolation>(empty).RuleDescription);
    }

    [Fact]
    public void HaveName_honours_allow_empty_tests_with_the_positive_mood()
    {
        IReadOnlyList<Violation> violations = FilesAssertion.HaveName(
            new Files(Graph()),
            NameFilter("Car.cs"),
            negate: false,
            options: new CheckOptions { AllowEmptyTests = true });

        Assert.Empty(violations);
    }

    [Fact]
    public void HaveName_honours_allow_empty_tests_with_the_negated_mood()
    {
        IReadOnlyList<Violation> violations = FilesAssertion.HaveName(
            new Files(Graph()),
            NameFilter("Car.cs"),
            negate: true,
            options: new CheckOptions { AllowEmptyTests = true });

        Assert.Empty(violations);
    }

    [Fact]
    public void HaveName_rejects_a_null_files_selection()
    {
        Assert.Throws<ArgumentNullException>(() =>
            FilesAssertion.HaveName(null!, NameFilter("Car.cs"), negate: false, options: null));
    }

    [Fact]
    public void HaveName_rejects_a_null_filter()
    {
        Assert.Throws<ArgumentNullException>(() =>
            FilesAssertion.HaveName(new Files(Graph(Self("Car.cs"))), null!, negate: false, options: null));
    }

    [Fact]
    public void BeInFolder_passes_every_file_whose_folder_matches()
    {
        var files = new Files(Graph(Self("src/Models/Car.cs"), Self("src/Models/Truck.cs")));

        IReadOnlyList<Violation> violations = FilesAssertion.BeInFolder(
            files, FolderFilter("src/Models"), negate: false, options: null);

        Assert.Empty(violations);
    }

    [Fact]
    public void BeInFolder_matches_the_folder_not_the_file_name()
    {
        var files = new Files(Graph(Self("src/Models/Car.cs"), Self("src/App/Car.cs")));

        IReadOnlyList<Violation> violations = FilesAssertion.BeInFolder(
            files, FolderFilter("src/Models"), negate: false, options: null);

        Assert.Equal(new[] { new FileViolation("src/App/Car.cs") }, violations);
    }

    [Fact]
    public void BeInFolder_flags_every_file_in_the_folder_with_the_negated_mood()
    {
        var files = new Files(Graph(Self("src/Models/Car.cs"), Self("src/App/Program.cs")));

        IReadOnlyList<Violation> violations = FilesAssertion.BeInFolder(
            files, FolderFilter("src/Models"), negate: true, options: null);

        Assert.Equal(new[] { new FileViolation("src/Models/Car.cs") }, violations);
    }

    [Fact]
    public void BeInFolder_guards_an_empty_selection_with_the_positive_mood()
    {
        IReadOnlyList<Violation> violations = FilesAssertion.BeInFolder(
            new Files(Graph()), FolderFilter("src/Models"), negate: false, options: null);

        var empty = Assert.Single(violations);
        Assert.Equal(
            "project files should be in folder 'src/Models'",
            Assert.IsType<EmptyTestViolation>(empty).RuleDescription);
    }

    [Fact]
    public void BeInFolder_guards_an_empty_selection_with_the_negated_mood()
    {
        IReadOnlyList<Violation> violations = FilesAssertion.BeInFolder(
            new Files(Graph()), FolderFilter("src/Models"), negate: true, options: null);

        var empty = Assert.Single(violations);
        Assert.Equal(
            "project files should not be in folder 'src/Models'",
            Assert.IsType<EmptyTestViolation>(empty).RuleDescription);
    }

    [Fact]
    public void BeInFolder_honours_allow_empty_tests_with_the_positive_mood()
    {
        IReadOnlyList<Violation> violations = FilesAssertion.BeInFolder(
            new Files(Graph()),
            FolderFilter("src/Models"),
            negate: false,
            options: new CheckOptions { AllowEmptyTests = true });

        Assert.Empty(violations);
    }

    [Fact]
    public void BeInFolder_honours_allow_empty_tests_with_the_negated_mood()
    {
        IReadOnlyList<Violation> violations = FilesAssertion.BeInFolder(
            new Files(Graph()),
            FolderFilter("src/Models"),
            negate: true,
            options: new CheckOptions { AllowEmptyTests = true });

        Assert.Empty(violations);
    }

    [Fact]
    public void BeInFolder_rejects_a_null_files_selection()
    {
        Assert.Throws<ArgumentNullException>(() =>
            FilesAssertion.BeInFolder(null!, FolderFilter("src/Models"), negate: false, options: null));
    }

    [Fact]
    public void BeInFolder_rejects_a_null_filter()
    {
        Assert.Throws<ArgumentNullException>(() =>
            FilesAssertion.BeInFolder(new Files(Graph(Self("Car.cs"))), null!, negate: false, options: null));
    }

    [Fact]
    public void BeInPath_passes_a_file_whose_path_matches()
    {
        var files = new Files(Graph(Self("src/Models/Car.cs")));

        IReadOnlyList<Violation> violations = FilesAssertion.BeInPath(
            files, PathFilter("src/Models/Car.cs"), negate: false, options: null);

        Assert.Empty(violations);
    }

    [Fact]
    public void BeInPath_matches_the_whole_path_not_just_the_name()
    {
        var files = new Files(Graph(Self("src/Models/Car.cs"), Self("src/App/Car.cs")));

        IReadOnlyList<Violation> violations = FilesAssertion.BeInPath(
            files, PathFilter("src/Models/Car.cs"), negate: false, options: null);

        Assert.Equal(new[] { new FileViolation("src/App/Car.cs") }, violations);
    }

    [Fact]
    public void BeInPath_flags_every_file_at_the_path_with_the_negated_mood()
    {
        var files = new Files(Graph(Self("src/Models/Car.cs"), Self("src/App/Program.cs")));

        IReadOnlyList<Violation> violations = FilesAssertion.BeInPath(
            files, PathFilter("src/Models/Car.cs"), negate: true, options: null);

        Assert.Equal(new[] { new FileViolation("src/Models/Car.cs") }, violations);
    }

    [Fact]
    public void BeInPath_guards_an_empty_selection_with_the_positive_mood()
    {
        IReadOnlyList<Violation> violations = FilesAssertion.BeInPath(
            new Files(Graph()), PathFilter("src/Models/Car.cs"), negate: false, options: null);

        var empty = Assert.Single(violations);
        Assert.Equal(
            "project files should be in path 'src/Models/Car.cs'",
            Assert.IsType<EmptyTestViolation>(empty).RuleDescription);
    }

    [Fact]
    public void BeInPath_guards_an_empty_selection_with_the_negated_mood()
    {
        IReadOnlyList<Violation> violations = FilesAssertion.BeInPath(
            new Files(Graph()), PathFilter("src/Models/Car.cs"), negate: true, options: null);

        var empty = Assert.Single(violations);
        Assert.Equal(
            "project files should not be in path 'src/Models/Car.cs'",
            Assert.IsType<EmptyTestViolation>(empty).RuleDescription);
    }

    [Fact]
    public void BeInPath_honours_allow_empty_tests_with_the_positive_mood()
    {
        IReadOnlyList<Violation> violations = FilesAssertion.BeInPath(
            new Files(Graph()),
            PathFilter("src/Models/Car.cs"),
            negate: false,
            options: new CheckOptions { AllowEmptyTests = true });

        Assert.Empty(violations);
    }

    [Fact]
    public void BeInPath_honours_allow_empty_tests_with_the_negated_mood()
    {
        IReadOnlyList<Violation> violations = FilesAssertion.BeInPath(
            new Files(Graph()),
            PathFilter("src/Models/Car.cs"),
            negate: true,
            options: new CheckOptions { AllowEmptyTests = true });

        Assert.Empty(violations);
    }

    [Fact]
    public void BeInPath_rejects_a_null_files_selection()
    {
        Assert.Throws<ArgumentNullException>(() =>
            FilesAssertion.BeInPath(null!, PathFilter("src/Models/Car.cs"), negate: false, options: null));
    }

    [Fact]
    public void BeInPath_rejects_a_null_filter()
    {
        Assert.Throws<ArgumentNullException>(() =>
            FilesAssertion.BeInPath(new Files(Graph(Self("Car.cs"))), null!, negate: false, options: null));
    }

    [Fact]
    public void DependOn_passes_when_every_subject_depends_on_an_object_file()
    {
        var rule = new Files(Graph(
            Self("src/App/Program.cs"),
            Self("src/App/Truck.cs"),
            Self("src/Models/Car.cs"),
            Using("src/App/Program.cs", "src/Models/Car.cs"),
            Using("src/App/Truck.cs", "src/Models/Car.cs")))
            .InFolder("src/App")
            .Should()
            .DependOn()
            .InFolder("src/Models");

        IReadOnlyList<Violation> violations = FilesAssertion.DependOn(rule, options: null);

        Assert.Empty(violations);
    }

    [Fact]
    public void DependOn_flags_a_subject_file_that_depends_on_nothing()
    {
        var rule = new Files(Graph(
            Self("src/App/Program.cs"),
            Self("src/App/Orphan.cs"),
            Self("src/Models/Car.cs"),
            Using("src/App/Program.cs", "src/Models/Car.cs")))
            .InFolder("src/App")
            .Should()
            .DependOn()
            .InFolder("src/Models");

        IReadOnlyList<Violation> violations = FilesAssertion.DependOn(rule, options: null);

        Assert.Equal(new[] { new FileViolation("src/App/Orphan.cs") }, violations);
    }

    [Fact]
    public void DependOn_flags_a_subject_file_whose_dependencies_miss_the_object()
    {
        var rule = new Files(Graph(
            Self("src/App/Program.cs"),
            Self("src/Util/Helper.cs"),
            Self("src/Models/Car.cs"),
            Using("src/App/Program.cs", "src/Util/Helper.cs")))
            .InFolder("src/App")
            .Should()
            .DependOn()
            .InFolder("src/Models");

        IReadOnlyList<Violation> violations = FilesAssertion.DependOn(rule, options: null);

        Assert.Equal(new[] { new FileViolation("src/App/Program.cs") }, violations);
    }

    [Fact]
    public void DependOn_passes_a_subject_file_that_also_depends_on_non_object_files()
    {
        var rule = new Files(Graph(
            Self("src/App/Program.cs"),
            Self("src/Models/Car.cs"),
            Self("src/Util/Helper.cs"),
            Using("src/App/Program.cs", "src/Models/Car.cs"),
            Using("src/App/Program.cs", "src/Util/Helper.cs"),
            Using("src/App/Program.cs", "System")))
            .InFolder("src/App")
            .Should()
            .DependOn()
            .InFolder("src/Models");

        IReadOnlyList<Violation> violations = FilesAssertion.DependOn(rule, options: null);

        Assert.Empty(violations);
    }

    [Fact]
    public void DependOn_reports_violations_in_subject_order()
    {
        var rule = new Files(Graph(
            Self("src/App/Alpha.cs"),
            Self("src/App/Mike.cs"),
            Self("src/App/Zeta.cs"),
            Self("src/Models/Car.cs"),
            Self("src/Util/Helper.cs"),
            Using("src/App/Alpha.cs", "src/Util/Helper.cs"),
            Using("src/App/Mike.cs", "src/Util/Helper.cs"),
            Using("src/App/Zeta.cs", "src/Models/Car.cs")))
            .InFolder("src/App")
            .Should()
            .DependOn()
            .InFolder("src/Models");

        IReadOnlyList<Violation> violations = FilesAssertion.DependOn(rule, options: null);

        Assert.Equal(
            new[] { "src/App/Alpha.cs", "src/App/Mike.cs" },
            violations.Select(static v => ((FileViolation)v).File));
    }

    [Fact]
    public void DependOn_flags_each_offending_dependency_with_the_negated_mood()
    {
        var rule = new Files(Graph(
            Self("src/App/Program.cs"),
            Self("src/Models/Car.cs"),
            Self("src/Models/Truck.cs"),
            Using("src/App/Program.cs", "src/Models/Car.cs"),
            Using("src/App/Program.cs", "src/Models/Truck.cs")))
            .InFolder("src/App")
            .ShouldNot()
            .DependOn()
            .InFolder("src/Models");

        IReadOnlyList<Violation> violations = FilesAssertion.DependOn(rule, options: null);

        Assert.Equal(
            new Violation[]
            {
                new DependencyViolation("src/App/Program.cs", "src/Models/Car.cs"),
                new DependencyViolation("src/App/Program.cs", "src/Models/Truck.cs"),
            },
            violations);
    }

    [Fact]
    public void DependOn_passes_when_no_subject_depends_on_an_object_file_with_the_negated_mood()
    {
        var rule = new Files(Graph(
            Self("src/App/Program.cs"),
            Self("src/Util/Helper.cs"),
            Self("src/Models/Car.cs"),
            Using("src/App/Program.cs", "src/Util/Helper.cs")))
            .InFolder("src/App")
            .ShouldNot()
            .DependOn()
            .InFolder("src/Models");

        IReadOnlyList<Violation> violations = FilesAssertion.DependOn(rule, options: null);

        Assert.Empty(violations);
    }

    [Fact]
    public void DependOn_guards_an_empty_selection_with_the_positive_mood()
    {
        var rule = new Files(Graph()).Should().DependOn().InFolder("src/Models");

        IReadOnlyList<Violation> violations = FilesAssertion.DependOn(rule, options: null);

        var empty = Assert.Single(violations);
        Assert.IsType<EmptyTestViolation>(empty);
        Assert.Equal(
            "project files should depend on files in folder 'src/Models'",
            Assert.IsType<EmptyTestViolation>(empty).RuleDescription);
    }

    [Fact]
    public void DependOn_guards_an_empty_selection_with_the_negated_mood()
    {
        var rule = new Files(Graph()).ShouldNot().DependOn().InFolder("src/Models");

        IReadOnlyList<Violation> violations = FilesAssertion.DependOn(rule, options: null);

        var empty = Assert.Single(violations);
        Assert.Equal(
            "project files should not depend on files in folder 'src/Models'",
            Assert.IsType<EmptyTestViolation>(empty).RuleDescription);
    }

    [Fact]
    public void DependOn_guards_an_empty_selection_with_a_matching_object_with_the_positive_mood()
    {
        var rule = new Files(Graph(Self("src/Models/Truck.cs")))
            .WithName("Car.cs")
            .Should()
            .DependOn()
            .WithName("Truck.cs");

        IReadOnlyList<Violation> violations = FilesAssertion.DependOn(rule, options: null);

        var empty = Assert.Single(violations);
        Assert.IsType<EmptyTestViolation>(empty);
        Assert.Equal(
            "project files with name 'Car.cs' should depend on files with name 'Truck.cs'",
            Assert.IsType<EmptyTestViolation>(empty).RuleDescription);
    }

    [Fact]
    public void DependOn_guards_an_empty_selection_with_a_matching_object_with_the_negated_mood()
    {
        var rule = new Files(Graph(Self("src/Models/Truck.cs")))
            .WithName("Car.cs")
            .ShouldNot()
            .DependOn()
            .WithName("Truck.cs");

        IReadOnlyList<Violation> violations = FilesAssertion.DependOn(rule, options: null);

        var empty = Assert.Single(violations);
        Assert.IsType<EmptyTestViolation>(empty);
        Assert.Equal(
            "project files with name 'Car.cs' should not depend on files with name 'Truck.cs'",
            Assert.IsType<EmptyTestViolation>(empty).RuleDescription);
    }

    [Fact]
    public void DependOn_guards_an_object_that_matches_nothing_with_the_positive_mood()
    {
        var rule = new Files(Graph(Self("a.cs"), Self("b.cs")))
            .Should()
            .DependOn()
            .WithName("Car.cs");

        IReadOnlyList<Violation> violations = FilesAssertion.DependOn(rule, options: null);

        var empty = Assert.Single(violations);
        Assert.Equal(
            "project files should depend on files with name 'Car.cs'",
            Assert.IsType<EmptyTestViolation>(empty).RuleDescription);
    }

    [Fact]
    public void DependOn_guards_an_object_that_matches_nothing_with_the_negated_mood()
    {
        var rule = new Files(Graph(Self("a.cs"), Self("b.cs")))
            .ShouldNot()
            .DependOn()
            .WithName("Car.cs");

        IReadOnlyList<Violation> violations = FilesAssertion.DependOn(rule, options: null);

        var empty = Assert.Single(violations);
        Assert.Equal(
            "project files should not depend on files with name 'Car.cs'",
            Assert.IsType<EmptyTestViolation>(empty).RuleDescription);
    }

    [Fact]
    public void The_depend_on_guard_names_the_selectors_of_both_selection_and_object()
    {
        var rule = new Files(Graph(Self("a.cs"), Self("b.cs")))
            .WithName("Car.cs")
            .ShouldNot()
            .DependOn()
            .WithName("Truck.cs")
            .InFolder("src/Models");

        IReadOnlyList<Violation> violations = FilesAssertion.DependOn(rule, options: null);

        var empty = Assert.Single(violations);
        Assert.Equal(
            "project files with name 'Car.cs' should not depend on files with name 'Truck.cs' in folder 'src/Models'",
            Assert.IsType<EmptyTestViolation>(empty).RuleDescription);
    }

    [Fact]
    public void DependOn_honours_allow_empty_tests_with_the_positive_mood()
    {
        var rule = new Files(Graph()).Should().DependOn().WithName("Car.cs");

        IReadOnlyList<Violation> violations = FilesAssertion.DependOn(
            rule,
            options: new CheckOptions { AllowEmptyTests = true });

        Assert.Empty(violations);
    }

    [Fact]
    public void DependOn_honours_allow_empty_tests_with_the_negated_mood()
    {
        var rule = new Files(Graph(Self("a.cs"))).ShouldNot().DependOn().WithName("Car.cs");

        IReadOnlyList<Violation> violations = FilesAssertion.DependOn(
            rule,
            options: new CheckOptions { AllowEmptyTests = true });

        Assert.Empty(violations);
    }

    [Fact]
    public void DependOn_rejects_a_null_rule()
    {
        Assert.Throws<ArgumentNullException>(() => FilesAssertion.DependOn(null!, options: null));
    }

    [Fact]
    public void DependOnExternalModules_passes_when_every_subject_depends_on_a_matching_module()
    {
        var rule = new Files(Graph(
            Self("src/App/Program.cs"),
            Self("src/App/Truck.cs"),
            External("src/App/Program.cs", "System.Linq"),
            External("src/App/Truck.cs", "System.Collections.Generic")))
            .InFolder("src/App")
            .Should()
            .DependOnExternalModules()
            .Matching("System.*");

        IReadOnlyList<Violation> violations = FilesAssertion.DependOnExternalModules(rule, options: null);

        Assert.Empty(violations);
    }

    [Fact]
    public void DependOnExternalModules_flags_a_subject_file_that_depends_on_no_matching_module()
    {
        var rule = new Files(Graph(
            Self("src/App/Program.cs"),
            Self("src/App/Orphan.cs"),
            External("src/App/Program.cs", "System.Linq")))
            .InFolder("src/App")
            .Should()
            .DependOnExternalModules()
            .Matching("System.*");

        IReadOnlyList<Violation> violations = FilesAssertion.DependOnExternalModules(rule, options: null);

        Assert.Equal(new[] { new FileViolation("src/App/Orphan.cs") }, violations);
    }

    [Fact]
    public void DependOnExternalModules_ignores_internal_targets()
    {
        var rule = new Files(Graph(
            Self("src/App/Program.cs"),
            Self("src/App/Other.cs"),
            Self("src/Models/Car.cs"),
            Using("src/App/Program.cs", "src/Models/Car.cs"),
            External("src/App/Other.cs", "Newtonsoft.Json")))
            .InFolder("src/App")
            .Should()
            .DependOnExternalModules()
            .Matching("**/*");

        IReadOnlyList<Violation> violations = FilesAssertion.DependOnExternalModules(rule, options: null);

        Assert.Equal(new[] { new FileViolation("src/App/Program.cs") }, violations);
    }

    [Fact]
    public void DependOnExternalModules_reports_violations_in_subject_order()
    {
        var rule = new Files(Graph(
            Self("src/App/Alpha.cs"),
            Self("src/App/Mike.cs"),
            Self("src/App/Zeta.cs"),
            External("src/App/Alpha.cs", "NUnit"),
            External("src/App/Mike.cs", "NUnit"),
            External("src/App/Zeta.cs", "System.Linq")))
            .InFolder("src/App")
            .Should()
            .DependOnExternalModules()
            .Matching("System.*");

        IReadOnlyList<Violation> violations = FilesAssertion.DependOnExternalModules(rule, options: null);

        Assert.Equal(
            new[] { "src/App/Alpha.cs", "src/App/Mike.cs" },
            violations.Select(static v => ((FileViolation)v).File));
    }

    [Fact]
    public void DependOnExternalModules_flags_each_offending_dependency_with_the_negated_mood()
    {
        var rule = new Files(Graph(
            Self("src/App/Program.cs"),
            External("src/App/Program.cs", "System.Linq"),
            External("src/App/Program.cs", "System.Collections.Generic"),
            External("src/App/Program.cs", "Newtonsoft.Json")))
            .InFolder("src/App")
            .ShouldNot()
            .DependOnExternalModules()
            .Matching("System.*");

        IReadOnlyList<Violation> violations = FilesAssertion.DependOnExternalModules(rule, options: null);

        Assert.Equal(
            new Violation[]
            {
                new DependencyViolation("src/App/Program.cs", "System.Collections.Generic"),
                new DependencyViolation("src/App/Program.cs", "System.Linq"),
            },
            violations);
    }

    [Fact]
    public void DependOnExternalModules_passes_when_no_subject_depends_on_a_matching_module_with_the_negated_mood()
    {
        var rule = new Files(Graph(
            Self("src/App/Program.cs"),
            Self("src/App/Other.cs"),
            Self("src/Util/Helper.cs"),
            External("src/App/Program.cs", "Newtonsoft.Json"),
            External("src/Util/Helper.cs", "System.Linq")))
            .InFolder("src/App")
            .ShouldNot()
            .DependOnExternalModules()
            .Matching("System.*");

        IReadOnlyList<Violation> violations = FilesAssertion.DependOnExternalModules(rule, options: null);

        Assert.Empty(violations);
    }

    [Fact]
    public void DependOnExternalModules_guards_an_empty_selection_with_the_positive_mood()
    {
        var rule = new Files(Graph()).Should().DependOnExternalModules().Matching("System.*");

        IReadOnlyList<Violation> violations = FilesAssertion.DependOnExternalModules(rule, options: null);

        var empty = Assert.Single(violations);
        Assert.IsType<EmptyTestViolation>(empty);
        Assert.Equal(
            "project files should depend on external modules matching 'System.*'",
            Assert.IsType<EmptyTestViolation>(empty).RuleDescription);
    }

    [Fact]
    public void DependOnExternalModules_guards_an_empty_selection_with_the_negated_mood()
    {
        var rule = new Files(Graph()).ShouldNot().DependOnExternalModules().Matching("System.*");

        IReadOnlyList<Violation> violations = FilesAssertion.DependOnExternalModules(rule, options: null);

        var empty = Assert.Single(violations);
        Assert.Equal(
            "project files should not depend on external modules matching 'System.*'",
            Assert.IsType<EmptyTestViolation>(empty).RuleDescription);
    }

    [Fact]
    public void DependOnExternalModules_guards_an_object_that_matches_nothing_with_the_positive_mood()
    {
        var rule = new Files(Graph(Self("a.cs"))).Should().DependOnExternalModules().Matching("Newtonsoft.*");

        IReadOnlyList<Violation> violations = FilesAssertion.DependOnExternalModules(rule, options: null);

        var empty = Assert.Single(violations);
        Assert.Equal(
            "project files should depend on external modules matching 'Newtonsoft.*'",
            Assert.IsType<EmptyTestViolation>(empty).RuleDescription);
    }

    [Fact]
    public void DependOnExternalModules_guards_an_object_that_matches_nothing_with_the_negated_mood()
    {
        var rule = new Files(Graph(Self("a.cs"))).ShouldNot().DependOnExternalModules().Matching("Newtonsoft.*");

        IReadOnlyList<Violation> violations = FilesAssertion.DependOnExternalModules(rule, options: null);

        var empty = Assert.Single(violations);
        Assert.Equal(
            "project files should not depend on external modules matching 'Newtonsoft.*'",
            Assert.IsType<EmptyTestViolation>(empty).RuleDescription);
    }

    [Fact]
    public void The_external_modules_guard_names_the_selectors_of_both_selection_and_object()
    {
        var rule = new Files(Graph(Self("a.cs"), Self("b.cs")))
            .WithName("Car.cs")
            .ShouldNot()
            .DependOnExternalModules()
            .Matching("System.*")
            .Matching("Newtonsoft.*");

        IReadOnlyList<Violation> violations = FilesAssertion.DependOnExternalModules(rule, options: null);

        var empty = Assert.Single(violations);
        Assert.Equal(
            "project files with name 'Car.cs' should not depend on external modules matching 'System.*' matching 'Newtonsoft.*'",
            Assert.IsType<EmptyTestViolation>(empty).RuleDescription);
    }

    [Fact]
    public void DependOnExternalModules_honours_allow_empty_tests_with_the_positive_mood()
    {
        var rule = new Files(Graph()).Should().DependOnExternalModules().Matching("System.*");

        IReadOnlyList<Violation> violations = FilesAssertion.DependOnExternalModules(
            rule,
            options: new CheckOptions { AllowEmptyTests = true });

        Assert.Empty(violations);
    }

    [Fact]
    public void DependOnExternalModules_honours_allow_empty_tests_with_the_negated_mood()
    {
        var rule = new Files(Graph(Self("a.cs"))).ShouldNot().DependOnExternalModules().Matching("System.*");

        IReadOnlyList<Violation> violations = FilesAssertion.DependOnExternalModules(
            rule,
            options: new CheckOptions { AllowEmptyTests = true });

        Assert.Empty(violations);
    }

    [Fact]
    public void DependOnExternalModules_rejects_a_null_rule()
    {
        Assert.Throws<ArgumentNullException>(() => FilesAssertion.DependOnExternalModules(null!, options: null));
    }

    [Fact]
    public void AdhereTo_passes_every_file_the_predicate_accepts()
    {
        var files = new Files(Graph(Self("a.cs"), Self("b.cs")), Reader("namespace App; public class X { }"));

        IReadOnlyList<Violation> violations = FilesAssertion.AdhereTo(
            files, static _ => true, "message", negate: false, options: null);

        Assert.Empty(violations);
    }

    [Fact]
    public void AdhereTo_flags_every_file_the_predicate_rejects()
    {
        var files = new Files(Graph(Self("a.cs"), Self("b.txt")), Reader("text"));

        IReadOnlyList<Violation> violations = FilesAssertion.AdhereTo(
            files,
            static detail => detail.Extension == ".cs",
            "every file is a C# file",
            negate: false,
            options: null);

        Assert.Equal(
            new Violation[]
            {
                new AdhereToViolation("b.txt", "every file is a C# file"),
            },
            violations);
    }

    [Fact]
    public void AdhereTo_flags_every_file_the_predicate_accepts_with_the_negated_mood()
    {
        var files = new Files(Graph(Self("a.cs"), Self("b.txt")), Reader("text"));

        IReadOnlyList<Violation> violations = FilesAssertion.AdhereTo(
            files,
            static detail => detail.Extension == ".cs",
            "no file is a C# file",
            negate: true,
            options: null);

        Assert.Equal(
            new Violation[]
            {
                new AdhereToViolation("a.cs", "no file is a C# file"),
            },
            violations);
    }

    [Fact]
    public void AdhereTo_passes_when_the_predicate_rejects_every_file_with_the_negated_mood()
    {
        var files = new Files(Graph(Self("a.cs"), Self("b.cs")), Reader("text"));

        IReadOnlyList<Violation> violations = FilesAssertion.AdhereTo(
            files,
            static detail => detail.Extension == ".txt",
            "no file is a text file",
            negate: true,
            options: null);

        Assert.Empty(violations);
    }

    [Fact]
    public void AdhereTo_passes_the_file_detail_to_the_predicate()
    {
        var sources = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["src/Models/Car.cs"] = "namespace App.Models;\n\npublic class Car { }\n",
        };
        var files = new Files(Graph(Self("src/Models/Car.cs")), identifier => sources[identifier]);

        FileDetail? seen = null;
        IReadOnlyList<Violation> violations = FilesAssertion.AdhereTo(
            files,
            detail =>
            {
                seen = detail;
                return true;
            },
            "message",
            negate: false,
            options: null);

        Assert.Empty(violations);
        var detail = Assert.IsType<FileDetail>(seen);
        Assert.Equal("src/Models/Car.cs", detail.Path);
        Assert.Equal("Car", detail.NameWithoutExtension);
        Assert.Equal(".cs", detail.Extension);
        Assert.Equal("src/Models", detail.Directory);
        Assert.Equal("namespace App.Models;\n\npublic class Car { }\n", detail.SourceText);
        Assert.Equal(2, detail.NonBlankLineCount);
    }

    [Fact]
    public void AdhereTo_reports_violations_in_selection_order()
    {
        var files = new Files(Graph(Self("Z/z.cs"), Self("A/a.cs")), Reader("text"));

        IReadOnlyList<Violation> violations = FilesAssertion.AdhereTo(
            files, static _ => false, "message", negate: false, options: null);

        Assert.Equal(
            new[] { "A/a.cs", "Z/z.cs" },
            violations.Select(static v => ((AdhereToViolation)v).File));
    }

    [Fact]
    public void The_mood_flag_is_the_only_difference_between_the_two_adhere_to_moods()
    {
        var files = new Files(Graph(Self("a.cs"), Self("b.txt")), Reader("text"));

        IReadOnlyList<Violation> positive = FilesAssertion.AdhereTo(
            files,
            static detail => detail.Extension == ".cs",
            "message",
            negate: false,
            options: null);
        IReadOnlyList<Violation> negated = FilesAssertion.AdhereTo(
            files,
            static detail => detail.Extension == ".cs",
            "message",
            negate: true,
            options: null);

        Assert.Equal(new[] { new AdhereToViolation("b.txt", "message") }, positive);
        Assert.Equal(new[] { new AdhereToViolation("a.cs", "message") }, negated);
    }

    [Fact]
    public void AdhereTo_guards_an_empty_selection_with_the_positive_mood()
    {
        IReadOnlyList<Violation> violations = FilesAssertion.AdhereTo(
            new Files(Graph()), static _ => true, "message", negate: false, options: null);

        var empty = Assert.Single(violations);
        Assert.IsType<EmptyTestViolation>(empty);
        Assert.Equal(
            "project files should adhere to 'message'",
            Assert.IsType<EmptyTestViolation>(empty).RuleDescription);
    }

    [Fact]
    public void AdhereTo_guards_an_empty_selection_with_the_negated_mood()
    {
        IReadOnlyList<Violation> violations = FilesAssertion.AdhereTo(
            new Files(Graph()), static _ => true, "message", negate: true, options: null);

        var empty = Assert.Single(violations);
        Assert.Equal(
            "project files should not adhere to 'message'",
            Assert.IsType<EmptyTestViolation>(empty).RuleDescription);
    }

    [Fact]
    public void The_adhere_to_guard_names_the_selectors_that_left_the_selection_empty()
    {
        var files = new Files(Graph(Self("a.cs"), Self("b.cs"))).WithName("Car.cs");

        IReadOnlyList<Violation> violations = FilesAssertion.AdhereTo(
            files, static _ => true, "message", negate: false, options: null);

        var empty = Assert.Single(violations);
        Assert.Equal(
            "project files with name 'Car.cs' should adhere to 'message'",
            Assert.IsType<EmptyTestViolation>(empty).RuleDescription);
    }

    [Fact]
    public void AdhereTo_honours_allow_empty_tests_with_the_positive_mood()
    {
        IReadOnlyList<Violation> violations = FilesAssertion.AdhereTo(
            new Files(Graph()),
            static _ => true,
            "message",
            negate: false,
            options: new CheckOptions { AllowEmptyTests = true });

        Assert.Empty(violations);
    }

    [Fact]
    public void AdhereTo_honours_allow_empty_tests_with_the_negated_mood()
    {
        IReadOnlyList<Violation> violations = FilesAssertion.AdhereTo(
            new Files(Graph()),
            static _ => true,
            "message",
            negate: true,
            options: new CheckOptions { AllowEmptyTests = true });

        Assert.Empty(violations);
    }

    [Fact]
    public void AdhereTo_rejects_a_null_files_selection()
    {
        Assert.Throws<ArgumentNullException>(() =>
            FilesAssertion.AdhereTo(null!, static _ => true, "message", negate: false, options: null));
    }

    [Fact]
    public void AdhereTo_rejects_a_null_predicate()
    {
        Assert.Throws<ArgumentNullException>(() =>
            FilesAssertion.AdhereTo(new Files(Graph(Self("a.cs"))), null!, "message", negate: false, options: null));
    }

    [Fact]
    public void AdhereTo_rejects_a_null_message()
    {
        Assert.Throws<ArgumentNullException>(() =>
            FilesAssertion.AdhereTo(new Files(Graph(Self("a.cs"))), static _ => true, null!, negate: false, options: null));
    }

    private static Func<string, string> Reader(string content) => _ => content;

    private static Graph Graph(params Edge[] edges) => new(edges);

    private static Edge Self(string file) => new(file, file, external: false, ImportKind.None);

    private static Edge Using(string source, string target) =>
        new(source, target, external: false, ImportKind.Using);

    private static Edge External(string source, string module) =>
        new(source, module, external: true, ImportKind.Using);

    private static Filter NameFilter(string glob) => new(new Pattern(glob), MatchTarget.Filename);

    private static Filter FolderFilter(string glob) => new(new Pattern(glob), MatchTarget.PathWithoutFilename);

    private static Filter PathFilter(string glob) => new(new Pattern(glob), MatchTarget.Path);
}
