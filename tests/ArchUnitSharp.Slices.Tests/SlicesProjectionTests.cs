using ArchUnitSharp.Common.Extraction;
using ArchUnitSharp.Projection;
using ArchUnitSharp.Slices.Projection;

namespace ArchUnitSharp.Slices.Tests;

public class SlicesProjectionTests
{
    [Fact]
    public void SliceOf_names_a_file_by_its_capture()
    {
        var definitions = new[] { ByPattern("src/features/(**)/*.cs") };

        Assert.Equal("billing", SlicesProjection.SliceOf(definitions, "src/features/billing/order.cs"));
        Assert.Equal("auth", SlicesProjection.SliceOf(definitions, "src/features/auth/login.cs"));
    }

    [Fact]
    public void SliceOf_returns_nothing_for_a_file_that_does_not_match()
    {
        var definitions = new[] { ByPattern("src/features/(**)/*.cs") };

        Assert.Null(SlicesProjection.SliceOf(definitions, "src/legacy/Old.cs"));
    }

    [Fact]
    public void SliceOf_returns_nothing_for_an_empty_capture()
    {
        var definitions = new[] { ByPattern("src/features/(**)/*.cs") };

        Assert.Null(SlicesProjection.SliceOf(definitions, "src/features/order.cs"));
    }

    [Fact]
    public void SliceOf_uses_the_first_definition_that_captures_a_name()
    {
        var definitions = new[]
        {
            ByPattern("src/features/(**)/*.cs"),
            ByPattern("src/(**)/*.cs"),
        };

        Assert.Equal("billing", SlicesProjection.SliceOf(definitions, "src/features/billing/order.cs"));
        Assert.Equal("shared", SlicesProjection.SliceOf(definitions, "src/shared/Util.cs"));
    }

    [Fact]
    public void ByRegex_names_a_file_by_its_first_capture_group()
    {
        var definitions = new[] { ByRegex("src/features/([a-z]+)/.*\\.cs") };

        Assert.Equal("billing", SlicesProjection.SliceOf(definitions, "src/features/billing/order.cs"));
        Assert.Null(SlicesProjection.SliceOf(definitions, "src/legacy/Old.cs"));
    }

    [Fact]
    public void SlicedFiles_returns_every_sliced_file_sorted()
    {
        var graph = Graph(
            Self("src/features/auth/login.cs"),
            Self("src/features/billing/order.cs"),
            Self("src/legacy/Old.cs"));

        IReadOnlyList<string> files = SlicesProjection.SlicedFiles(
            graph,
            new[] { ByPattern("src/features/(**)/*.cs") });

        Assert.Equal(
            new[] { "src/features/auth/login.cs", "src/features/billing/order.cs" },
            files);
    }

    [Fact]
    public void Slices_lists_the_distinct_slices_sorted()
    {
        var graph = Graph(
            Self("src/features/auth/login.cs"),
            Self("src/features/billing/order.cs"),
            Self("src/features/billing/invoice.cs"));

        IReadOnlyList<string> slices = SlicesProjection.Slices(
            graph,
            new[] { ByPattern("src/features/(**)/*.cs") });

        Assert.Equal(new[] { "auth", "billing" }, slices);
    }

    [Fact]
    public void FilesOf_returns_sliced_files_matching_a_filter()
    {
        var graph = Graph(
            Self("src/features/auth/login.cs"),
            Self("src/features/billing/order.cs"),
            Self("src/features/billing/invoice.cs"));

        IReadOnlyList<string> files = SlicesProjection.FilesOf(
            graph,
            new[] { ByPattern("src/features/(**)/*.cs") },
            Filter("src/features/billing/**"));

        Assert.Equal(
            new[] { "src/features/billing/invoice.cs", "src/features/billing/order.cs" },
            files);
    }

    [Fact]
    public void MatchingFiles_returns_every_file_matching_a_filter_sliced_or_not()
    {
        var graph = Graph(
            Self("src/features/billing/order.cs"),
            Self("src/legacy/Old.cs"));

        IReadOnlyList<string> files = SlicesProjection.MatchingFiles(graph, Filter("src/legacy/**"));

        Assert.Equal(new[] { "src/legacy/Old.cs" }, files);
    }

    [Fact]
    public void Dependencies_counts_an_edge_from_a_from_file_to_a_to_file()
    {
        var graph = Graph(
            Self("src/features/billing/order.cs"),
            Self("src/features/auth/login.cs"),
            Self("src/legacy/Old.cs"),
            Using("src/features/billing/order.cs", "src/legacy/Old.cs"),
            Using("src/features/auth/login.cs", "src/features/billing/order.cs"));

        IReadOnlyList<SliceDependency> dependencies = SlicesProjection.Dependencies(
            graph,
            new[] { ByPattern("src/features/(**)/*.cs") },
            Filter("src/features/**"),
            Filter("src/legacy/**"));

        Assert.Equal(
            new[]
            {
                new SliceDependency("billing", "src/features/billing/order.cs", "src/legacy/Old.cs"),
            },
            dependencies);
    }

    [Fact]
    public void Dependencies_counts_a_target_outside_the_slices()
    {
        var graph = Graph(
            Self("src/features/billing/order.cs"),
            Self("src/features/auth/login.cs"),
            Self("src/shared/Util.cs"),
            Using("src/features/billing/order.cs", "src/shared/Util.cs"));

        IReadOnlyList<SliceDependency> dependencies = SlicesProjection.Dependencies(
            graph,
            new[] { ByPattern("src/features/(**)/*.cs") },
            Filter("src/features/**"),
            Filter("src/shared/**"));

        Assert.Equal(
            new[]
            {
                new SliceDependency("billing", "src/features/billing/order.cs", "src/shared/Util.cs"),
            },
            dependencies);
    }

    [Fact]
    public void Dependencies_ignores_an_edge_from_an_unsliced_file()
    {
        var graph = Graph(
            Self("src/legacy/Old.cs"),
            Self("src/shared/Util.cs"),
            Using("src/legacy/Old.cs", "src/shared/Util.cs"));

        IReadOnlyList<SliceDependency> dependencies = SlicesProjection.Dependencies(
            graph,
            new[] { ByPattern("src/features/(**)/*.cs") },
            Filter("src/legacy/**"),
            Filter("src/shared/**"));

        Assert.Empty(dependencies);
    }

    [Fact]
    public void Dependencies_ignores_self_edges()
    {
        var graph = Graph(
            Self("src/features/billing/order.cs"),
            Self("src/features/auth/login.cs"));

        IReadOnlyList<SliceDependency> dependencies = SlicesProjection.Dependencies(
            graph,
            new[] { ByPattern("src/features/(**)/*.cs") },
            Filter("src/features/**"),
            Filter("src/features/**"));

        Assert.Empty(dependencies);
    }

    [Fact]
    public void Dependencies_ignores_external_edges()
    {
        var graph = Graph(
            Self("src/features/billing/order.cs"),
            Self("src/features/auth/login.cs"),
            External("src/features/billing/order.cs", "System.Linq"));

        IReadOnlyList<SliceDependency> dependencies = SlicesProjection.Dependencies(
            graph,
            new[] { ByPattern("src/features/(**)/*.cs") },
            Filter("src/features/**"),
            Filter("System.*"));

        Assert.Empty(dependencies);
    }

    [Fact]
    public void Dependencies_ignores_an_edge_whose_target_does_not_match_to()
    {
        var graph = Graph(
            Self("src/features/billing/order.cs"),
            Self("src/features/auth/login.cs"),
            Using("src/features/billing/order.cs", "src/features/auth/login.cs"));

        IReadOnlyList<SliceDependency> dependencies = SlicesProjection.Dependencies(
            graph,
            new[] { ByPattern("src/features/(**)/*.cs") },
            Filter("src/features/**"),
            Filter("src/legacy/**"));

        Assert.Empty(dependencies);
    }

    [Fact]
    public void Dependencies_result_is_sorted()
    {
        var graph = Graph(
            Self("B/b.cs"),
            Self("A/a.cs"),
            Self("A/c.cs"),
            Using("A/a.cs", "X/x.cs"),
            Using("B/b.cs", "X/x.cs"),
            Using("A/c.cs", "X/x.cs"));

        IReadOnlyList<SliceDependency> dependencies = SlicesProjection.Dependencies(
            graph,
            new[] { ByRegex("([^/]+)/[^/]+\\.cs") },
            Filter("**"),
            Filter("**"));

        Assert.Equal(
            new[]
            {
                new SliceDependency("A", "A/a.cs", "X/x.cs"),
                new SliceDependency("A", "A/c.cs", "X/x.cs"),
                new SliceDependency("B", "B/b.cs", "X/x.cs"),
            },
            dependencies);
    }

    [Fact]
    public void FileSuffix_names_a_file_by_its_extension()
    {
        Assert.Equal(".cs", SlicesProjection.FileSuffix("src/Models/Car.cs"));
        Assert.Equal(".css", SlicesProjection.FileSuffix("src/wwwroot/site.css"));
    }

    [Fact]
    public void FileSuffix_returns_nothing_for_a_file_without_an_extension()
    {
        Assert.Null(SlicesProjection.FileSuffix("src/README"));
    }

    [Fact]
    public void ByPattern_projects_the_graph_to_slices()
    {
        var graph = Graph(
            Self("src/features/billing/order.cs"),
            Self("src/features/auth/login.cs"),
            Using("src/features/billing/order.cs", "src/features/auth/login.cs"));

        IReadOnlyList<ProjectedEdge> edges =
            ArchUnitSharp.Projection.Projection.Edges(graph, Slice.ByPattern("src/features/(**)/*.cs"));

        Assert.Equal(
            new[] { ("billing", "auth") },
            edges.Select(static edge => (edge.Source, edge.Target)));
    }

    [Fact]
    public void ByPattern_projects_the_nodes_to_slices()
    {
        var graph = Graph(
            Self("src/features/billing/order.cs"),
            Self("src/features/auth/login.cs"));

        IReadOnlyList<ProjectedNode> nodes =
            ArchUnitSharp.Projection.Projection.ToNodes(graph, Slice.ByPattern("src/features/(**)/*.cs"));

        Assert.Equal(new[] { "auth", "billing" }, nodes.Select(static node => node.Label));
    }

    [Fact]
    public void ByPattern_drops_an_edge_whose_endpoint_is_unsliced()
    {
        var graph = Graph(
            Self("src/features/billing/order.cs"),
            Self("src/shared/Util.cs"),
            Using("src/features/billing/order.cs", "src/shared/Util.cs"));

        IReadOnlyList<ProjectedEdge> edges =
            ArchUnitSharp.Projection.Projection.Edges(graph, Slice.ByPattern("src/features/(**)/*.cs"));

        Assert.Empty(edges);
    }

    [Fact]
    public void ByRegex_projects_the_graph_to_slices()
    {
        var graph = Graph(
            Self("src/features/billing/order.cs"),
            Self("src/features/auth/login.cs"),
            Using("src/features/billing/order.cs", "src/features/auth/login.cs"));

        IReadOnlyList<ProjectedEdge> edges =
            ArchUnitSharp.Projection.Projection.Edges(graph, Slice.ByRegex("src/features/([a-z]+)/.*\\.cs"));

        Assert.Equal(
            new[] { ("billing", "auth") },
            edges.Select(static edge => (edge.Source, edge.Target)));
    }

    [Fact]
    public void ByFileSuffix_projects_the_nodes_to_extensions()
    {
        var graph = Graph(
            Self("src/site.css"),
            Self("src/Models/Car.cs"),
            Self("src/App/Program.cs"));

        IReadOnlyList<ProjectedNode> nodes = ArchUnitSharp.Projection.Projection.ToNodes(graph, Slice.ByFileSuffix());

        Assert.Equal(new[] { ".cs", ".css" }, nodes.Select(static node => node.Label));
    }

    [Fact]
    public void ByFileSuffix_drops_a_file_without_an_extension()
    {
        var graph = Graph(
            Self("src/Models/Car.cs"),
            Self("README"));

        IReadOnlyList<ProjectedNode> nodes = ArchUnitSharp.Projection.Projection.ToNodes(graph, Slice.ByFileSuffix());

        Assert.Equal(new[] { ".cs" }, nodes.Select(static node => node.Label));
    }

    [Fact]
    public void ByFileSuffix_drops_external_edges()
    {
        var graph = Graph(
            Self("src/Models/Car.cs"),
            External("src/Models/Car.cs", "System.Linq"));

        IReadOnlyList<ProjectedEdge> edges =
            ArchUnitSharp.Projection.Projection.Edges(graph, Slice.ByFileSuffix());

        Assert.Empty(edges);
    }

    [Fact]
    public void Identity_keeps_every_file_as_its_own_slice()
    {
        var graph = Graph(Self("a.cs"), Self("b.cs"));

        IReadOnlyList<ProjectedNode> nodes = ArchUnitSharp.Projection.Projection.ToNodes(graph, Slice.Identity);

        Assert.Equal(new[] { "a.cs", "b.cs" }, nodes.Select(static node => node.Label));
    }

    [Fact]
    public void Identity_is_the_projection_layers_own_identity_map()
    {
        Assert.Same(MapFunctions.Identity, Slice.Identity);
    }

    [Fact]
    public void ByPattern_rejects_a_glob_without_a_capture()
    {
        Assert.Throws<UserError>(() => Slice.ByPattern("src/features/**"));
    }

    [Fact]
    public void ByRegex_rejects_a_pattern_without_a_capture()
    {
        Assert.Throws<UserError>(() => Slice.ByRegex("src/features/.*"));
    }

    [Fact]
    public void SliceOf_rejects_null_definitions()
    {
        Assert.Throws<ArgumentNullException>(() => SlicesProjection.SliceOf(null!, "a.cs"));
    }

    [Fact]
    public void SliceOf_rejects_a_null_identifier()
    {
        Assert.Throws<ArgumentNullException>(() =>
            SlicesProjection.SliceOf(new[] { ByPattern("src/(**)/*.cs") }, null!));
    }

    [Fact]
    public void Dependencies_rejects_a_null_graph()
    {
        Assert.Throws<ArgumentNullException>(() =>
            SlicesProjection.Dependencies(
                null!,
                new[] { ByPattern("src/(**)/*.cs") },
                Filter("**"),
                Filter("**")));
    }

    [Fact]
    public void Dependencies_rejects_null_definitions()
    {
        Assert.Throws<ArgumentNullException>(() =>
            SlicesProjection.Dependencies(Graph(Self("a.cs")), null!, Filter("**"), Filter("**")));
    }

    [Fact]
    public void Dependencies_rejects_a_null_from_filter()
    {
        Assert.Throws<ArgumentNullException>(() =>
            SlicesProjection.Dependencies(
                Graph(Self("a.cs")),
                new[] { ByPattern("src/(**)/*.cs") },
                null!,
                Filter("**")));
    }

    [Fact]
    public void Dependencies_rejects_a_null_to_filter()
    {
        Assert.Throws<ArgumentNullException>(() =>
            SlicesProjection.Dependencies(
                Graph(Self("a.cs")),
                new[] { ByPattern("src/(**)/*.cs") },
                Filter("**"),
                null!));
    }

    [Fact]
    public void FileSuffix_rejects_a_null_identifier()
    {
        Assert.Throws<ArgumentNullException>(() => SlicesProjection.FileSuffix(null!));
    }

    private static Graph Graph(params Edge[] edges) => new(edges);

    private static Edge Self(string file) => new(file, file, external: false, ImportKind.None);

    private static Edge Using(string source, string target) =>
        new(source, target, external: false, ImportKind.Using);

    private static Edge External(string source, string module) =>
        new(source, module, external: true, ImportKind.Using);

    private static SliceDefinition ByPattern(string glob) => SliceDefinition.ByPattern(glob);

    private static SliceDefinition ByRegex(string pattern) => SliceDefinition.ByRegex(pattern);

    private static Filter Filter(string glob) => new(new Pattern(glob), MatchTarget.Path);
}
