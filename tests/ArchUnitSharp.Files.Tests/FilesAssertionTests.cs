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

    private static Graph Graph(params Edge[] edges) => new(edges);

    private static Edge Self(string file) => new(file, file, external: false, ImportKind.None);

    private static Edge Using(string source, string target) =>
        new(source, target, external: false, ImportKind.Using);

    private static Filter NameFilter(string glob) => new(new Pattern(glob), MatchTarget.Filename);

    private static Filter FolderFilter(string glob) => new(new Pattern(glob), MatchTarget.PathWithoutFilename);

    private static Filter PathFilter(string glob) => new(new Pattern(glob), MatchTarget.Path);
}
