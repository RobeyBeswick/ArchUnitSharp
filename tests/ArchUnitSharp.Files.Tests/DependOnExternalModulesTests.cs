using ArchUnitSharp.Common.Extraction;

namespace ArchUnitSharp.Files.Tests;

public class DependOnExternalModulesTests
{
    [Fact]
    public void DependOnExternalModules_passes_through_the_fluent_chain()
    {
        var rule = new Files(Graph(
            Self("src/App/Program.cs"),
            Self("src/App/Other.cs"),
            External("src/App/Program.cs", "System.Linq"),
            External("src/App/Other.cs", "System.Collections.Generic")))
            .InFolder("src/App")
            .Should()
            .DependOnExternalModules()
            .Matching("System.*");

        Assert.Empty(rule.Check());
    }

    [Fact]
    public void DependOnExternalModules_reports_each_offending_dependency_through_the_fluent_chain()
    {
        var rule = new Files(Graph(
            Self("src/App/Program.cs"),
            External("src/App/Program.cs", "System.Linq")))
            .InFolder("src/App")
            .ShouldNot()
            .DependOnExternalModules()
            .Matching("System.*");

        IReadOnlyList<Violation> violations = rule.Check();

        Assert.Equal(
            new Violation[] { new DependencyViolation("src/App/Program.cs", "System.Linq") },
            violations);
    }

    [Fact]
    public void Matching_selectors_combine_with_or()
    {
        var rule = new Files(Graph(
            Self("src/App/Program.cs"),
            Self("src/App/Other.cs"),
            Self("src/App/Third.cs"),
            External("src/App/Program.cs", "System.Linq"),
            External("src/App/Other.cs", "Newtonsoft.Json"),
            External("src/App/Third.cs", "NUnit")))
            .InFolder("src/App")
            .Should()
            .DependOnExternalModules()
            .Matching("System.*")
            .Matching("Newtonsoft.*");

        IReadOnlyList<Violation> violations = rule.Check();

        Assert.Equal(new[] { new FileViolation("src/App/Third.cs") }, violations);
    }

    [Fact]
    public void A_selector_leaves_the_parent_object_unchanged()
    {
        var files = new Files(Graph(
            Self("src/App/Program.cs"),
            External("src/App/Program.cs", "System.Linq"),
            External("src/App/Program.cs", "Newtonsoft.Json")))
            .InFolder("src/App");

        var parent = files.ShouldNot().DependOnExternalModules().Matching("System.*");
        var narrowed = parent.Matching("Newtonsoft.*");

        Assert.Equal(
            new Violation[] { new DependencyViolation("src/App/Program.cs", "System.Linq") },
            parent.Check());
        Assert.Equal(
            new Violation[]
            {
                new DependencyViolation("src/App/Program.cs", "Newtonsoft.Json"),
                new DependencyViolation("src/App/Program.cs", "System.Linq"),
            },
            narrowed.Check());
    }

    [Fact]
    public void Two_branches_off_one_parent_do_not_see_each_others_selectors()
    {
        var files = new Files(Graph(
            Self("src/App/Program.cs"),
            External("src/App/Program.cs", "System.Linq"),
            External("src/App/Program.cs", "Newtonsoft.Json")))
            .InFolder("src/App");

        var parent = files.ShouldNot().DependOnExternalModules();
        var system = parent.Matching("System.*");
        var newtonsoft = parent.Matching("Newtonsoft.*");

        Assert.Equal(
            new Violation[]
            {
                new DependencyViolation("src/App/Program.cs", "Newtonsoft.Json"),
                new DependencyViolation("src/App/Program.cs", "System.Linq"),
            },
            parent.Check());
        Assert.Equal(
            new Violation[] { new DependencyViolation("src/App/Program.cs", "System.Linq") },
            system.Check());
        Assert.Equal(
            new Violation[] { new DependencyViolation("src/App/Program.cs", "Newtonsoft.Json") },
            newtonsoft.Check());
    }

    [Fact]
    public void Two_depend_on_external_modules_rules_off_one_selection_do_not_see_each_other()
    {
        var files = new Files(Graph(
            Self("src/App/Program.cs"),
            External("src/App/Program.cs", "System.Linq"),
            External("src/App/Program.cs", "Newtonsoft.Json")))
            .InFolder("src/App");

        var should = files.Should().DependOnExternalModules().Matching("System.*");
        var shouldNot = files.ShouldNot().DependOnExternalModules().Matching("Newtonsoft.*");

        Assert.Empty(should.Check());
        Assert.Equal(
            new Violation[] { new DependencyViolation("src/App/Program.cs", "Newtonsoft.Json") },
            shouldNot.Check());
        Assert.Equal(new[] { "src/App/Program.cs" }, files.Select());
    }

    [Fact]
    public void A_rule_can_be_checked_twice_and_reports_the_same_result()
    {
        var rule = new Files(Graph(
            Self("src/App/Program.cs"),
            External("src/App/Program.cs", "System.Linq")))
            .InFolder("src/App")
            .ShouldNot()
            .DependOnExternalModules()
            .Matching("System.*");

        IReadOnlyList<Violation> first = rule.Check();
        IReadOnlyList<Violation> second = rule.Check();

        Assert.Equal(first, second);
        Assert.NotSame(first, second);
    }

    [Fact]
    public void Matching_except_skips_the_excluded_module()
    {
        var rule = new Files(Graph(
            Self("src/App/Program.cs"),
            External("src/App/Program.cs", "System.Linq"),
            External("src/App/Program.cs", "System.Runtime")))
            .InFolder("src/App")
            .ShouldNot()
            .DependOnExternalModules()
            .Matching("System.*")
            .Except("System.Runtime");

        IReadOnlyList<Violation> violations = rule.Check();

        Assert.Equal(
            new Violation[] { new DependencyViolation("src/App/Program.cs", "System.Linq") },
            violations);
    }

    [Fact]
    public void Matching_except_leaves_the_parent_object_unchanged()
    {
        var files = new Files(Graph(
            Self("src/App/Program.cs"),
            External("src/App/Program.cs", "System.Linq"),
            External("src/App/Program.cs", "System.Runtime")))
            .InFolder("src/App");

        var parent = files.ShouldNot().DependOnExternalModules().Matching("System.*");
        var narrowed = parent.Except("System.Runtime");

        Assert.Equal(
            new Violation[]
            {
                new DependencyViolation("src/App/Program.cs", "System.Linq"),
                new DependencyViolation("src/App/Program.cs", "System.Runtime"),
            },
            parent.Check());
        Assert.Equal(
            new Violation[] { new DependencyViolation("src/App/Program.cs", "System.Linq") },
            narrowed.Check());
    }

    [Fact]
    public void Matching_except_without_a_selector_raises_a_user_error()
    {
        Assert.Throws<UserError>(() =>
            new Files(Graph(Self("a.cs"))).Should().DependOnExternalModules().Except("System.*"));
    }

    [Fact]
    public void DependOnExternalModules_guards_a_selection_that_matches_nothing()
    {
        var rule = new Files(Graph(Self("a.cs")))
            .WithName("Car.cs")
            .Should()
            .DependOnExternalModules()
            .Matching("System.*");

        IReadOnlyList<Violation> violations = rule.Check();

        Assert.Equal(
            new Violation[]
            {
                new EmptyTestViolation(
                    "project files with name 'Car.cs' should depend on external modules matching 'System.*'"),
            },
            violations);
    }

    [Fact]
    public void DependOnExternalModules_guards_an_object_that_matches_nothing()
    {
        var rule = new Files(Graph(Self("a.cs"), External("a.cs", "System.Linq")))
            .Should()
            .DependOnExternalModules()
            .Matching("Newtonsoft.*");

        IReadOnlyList<Violation> violations = rule.Check();

        Assert.Equal(
            new Violation[]
            {
                new EmptyTestViolation(
                    "project files should depend on external modules matching 'Newtonsoft.*'"),
            },
            violations);
    }

    [Fact]
    public void DependOnExternalModules_honours_allow_empty_tests()
    {
        var rule = new Files(Graph(Self("a.cs")))
            .WithName("Car.cs")
            .Should()
            .DependOnExternalModules()
            .Matching("System.*");

        IReadOnlyList<Violation> violations = rule.Check(new CheckOptions { AllowEmptyTests = true });

        Assert.Empty(violations);
    }

    [Fact]
    public void Matching_rejects_a_null_glob()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new Files(Graph(Self("a.cs"))).Should().DependOnExternalModules().Matching(null!));
    }

    [Fact]
    public void Matching_rejects_an_empty_glob()
    {
        Assert.Throws<ArgumentException>(() =>
            new Files(Graph(Self("a.cs"))).Should().DependOnExternalModules().Matching(string.Empty));
    }

    private static Graph Graph(params Edge[] edges) => new(edges);

    private static Edge Self(string file) => new(file, file, external: false, ImportKind.None);

    private static Edge External(string source, string module) =>
        new(source, module, external: true, ImportKind.Using);
}
