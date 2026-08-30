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
    public void Select_reachable_from_follows_the_outgoing_transitive_closure()
    {
        var graph = Graph(
            Self("A/a.cs"),
            Self("B/b.cs"),
            Self("C/c.cs"),
            Self("D/d.cs"),
            Using("A/a.cs", "B/b.cs"),
            Using("B/b.cs", "C/c.cs"),
            Using("C/c.cs", "D/d.cs"));

        IReadOnlyList<string> scope = GraphProjection.Select(graph, ReachableFrom("A/a.cs"));

        Assert.Equal(new[] { "A/a.cs", "B/b.cs", "C/c.cs", "D/d.cs" }, scope);
    }

    [Fact]
    public void Select_dependents_of_follows_the_incoming_transitive_closure()
    {
        var graph = Graph(
            Self("A/a.cs"),
            Self("B/b.cs"),
            Self("C/c.cs"),
            Self("D/d.cs"),
            Using("A/a.cs", "D/d.cs"),
            Using("B/b.cs", "D/d.cs"),
            Using("C/c.cs", "B/b.cs"));

        IReadOnlyList<string> scope = GraphProjection.Select(graph, DependentsOf("D/d.cs"));

        Assert.Equal(new[] { "A/a.cs", "B/b.cs", "C/c.cs", "D/d.cs" }, scope);
    }

    [Fact]
    public void Select_intersects_the_set_restrictions()
    {
        var graph = Graph(
            Self("A/a.cs"),
            Self("B/b.cs"),
            Self("C/c.cs"),
            Using("A/a.cs", "B/b.cs"),
            Using("B/b.cs", "C/c.cs"));

        var options = GraphQueryOptions.Default with
        {
            Focus = new Filter(new Pattern("B/b.cs"), MatchTarget.Path),
            FocusDepth = 0,
            ReachableFrom = new Filter(new Pattern("A/a.cs"), MatchTarget.Path),
        };

        IReadOnlyList<string> scope = GraphProjection.Select(graph, options);

        Assert.Equal(new[] { "B/b.cs" }, scope);
    }

    [Fact]
    public void Select_never_traverses_external_targets()
    {
        var graph = Graph(
            Self("A/a.cs"),
            Self("B/b.cs"),
            Self("System.Linq"),
            Using("A/a.cs", "B/b.cs"),
            External("B/b.cs", "System.Linq"));

        IReadOnlyList<string> scope = GraphProjection.Select(graph, ReachableFrom("B/b.cs"));

        Assert.Equal(new[] { "B/b.cs" }, scope);
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
    public void Build_captures_every_file_as_a_node()
    {
        GraphSnapshot snapshot = GraphProjection.Build(Graph(
            Self("src/App/Program.cs"),
            Self("src/Models/Car.cs")), GraphQueryOptions.Default);

        Assert.Equal(
            new[] { "src/App/Program.cs", "src/Models/Car.cs" },
            snapshot.Nodes.Select(static n => n.Label));
    }

    [Fact]
    public void Build_captures_dependencies_between_distinct_files_only()
    {
        GraphSnapshot snapshot = GraphProjection.Build(Graph(
            Self("src/App/Program.cs"),
            Self("src/Models/Car.cs"),
            Using("src/App/Program.cs", "src/Models/Car.cs")), GraphQueryOptions.Default);

        IReadOnlyList<SnapshotEdge> edges = snapshot.Edges;

        Assert.Equal(
            new[] { ("src/App/Program.cs", "src/Models/Car.cs") },
            edges.Select(static e => (e.Source, e.Target)));
    }

    [Fact]
    public void Build_excludes_external_dependencies_by_default()
    {
        GraphSnapshot snapshot = GraphProjection.Build(Graph(
            Self("src/App/Program.cs"),
            External("src/App/Program.cs", "System.Linq")), GraphQueryOptions.Default);

        Assert.Empty(snapshot.Edges);
    }

    [Fact]
    public void Build_keeps_external_dependencies_when_included()
    {
        var options = GraphQueryOptions.Default with { IncludeExternalDependencies = true };
        GraphSnapshot snapshot = GraphProjection.Build(Graph(
            Self("src/App/Program.cs"),
            External("src/App/Program.cs", "System.Linq")), options);

        var edge = Assert.Single(snapshot.Edges);
        Assert.Equal("src/App/Program.cs", edge.Source);
        Assert.Equal("System.Linq", edge.Target);
        Assert.True(edge.External);
    }

    [Fact]
    public void Build_result_nodes_and_edges_are_sorted()
    {
        var graph = Graph(
            Self("Z/z.cs"),
            Self("A/a.cs"),
            Self("M/m.cs"),
            Using("Z/z.cs", "M/m.cs"),
            Using("M/m.cs", "A/a.cs"),
            Using("Z/z.cs", "A/a.cs"));

        GraphSnapshot snapshot = GraphProjection.Build(graph, GraphQueryOptions.Default);

        Assert.Equal(
            new[]
            {
                new SnapshotNode("A/a.cs", new[] { "A/a.cs" }),
                new SnapshotNode("M/m.cs", new[] { "M/m.cs" }),
                new SnapshotNode("Z/z.cs", new[] { "Z/z.cs" }),
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
    public void Build_result_nodes_are_sorted_by_label_even_when_collapse_reorders_them()
    {
        var graph = Graph(
            Self("A.b/c.cs"),
            Self("A/a.cs"));
        var options = GraphQueryOptions.Default with
        {
            Collapse = new CollapseRule[] { new CollapseRule.FolderDepth(1) },
        };

        GraphSnapshot snapshot = GraphProjection.Build(graph, options);

        Assert.Equal(
            new[]
            {
                new SnapshotNode("A", new[] { "A/a.cs" }),
                new SnapshotNode("A.b", new[] { "A.b/c.cs" }),
            },
            snapshot.Nodes);
    }

    [Fact]
    public void Build_merges_parallel_edges_into_one_edge_with_count_and_union_import_kinds()
    {
        GraphSnapshot snapshot = GraphProjection.Build(Graph(
            Self("A/a.cs"),
            Self("B/b.cs"),
            Using("A/a.cs", "B/b.cs"),
            new Edge("A/a.cs", "B/b.cs", external: false, ImportKind.UsingStatic)), GraphQueryOptions.Default);

        var edge = Assert.Single(snapshot.Edges);
        Assert.Equal(2, edge.Count);
        Assert.Equal(ImportKind.Using | ImportKind.UsingStatic, edge.ImportKinds);
        Assert.False(edge.External);
    }

    [Fact]
    public void Build_keeps_an_external_flag_when_every_merged_edge_was_external()
    {
        var options = GraphQueryOptions.Default with { IncludeExternalDependencies = true };
        GraphSnapshot snapshot = GraphProjection.Build(Graph(
            Self("A/a.cs"),
            External("A/a.cs", "System.Linq"),
            new Edge("A/a.cs", "System.Linq", external: true, ImportKind.UsingStatic)), options);

        var edge = Assert.Single(snapshot.Edges);
        Assert.True(edge.External);
        Assert.Equal(2, edge.Count);
        Assert.Equal(ImportKind.Using | ImportKind.UsingStatic, edge.ImportKinds);
    }

    [Fact]
    public void Build_excludes_the_marker_self_edge_by_default()
    {
        GraphSnapshot snapshot = GraphProjection.Build(Graph(Self("A/a.cs")), GraphQueryOptions.Default);

        Assert.Empty(snapshot.Edges);
    }

    [Fact]
    public void Build_includes_the_marker_self_edge_when_self_dependencies_are_included()
    {
        var options = GraphQueryOptions.Default with { IncludeSelfDependencies = true };
        GraphSnapshot snapshot = GraphProjection.Build(Graph(Self("A/a.cs")), options);

        var edge = Assert.Single(snapshot.Edges);
        Assert.Equal("A/a.cs", edge.Source);
        Assert.Equal("A/a.cs", edge.Target);
        Assert.Equal(1, edge.Count);
    }

    [Fact]
    public void Build_collapses_to_the_folder_at_the_given_depth()
    {
        var graph = Graph(
            Self("src/App/Program.cs"),
            Self("src/App/Startup.cs"),
            Self("src/Models/Car.cs"));
        var options = GraphQueryOptions.Default with
        {
            Collapse = new CollapseRule[] { new CollapseRule.FolderDepth(1) },
        };

        GraphSnapshot snapshot = GraphProjection.Build(graph, options);

        Assert.Equal(
            new[]
            {
                new SnapshotNode("src", new[] { "src/App/Program.cs", "src/App/Startup.cs", "src/Models/Car.cs" }),
            },
            snapshot.Nodes);
    }

    [Fact]
    public void Build_folder_depth_on_absolute_identifiers_skips_the_leading_separator()
    {
        var graph = Graph(
            Self("/src/App/Program.cs"),
            Self("/src/Models/Car.cs"));
        var options = GraphQueryOptions.Default with
        {
            Collapse = new CollapseRule[] { new CollapseRule.FolderDepth(1) },
        };

        GraphSnapshot snapshot = GraphProjection.Build(graph, options);

        Assert.Equal(
            new[]
            {
                new SnapshotNode("src", new[] { "/src/App/Program.cs", "/src/Models/Car.cs" }),
            },
            snapshot.Nodes);
    }

    [Fact]
    public void Build_folder_depth_on_an_absolute_root_level_file_uses_the_root_bucket()
    {
        var graph = Graph(
            Self("/Program.cs"),
            Self("/startup.cs"));
        var options = GraphQueryOptions.Default with
        {
            Collapse = new CollapseRule[] { new CollapseRule.FolderDepth(1) },
        };

        GraphSnapshot snapshot = GraphProjection.Build(graph, options);

        Assert.Equal(
            new[]
            {
                new SnapshotNode(GraphProjection.RootBucket, new[] { "/Program.cs", "/startup.cs" }),
            },
            snapshot.Nodes);
    }

    [Fact]
    public void Build_a_dependency_between_files_of_the_same_label_surfaces_as_a_self_loop()
    {
        var graph = Graph(
            Self("src/App/Program.cs"),
            Self("src/Models/Car.cs"),
            Using("src/App/Program.cs", "src/Models/Car.cs"));
        var options = GraphQueryOptions.Default with
        {
            Collapse = new CollapseRule[] { new CollapseRule.FolderDepth(1) },
        };

        GraphSnapshot snapshot = GraphProjection.Build(graph, options);

        var edge = Assert.Single(snapshot.Edges);
        Assert.Equal("src", edge.Source);
        Assert.Equal("src", edge.Target);
        Assert.Equal(1, edge.Count);
    }

    [Fact]
    public void Build_collapses_by_pattern_to_a_single_bucket()
    {
        var graph = Graph(
            Self("src/Models/Car.cs"),
            Self("src/Models/Engine.cs"),
            Self("src/App/Program.cs"));
        var options = GraphQueryOptions.Default with
        {
            Collapse = new CollapseRule[] { new CollapseRule.Pattern(new Filter(new Pattern("src/Models/**"), MatchTarget.Path)) },
        };

        GraphSnapshot snapshot = GraphProjection.Build(graph, options);

        Assert.Equal(
            new[]
            {
                new SnapshotNode("src/App/Program.cs", new[] { "src/App/Program.cs" }),
                new SnapshotNode("src/Models/**", new[] { "src/Models/Car.cs", "src/Models/Engine.cs" }),
            },
            snapshot.Nodes);
    }

    [Fact]
    public void Build_collapse_rules_apply_in_order_and_the_first_match_wins()
    {
        var graph = Graph(
            Self("A/Models/Car.cs"),
            Self("B/Service.cs"));
        var options = GraphQueryOptions.Default with
        {
            Collapse = new CollapseRule[]
            {
                new CollapseRule.Pattern(new Filter(new Pattern("A/**"), MatchTarget.Path)),
                new CollapseRule.FolderDepth(1),
            },
        };

        GraphSnapshot snapshot = GraphProjection.Build(graph, options);

        Assert.Equal(
            new[]
            {
                new SnapshotNode("A/**", new[] { "A/Models/Car.cs" }),
                new SnapshotNode("B", new[] { "B/Service.cs" }),
            },
            snapshot.Nodes);
    }

    [Fact]
    public void Build_a_folder_depth_rule_relabels_every_file_so_later_rules_are_unreachable()
    {
        var graph = Graph(
            Self("A/Models/Car.cs"),
            Self("B/Service.cs"));
        var options = GraphQueryOptions.Default with
        {
            Collapse = new CollapseRule[]
            {
                new CollapseRule.FolderDepth(1),
                new CollapseRule.Pattern(new Filter(new Pattern("A/**"), MatchTarget.Path)),
            },
        };

        GraphSnapshot snapshot = GraphProjection.Build(graph, options);

        Assert.Equal(
            new[]
            {
                new SnapshotNode("A", new[] { "A/Models/Car.cs" }),
                new SnapshotNode("B", new[] { "B/Service.cs" }),
            },
            snapshot.Nodes);
    }

    [Fact]
    public void Build_carries_the_title()
    {
        var options = GraphQueryOptions.Default with { Title = "The app" };

        GraphSnapshot snapshot = GraphProjection.Build(Graph(Self("a.cs")), options);

        Assert.Equal("The app", snapshot.Title);
    }

    [Fact]
    public void Build_summary_counts_reflect_the_snapshot()
    {
        var graph = Graph(
            Self("A/a.cs"),
            Self("A/b.cs"),
            Self("B/c.cs"),
            Using("A/a.cs", "B/c.cs"),
            Using("A/b.cs", "B/c.cs"));
        var options = GraphQueryOptions.Default with
        {
            Collapse = new CollapseRule[] { new CollapseRule.FolderDepth(1) },
        };

        GraphSnapshot snapshot = GraphProjection.Build(graph, options);

        Assert.Equal(2, snapshot.NodeCount);
        Assert.Equal(1, snapshot.EdgeCount);
        Assert.Equal(3, snapshot.FileCount);
        Assert.Equal(2, Assert.Single(snapshot.Edges).Count);
    }

    [Fact]
    public void Build_nodes_return_a_fresh_copy_on_every_read()
    {
        GraphSnapshot snapshot = GraphProjection.Build(Graph(
            Self("A/a.cs"),
            Self("B/b.cs")), GraphQueryOptions.Default);

        ((SnapshotNode[])snapshot.Nodes)[0] = null!;

        Assert.Equal(new[] { "A/a.cs", "B/b.cs" }, snapshot.Nodes.Select(static n => n.Label));
    }

    [Fact]
    public void Build_is_identical_when_built_twice_from_the_same_graph()
    {
        var graph = Graph(
            Self("A/a.cs"),
            Self("B/b.cs"),
            Using("A/a.cs", "B/b.cs"));

        Assert.Equal(
            GraphProjection.Build(graph, GraphQueryOptions.Default),
            GraphProjection.Build(graph, GraphQueryOptions.Default));
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

    private static KernelGraph Graph(params Edge[] edges) => new(edges);

    private static Edge Self(string file) => new(file, file, external: false, ImportKind.None);

    private static Edge Using(string source, string target) =>
        new(source, target, external: false, ImportKind.Using);

    private static Edge External(string source, string module) =>
        new(source, module, external: true, ImportKind.Using);

    private static GraphQueryOptions Focused(string glob, int depth) =>
        GraphQueryOptions.Default with
        {
            Focus = new Filter(new Pattern(glob), MatchTarget.Path),
            FocusDepth = depth,
        };

    private static GraphQueryOptions ReachableFrom(string glob) =>
        GraphQueryOptions.Default with
        {
            ReachableFrom = new Filter(new Pattern(glob), MatchTarget.Path),
        };

    private static GraphQueryOptions DependentsOf(string glob) =>
        GraphQueryOptions.Default with
        {
            DependentsOf = new Filter(new Pattern(glob), MatchTarget.Path),
        };
}
