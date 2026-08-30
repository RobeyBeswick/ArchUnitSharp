using ArchUnitSharp.Common.Extraction;
using KernelGraph = ArchUnitSharp.Common.Extraction.Graph;

namespace ArchUnitSharp.Graph.Tests;

public class GraphReportTests
{
    [Fact]
    public void A_full_query_builds_a_snapshot_through_the_fluent_chain()
    {
        var report = new GraphReport(Graph(
                Self("src/App/Program.cs"),
                Self("src/App/Web/Page.cs"),
                Self("src/Models/Car.cs"),
                Using("src/App/Program.cs", "src/Models/Car.cs"),
                Using("src/App/Web/Page.cs", "src/Models/Car.cs"),
                Using("src/App/Program.cs", "src/App/Web/Page.cs")))
            .IncludingExternalDependencies()
            .CollapsedToFolderDepth(2)
            .Titled("The app");

        GraphSnapshot snapshot = report.Build();

        Assert.Equal("The app", snapshot.Title);
        Assert.Equal(2, snapshot.NodeCount);
        Assert.Equal(
            new[]
            {
                new SnapshotEdge("src/App", "src/App", count: 1, external: false, ImportKind.Using),
                new SnapshotEdge("src/App", "src/Models", count: 2, external: false, ImportKind.Using),
            },
            snapshot.Edges);
    }

    [Fact]
    public void Reachable_from_and_dependents_of_shape_the_snapshot_through_the_fluent_chain()
    {
        var report = new GraphReport(Graph(
                Self("A/a.cs"),
                Self("B/b.cs"),
                Self("C/c.cs"),
                Self("D/d.cs"),
                Self("E/e.cs"),
                Using("A/a.cs", "B/b.cs"),
                Using("B/b.cs", "C/c.cs"),
                Using("D/d.cs", "A/a.cs"),
                Using("E/e.cs", "D/d.cs")))
            .ReachableFrom("A/a.cs")
            .DependentsOf("B/b.cs");

        GraphSnapshot snapshot = report.Build();

        Assert.Equal(
            new[]
            {
                new SnapshotNode("A/a.cs", new[] { "A/a.cs" }, external: false),
                new SnapshotNode("B/b.cs", new[] { "B/b.cs" }, external: false),
            },
            snapshot.Nodes);
        Assert.Equal(
            new[]
            {
                new SnapshotEdge("A/a.cs", "B/b.cs", count: 1, external: false, ImportKind.Using),
            },
            snapshot.Edges);
    }

    [Fact]
    public void FocusingOn_restricts_the_built_snapshot_to_the_neighbourhood()
    {
        var report = new GraphReport(Graph(
                Self("A/a.cs"),
                Self("B/b.cs"),
                Self("C/c.cs"),
                Self("D/d.cs"),
                Self("E/e.cs"),
                Using("A/a.cs", "B/b.cs"),
                Using("B/b.cs", "C/c.cs"),
                Using("D/d.cs", "A/a.cs"),
                Using("E/e.cs", "D/d.cs")))
            .FocusingOn("A/a.cs", depth: 1);

        GraphSnapshot snapshot = report.Build();

        Assert.Equal(
            new[]
            {
                new SnapshotNode("A/a.cs", new[] { "A/a.cs" }, external: false),
                new SnapshotNode("B/b.cs", new[] { "B/b.cs" }, external: false),
                new SnapshotNode("D/d.cs", new[] { "D/d.cs" }, external: false),
            },
            snapshot.Nodes);
        Assert.Equal(
            new[]
            {
                new SnapshotEdge("A/a.cs", "B/b.cs", count: 1, external: false, ImportKind.Using),
                new SnapshotEdge("D/d.cs", "A/a.cs", count: 1, external: false, ImportKind.Using),
            },
            snapshot.Edges);
    }

    [Fact]
    public void CollapsedByPattern_collapses_matching_files_through_the_fluent_chain()
    {
        var report = new GraphReport(Graph(
                Self("src/App/Program.cs"),
                Self("src/App/Web/Page.cs"),
                Self("src/Models/Car.cs"),
                Using("src/App/Program.cs", "src/Models/Car.cs")))
            .CollapsedByPattern("src/App/**");

        GraphSnapshot snapshot = report.Build();

        Assert.Equal(
            new[]
            {
                new SnapshotNode("src/App/**", new[] { "src/App/Program.cs", "src/App/Web/Page.cs" }, external: false),
                new SnapshotNode("src/Models/Car.cs", new[] { "src/Models/Car.cs" }, external: false),
            },
            snapshot.Nodes);
        Assert.Equal(
            new[]
            {
                new SnapshotEdge("src/App/**", "src/Models/Car.cs", count: 1, external: false, ImportKind.Using),
            },
            snapshot.Edges);
    }

    [Fact]
    public void A_query_can_be_built_twice_and_reports_the_same_snapshot()
    {
        var report = new GraphReport(Graph(
                Self("A/a.cs"),
                Self("B/b.cs"),
                Using("A/a.cs", "B/b.cs")))
            .Titled("twice");

        GraphSnapshot first = report.Build();
        GraphSnapshot second = report.Build();

        Assert.Equal(first, second);
        Assert.NotSame(first, second);
    }

    [Fact]
    public void Two_branches_off_one_parent_do_not_see_each_others_options()
    {
        var parent = new GraphReport(Graph(
                Self("A/a.cs"),
                Self("B/b.cs"),
                Using("A/a.cs", "B/b.cs")))
            .CollapsedToFolderDepth(2);

        var withExternal = parent.IncludingExternalDependencies();
        var withSelf = parent.IncludingSelfDependencies();

        Assert.False(parent.QueryOptions.IncludeExternalDependencies);
        Assert.False(parent.QueryOptions.IncludeSelfDependencies);
        Assert.True(withExternal.QueryOptions.IncludeExternalDependencies);
        Assert.False(withExternal.QueryOptions.IncludeSelfDependencies);
        Assert.True(withSelf.QueryOptions.IncludeSelfDependencies);
        Assert.False(withSelf.QueryOptions.IncludeExternalDependencies);
    }

    [Fact]
    public void Two_branches_off_one_parent_do_not_see_each_others_collapse_rules()
    {
        var parent = new GraphReport(Graph(Self("A/a.cs")))
            .CollapsedToFolderDepth(2);

        var byPattern = parent.CollapsedByPattern("**/Models/**");
        var deeper = parent.CollapsedToFolderDepth(3);

        Assert.Single(parent.QueryOptions.Collapse);
        Assert.Equal(2, byPattern.QueryOptions.Collapse.Length);
        Assert.Equal(2, deeper.QueryOptions.Collapse.Length);
    }

    [Fact]
    public void Collapse_rules_are_copied_before_they_leave_the_builder()
    {
        var report = new GraphReport(Graph(Self("A/a.cs")))
            .CollapsedByPattern("**/Models/**");

        CollapseRule[] rules = report.QueryOptions.Collapse;
        rules[0] = new CollapseRule.FolderDepth(7);

        Assert.IsType<CollapseRule.Pattern>(report.QueryOptions.Collapse[0]);
    }

    [Fact]
    public void A_scope_that_matches_nothing_is_an_empty_test()
    {
        var report = new GraphReport(Graph(Self("A/a.cs"))).ReachableFrom("No/Such/File.cs");

        IReadOnlyList<Violation> violations = report.Check();

        Assert.Equal(
            new Violation[] { new EmptyTestViolation("project graph reachable from 'No/Such/File.cs'") },
            violations);
    }

    [Fact]
    public void An_empty_project_graph_is_an_empty_test()
    {
        var report = new GraphReport(Graph());

        IReadOnlyList<Violation> violations = report.Check();

        Assert.Equal(new Violation[] { new EmptyTestViolation("project graph") }, violations);
    }

    [Fact]
    public void An_empty_test_honours_the_queries_own_check_options()
    {
        var report = new GraphReport(Graph(Self("A/a.cs")))
            .ReachableFrom("No/Such/File.cs")
            .WithCheckOptions(new CheckOptions { AllowEmptyTests = true });

        Assert.Empty(report.Check());
    }

    [Fact]
    public void Check_options_passed_to_check_override_the_queries_own()
    {
        var report = new GraphReport(Graph(Self("A/a.cs"))).ReachableFrom("No/Such/File.cs");

        Assert.Empty(report.Check(new CheckOptions { AllowEmptyTests = true }));
        Assert.Equal(
            new Violation[] { new EmptyTestViolation("project graph reachable from 'No/Such/File.cs'") },
            report.Check());
    }

    [Fact]
    public void A_scope_that_matches_some_files_passes()
    {
        var report = new GraphReport(Graph(Self("A/a.cs")));

        Assert.Empty(report.Check());
    }

    [Fact]
    public void A_query_can_be_checked_twice_and_reports_the_same_result()
    {
        var report = new GraphReport(Graph(Self("A/a.cs"))).ReachableFrom("No/Such/File.cs");

        IReadOnlyList<Violation> first = report.Check();
        IReadOnlyList<Violation> second = report.Check();

        Assert.Equal(first, second);
        Assert.NotSame(first, second);
    }

    [Fact]
    public void FocusingOn_rejects_a_null_glob()
    {
        Assert.Throws<ArgumentNullException>(() => new GraphReport(Graph(Self("a.cs"))).FocusingOn(null!, 1));
    }

    [Fact]
    public void FocusingOn_rejects_an_empty_glob()
    {
        Assert.Throws<ArgumentException>(() => new GraphReport(Graph(Self("a.cs"))).FocusingOn(string.Empty, 1));
    }

    [Fact]
    public void FocusingOn_rejects_a_negative_depth()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new GraphReport(Graph(Self("a.cs"))).FocusingOn("**/*.cs", -1));
    }

    [Fact]
    public void ReachableFrom_rejects_a_null_glob()
    {
        Assert.Throws<ArgumentNullException>(() => new GraphReport(Graph(Self("a.cs"))).ReachableFrom(null!));
    }

    [Fact]
    public void ReachableFrom_rejects_an_empty_glob()
    {
        Assert.Throws<ArgumentException>(() => new GraphReport(Graph(Self("a.cs"))).ReachableFrom(string.Empty));
    }

    [Fact]
    public void DependentsOf_rejects_a_null_glob()
    {
        Assert.Throws<ArgumentNullException>(() => new GraphReport(Graph(Self("a.cs"))).DependentsOf(null!));
    }

    [Fact]
    public void DependentsOf_rejects_an_empty_glob()
    {
        Assert.Throws<ArgumentException>(() => new GraphReport(Graph(Self("a.cs"))).DependentsOf(string.Empty));
    }

    [Fact]
    public void CollapsedToFolderDepth_rejects_a_negative_depth()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new GraphReport(Graph(Self("a.cs"))).CollapsedToFolderDepth(-1));
    }

    [Fact]
    public void CollapsedByPattern_rejects_a_null_glob()
    {
        Assert.Throws<ArgumentNullException>(() => new GraphReport(Graph(Self("a.cs"))).CollapsedByPattern(null!));
    }

    [Fact]
    public void CollapsedByPattern_rejects_an_empty_glob()
    {
        Assert.Throws<ArgumentException>(() => new GraphReport(Graph(Self("a.cs"))).CollapsedByPattern(string.Empty));
    }

    [Fact]
    public void Titled_rejects_a_null_title()
    {
        Assert.Throws<ArgumentNullException>(() => new GraphReport(Graph(Self("a.cs"))).Titled(null!));
    }

    [Fact]
    public void Titled_allows_an_empty_title()
    {
        var report = new GraphReport(Graph(Self("a.cs"))).Titled(string.Empty);

        Assert.Equal(string.Empty, report.Build().Title);
    }

    [Fact]
    public void WithCheckOptions_rejects_null_options()
    {
        Assert.Throws<ArgumentNullException>(() => new GraphReport(Graph(Self("a.cs"))).WithCheckOptions(null!));
    }

    [Fact]
    public void The_builder_rejects_a_null_graph()
    {
        Assert.Throws<ArgumentNullException>(() => new GraphReport(null!));
    }

    private static KernelGraph Graph(params Edge[] edges) => new(edges);

    private static Edge Self(string file) => new(file, file, external: false, ImportKind.None);

    private static Edge Using(string source, string target) =>
        new(source, target, external: false, ImportKind.Using);
}
