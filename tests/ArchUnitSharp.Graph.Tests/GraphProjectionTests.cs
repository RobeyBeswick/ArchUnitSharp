using ArchUnitSharp.Common.Extraction;
using ArchUnitSharp.Graph.Projection;
using KernelGraph = ArchUnitSharp.Common.Extraction.Graph;

namespace ArchUnitSharp.Graph.Tests;

public class GraphProjectionTests
{
    [Fact]
    public void Select_returns_every_file_sorted_with_no_restrictions()
    {
        var graph = Graph(
            Self("Z/z.cs"),
            Self("A/a.cs"),
            Self("M/m.cs"));

        IReadOnlyList<string> scope = GraphProjection.Select(graph, GraphQueryOptions.Default);

        Assert.Equal(new[] { "A/a.cs", "M/m.cs", "Z/z.cs" }, scope);
    }

    [Fact]
    public void Select_focus_at_depth_zero_selects_only_the_matching_files()
    {
        var graph = Graph(
            Self("A/a.cs"),
            Self("B/b.cs"),
            Self("C/c.cs"),
            Using("A/a.cs", "B/b.cs"),
            Using("C/c.cs", "A/a.cs"));

        IReadOnlyList<string> scope = GraphProjection.Select(graph, Focused("A/a.cs", depth: 0));

        Assert.Equal(new[] { "A/a.cs" }, scope);
    }

    [Fact]
    public void Select_focus_at_depth_one_selects_incoming_and_outgoing_neighbours()
    {
        var graph = Graph(
            Self("A/a.cs"),
            Self("B/b.cs"),
            Self("C/c.cs"),
            Self("D/d.cs"),
            Self("E/e.cs"),
            Using("A/a.cs", "B/b.cs"),
            Using("B/b.cs", "C/c.cs"),
            Using("D/d.cs", "A/a.cs"),
            Using("E/e.cs", "D/d.cs"));

        IReadOnlyList<string> scope = GraphProjection.Select(graph, Focused("A/a.cs", depth: 1));

        Assert.Equal(new[] { "A/a.cs", "B/b.cs", "D/d.cs" }, scope);
    }

    [Fact]
    public void Select_focus_at_depth_two_selects_the_wider_neighbourhood()
    {
        var graph = Graph(
            Self("A/a.cs"),
            Self("B/b.cs"),
            Self("C/c.cs"),
            Self("D/d.cs"),
            Self("E/e.cs"),
            Using("A/a.cs", "B/b.cs"),
            Using("B/b.cs", "C/c.cs"),
            Using("D/d.cs", "A/a.cs"),
            Using("E/e.cs", "D/d.cs"));

        IReadOnlyList<string> scope = GraphProjection.Select(graph, Focused("A/a.cs", depth: 2));

        Assert.Equal(new[] { "A/a.cs", "B/b.cs", "C/c.cs", "D/d.cs", "E/e.cs" }, scope);
    }

    [Fact]
    public void Select_reachable_from_follows_the_transitive_closure()
    {
        var graph = Graph(
            Self("A/a.cs"),
            Self("B/b.cs"),
            Self("C/c.cs"),
            Self("D/d.cs"),
            Using("A/a.cs", "B/b.cs"),
            Using("B/b.cs", "C/c.cs"),
            Using("D/d.cs", "A/a.cs"));

        IReadOnlyList<string> scope = GraphProjection.Select(graph, ReachableFrom("A/a.cs"));

        Assert.Equal(new[] { "A/a.cs", "B/b.cs", "C/c.cs" }, scope);
    }

    [Fact]
    public void Select_dependents_of_follows_incoming_edges_backwards()
    {
        var graph = Graph(
            Self("A/a.cs"),
            Self("B/b.cs"),
            Self("C/c.cs"),
            Self("D/d.cs"),
            Self("E/e.cs"),
            Using("A/a.cs", "B/b.cs"),
            Using("B/b.cs", "C/c.cs"),
            Using("D/d.cs", "A/a.cs"),
            Using("E/e.cs", "D/d.cs"));

        IReadOnlyList<string> scope = GraphProjection.Select(graph, DependentsOf("A/a.cs"));

        Assert.Equal(new[] { "A/a.cs", "D/d.cs", "E/e.cs" }, scope);
    }

    [Fact]
    public void Select_intersects_the_applied_restrictions()
    {
        var graph = Graph(
            Self("A/a.cs"),
            Self("B/b.cs"),
            Self("C/c.cs"),
            Self("D/d.cs"),
            Self("E/e.cs"),
            Using("A/a.cs", "B/b.cs"),
            Using("B/b.cs", "C/c.cs"),
            Using("D/d.cs", "A/a.cs"),
            Using("E/e.cs", "D/d.cs"));

        var options = GraphQueryOptions.Default with
        {
            ReachableFrom = PathOf("A/a.cs"),
            DependentsOf = PathOf("B/b.cs"),
        };

        IReadOnlyList<string> scope = GraphProjection.Select(graph, options);

        Assert.Equal(new[] { "A/a.cs", "B/b.cs" }, scope);
    }

    [Fact]
    public void Select_returns_nothing_when_a_restriction_matches_no_files()
    {
        var graph = Graph(Self("A/a.cs"));

        IReadOnlyList<string> scope = GraphProjection.Select(graph, ReachableFrom("No/Such/File.cs"));

        Assert.Empty(scope);
    }

    [Fact]
    public void Select_does_not_traverse_external_edges()
    {
        var graph = Graph(
            Self("A/a.cs"),
            Self("B/b.cs"),
            Self("C/c.cs"),
            Self("System.Linq"),
            Using("A/a.cs", "B/b.cs"),
            External("B/b.cs", "System.Linq"),
            Using("C/c.cs", "B/b.cs"));

        IReadOnlyList<string> scope = GraphProjection.Select(graph, DependentsOf("System.Linq"));

        Assert.Equal(new[] { "System.Linq" }, scope);
    }

    [Fact]
    public void Select_rejects_a_null_graph()
    {
        Assert.Throws<ArgumentNullException>(() => GraphProjection.Select(null!, GraphQueryOptions.Default));
    }

    [Fact]
    public void Select_rejects_null_options()
    {
        Assert.Throws<ArgumentNullException>(() => GraphProjection.Select(Graph(Self("a.cs")), null!));
    }

    [Fact]
    public void Build_makes_every_file_a_node_with_no_restrictions()
    {
        var graph = Graph(
            Self("A/a.cs"),
            Self("B/b.cs"),
            Self("C/c.cs"));

        GraphSnapshot snapshot = GraphProjection.Build(graph, GraphQueryOptions.Default);

        Assert.Equal(
            new[]
            {
                new SnapshotNode("A/a.cs", new[] { "A/a.cs" }, external: false),
                new SnapshotNode("B/b.cs", new[] { "B/b.cs" }, external: false),
                new SnapshotNode("C/c.cs", new[] { "C/c.cs" }, external: false),
            },
            snapshot.Nodes);
        Assert.Empty(snapshot.Edges);
    }

    [Fact]
    public void Build_aggregates_parallel_dependencies_into_one_edge_with_a_count()
    {
        var graph = Graph(
            Self("A/a.cs"),
            Self("B/b.cs"),
            Using("A/a.cs", "B/b.cs"),
            UsingStatic("A/a.cs", "B/b.cs"));

        GraphSnapshot snapshot = GraphProjection.Build(graph, GraphQueryOptions.Default);

        Assert.Equal(
            new[]
            {
                new SnapshotEdge("A/a.cs", "B/b.cs", count: 2, external: false, ImportKind.Using | ImportKind.UsingStatic),
            },
            snapshot.Edges);
    }

    [Fact]
    public void Build_reports_each_distinct_dependency_pair_separately()
    {
        var graph = Graph(
            Self("A/a.cs"),
            Self("B/b.cs"),
            Self("C/c.cs"),
            Using("A/a.cs", "B/b.cs"),
            Using("A/a.cs", "C/c.cs"));

        GraphSnapshot snapshot = GraphProjection.Build(graph, GraphQueryOptions.Default);

        Assert.Equal(
            new[]
            {
                new SnapshotEdge("A/a.cs", "B/b.cs", count: 1, external: false, ImportKind.Using),
                new SnapshotEdge("A/a.cs", "C/c.cs", count: 1, external: false, ImportKind.Using),
            },
            snapshot.Edges);
    }

    [Fact]
    public void Build_excludes_external_dependencies_by_default()
    {
        var graph = Graph(
            Self("A/a.cs"),
            External("A/a.cs", "System.Linq"));

        GraphSnapshot snapshot = GraphProjection.Build(graph, GraphQueryOptions.Default);

        Assert.Equal(
            new[] { new SnapshotNode("A/a.cs", new[] { "A/a.cs" }, external: false) },
            snapshot.Nodes);
        Assert.Empty(snapshot.Edges);
    }

    [Fact]
    public void Build_includes_external_dependencies_and_their_targets_with_the_option()
    {
        var graph = Graph(
            Self("A/a.cs"),
            Self("B/b.cs"),
            External("A/a.cs", "System.Linq"),
            External("B/b.cs", "System.Linq"));

        var options = GraphQueryOptions.Default with { IncludeExternalDependencies = true };

        GraphSnapshot snapshot = GraphProjection.Build(graph, options);

        Assert.Equal(
            new[]
            {
                new SnapshotNode("A/a.cs", new[] { "A/a.cs" }, external: false),
                new SnapshotNode("B/b.cs", new[] { "B/b.cs" }, external: false),
                new SnapshotNode("System.Linq", Array.Empty<string>(), external: true),
            },
            snapshot.Nodes);
        Assert.Equal(
            new[]
            {
                new SnapshotEdge("A/a.cs", "System.Linq", count: 1, external: true, ImportKind.Using),
                new SnapshotEdge("B/b.cs", "System.Linq", count: 1, external: true, ImportKind.Using),
            },
            snapshot.Edges);
    }

    [Fact]
    public void Build_excludes_self_edges_by_default()
    {
        var graph = Graph(
            Self("A/a.cs"),
            Self("B/b.cs"),
            Using("A/a.cs", "B/b.cs"));

        GraphSnapshot snapshot = GraphProjection.Build(graph, GraphQueryOptions.Default);

        Assert.Equal(
            new[] { new SnapshotEdge("A/a.cs", "B/b.cs", count: 1, external: false, ImportKind.Using) },
            snapshot.Edges);
    }

    [Fact]
    public void Build_includes_a_self_loop_per_file_with_the_option()
    {
        var graph = Graph(
            Self("A/a.cs"),
            Self("B/b.cs"),
            Using("A/a.cs", "B/b.cs"));

        var options = GraphQueryOptions.Default with { IncludeSelfDependencies = true };

        GraphSnapshot snapshot = GraphProjection.Build(graph, options);

        Assert.Equal(
            new[]
            {
                new SnapshotEdge("A/a.cs", "A/a.cs", count: 1, external: false, ImportKind.None),
                new SnapshotEdge("A/a.cs", "B/b.cs", count: 1, external: false, ImportKind.Using),
                new SnapshotEdge("B/b.cs", "B/b.cs", count: 1, external: false, ImportKind.None),
            },
            snapshot.Edges);
    }

    [Fact]
    public void Build_collapses_to_folder_depth_and_aggregates()
    {
        var graph = Graph(
            Self("src/App/Program.cs"),
            Self("src/App/Web/Page.cs"),
            Self("src/Models/Car.cs"),
            Self("Root.cs"),
            Using("src/App/Program.cs", "src/Models/Car.cs"),
            Using("src/App/Web/Page.cs", "src/Models/Car.cs"),
            Using("src/App/Program.cs", "src/App/Web/Page.cs"));

        var options = GraphQueryOptions.Default with
        {
            Collapse = new[] { new CollapseRule.FolderDepth(2) },
        };

        GraphSnapshot snapshot = GraphProjection.Build(graph, options);

        Assert.Equal(
            new[]
            {
                new SnapshotNode(
                    ".",
                    new[] { "Root.cs" },
                    external: false),
                new SnapshotNode(
                    "src/App",
                    new[] { "src/App/Program.cs", "src/App/Web/Page.cs" },
                    external: false),
                new SnapshotNode(
                    "src/Models",
                    new[] { "src/Models/Car.cs" },
                    external: false),
            },
            snapshot.Nodes);
        Assert.Equal(
            new[]
            {
                new SnapshotEdge("src/App", "src/App", count: 1, external: false, ImportKind.Using),
                new SnapshotEdge("src/App", "src/Models", count: 2, external: false, ImportKind.Using),
            },
            snapshot.Edges);
    }

    [Fact]
    public void Build_collapse_at_depth_one_folds_every_folder_into_the_src_bucket()
    {
        var graph = Graph(
            Self("src/App/Program.cs"),
            Self("src/Models/Car.cs"),
            Using("src/App/Program.cs", "src/Models/Car.cs"));

        var options = GraphQueryOptions.Default with
        {
            Collapse = new[] { new CollapseRule.FolderDepth(1) },
        };

        GraphSnapshot snapshot = GraphProjection.Build(graph, options);

        Assert.Equal(
            new[]
            {
                new SnapshotNode("src", new[] { "src/App/Program.cs", "src/Models/Car.cs" }, external: false),
            },
            snapshot.Nodes);
        Assert.Equal(
            new[]
            {
                new SnapshotEdge("src", "src", count: 1, external: false, ImportKind.Using),
            },
            snapshot.Edges);
    }

    [Fact]
    public void Build_collapses_by_pattern_to_a_bucket_labeled_with_the_glob()
    {
        var graph = Graph(
            Self("src/App/Program.cs"),
            Self("src/App/Web/Page.cs"),
            Self("src/Models/Car.cs"),
            Using("src/App/Program.cs", "src/Models/Car.cs"));

        var options = GraphQueryOptions.Default with
        {
            Collapse = new[] { new CollapseRule.Pattern(PathOf("src/App/**")) },
        };

        GraphSnapshot snapshot = GraphProjection.Build(graph, options);

        Assert.Equal(
            new[]
            {
                new SnapshotNode("src/App/**", new[] { "src/App/Program.cs", "src/App/Web/Page.cs" }, external: false),
                new SnapshotNode("src/Models/Car.cs", new[] { "src/Models/Car.cs" }, external: false),
            },
            snapshot.Nodes);
    }

    [Fact]
    public void Build_applies_collapse_rules_in_order_first_match_wins()
    {
        var graph = Graph(
            Self("src/App/Program.cs"),
            Self("src/Models/Car.cs"),
            Self("src/Shared/Thing.cs"),
            Using("src/App/Program.cs", "src/Models/Car.cs"));

        var options = GraphQueryOptions.Default with
        {
            Collapse = new CollapseRule[]
            {
                new CollapseRule.Pattern(PathOf("**/Models/**")),
                new CollapseRule.FolderDepth(2),
            },
        };

        GraphSnapshot snapshot = GraphProjection.Build(graph, options);

        Assert.Equal(
            new[]
            {
                new SnapshotNode("**/Models/**", new[] { "src/Models/Car.cs" }, external: false),
                new SnapshotNode("src/App", new[] { "src/App/Program.cs" }, external: false),
                new SnapshotNode("src/Shared", new[] { "src/Shared/Thing.cs" }, external: false),
            },
            snapshot.Nodes);
    }

    [Fact]
    public void Build_drops_edges_whose_target_is_outside_the_scope()
    {
        var graph = Graph(
            Self("A/a.cs"),
            Self("B/b.cs"),
            Using("A/a.cs", "B/b.cs"));

        GraphSnapshot snapshot = GraphProjection.Build(graph, Focused("A/a.cs", depth: 0));

        Assert.Equal(
            new[]
            {
                new SnapshotNode("A/a.cs", new[] { "A/a.cs" }, external: false),
            },
            snapshot.Nodes);
        Assert.Empty(snapshot.Edges);
    }

    [Fact]
    public void Build_counts_the_summary_counts()
    {
        var graph = Graph(
            Self("src/App/Program.cs"),
            Self("src/App/Web/Page.cs"),
            Self("src/Models/Car.cs"),
            Using("src/App/Program.cs", "src/Models/Car.cs"),
            External("src/App/Program.cs", "System.Linq"));

        var options = GraphQueryOptions.Default with
        {
            Collapse = new[] { new CollapseRule.FolderDepth(1) },
            IncludeExternalDependencies = true,
        };

        GraphSnapshot snapshot = GraphProjection.Build(graph, options);

        Assert.Equal(2, snapshot.NodeCount);
        Assert.Equal(2, snapshot.EdgeCount);
        Assert.Equal(3, snapshot.FileCount);
    }

    [Fact]
    public void Build_result_nodes_and_edges_are_sorted()
    {
        var graph = Graph(
            Self("Z/z.cs"),
            Self("A/a.cs"),
            Self("M/m.cs"),
            Using("M/m.cs", "A/a.cs"),
            Using("Z/z.cs", "A/a.cs"),
            Using("Z/z.cs", "M/m.cs"));

        GraphSnapshot snapshot = GraphProjection.Build(graph, GraphQueryOptions.Default);

        Assert.Equal(
            new[]
            {
                new SnapshotNode("A/a.cs", new[] { "A/a.cs" }, external: false),
                new SnapshotNode("M/m.cs", new[] { "M/m.cs" }, external: false),
                new SnapshotNode("Z/z.cs", new[] { "Z/z.cs" }, external: false),
            },
            snapshot.Nodes);
        Assert.Equal(
            new[]
            {
                new SnapshotEdge("M/m.cs", "A/a.cs", count: 1, external: false, ImportKind.Using),
                new SnapshotEdge("Z/z.cs", "A/a.cs", count: 1, external: false, ImportKind.Using),
                new SnapshotEdge("Z/z.cs", "M/m.cs", count: 1, external: false, ImportKind.Using),
            },
            snapshot.Edges);
    }

    [Fact]
    public void Build_rejects_a_null_graph()
    {
        Assert.Throws<ArgumentNullException>(() => GraphProjection.Build(null!, GraphQueryOptions.Default));
    }

    [Fact]
    public void Build_rejects_null_options()
    {
        Assert.Throws<ArgumentNullException>(() => GraphProjection.Build(Graph(Self("a.cs")), null!));
    }

    private static GraphQueryOptions Focused(string glob, int depth) =>
        GraphQueryOptions.Default with { Focus = PathOf(glob), FocusDepth = depth };

    private static GraphQueryOptions ReachableFrom(string glob) =>
        GraphQueryOptions.Default with { ReachableFrom = PathOf(glob) };

    private static GraphQueryOptions DependentsOf(string glob) =>
        GraphQueryOptions.Default with { DependentsOf = PathOf(glob) };

    private static Filter PathOf(string glob) => new(new Pattern(glob), MatchTarget.Path);

    private static KernelGraph Graph(params Edge[] edges) => new(edges);

    private static Edge Self(string file) => new(file, file, external: false, ImportKind.None);

    private static Edge Using(string source, string target) =>
        new(source, target, external: false, ImportKind.Using);

    private static Edge UsingStatic(string source, string target) =>
        new(source, target, external: false, ImportKind.UsingStatic);

    private static Edge External(string source, string module) =>
        new(source, module, external: true, ImportKind.Using);
}
