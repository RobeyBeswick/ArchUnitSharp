using ArchUnitSharp.Common.Extraction;
using ArchUnitSharp.Files.Projection;

namespace ArchUnitSharp.Files.Tests;

public class FilesProjectionTests
{
    [Fact]
    public void Select_returns_every_file_when_there_are_no_filters()
    {
        var graph = Graph(
            Self("src/App/Program.cs"),
            Self("src/Models/Car.cs"));

        IReadOnlyList<string> files = FilesProjection.Select(graph, Array.Empty<Filter>());

        Assert.Equal(new[] { "src/App/Program.cs", "src/Models/Car.cs" }, files);
    }

    [Fact]
    public void Select_returns_only_distinct_sources_so_external_targets_are_not_files()
    {
        var graph = Graph(
            Self("src/App/Program.cs"),
            Using("src/App/Program.cs", "System"));

        IReadOnlyList<string> files = FilesProjection.Select(graph, Array.Empty<Filter>());

        Assert.Equal(new[] { "src/App/Program.cs" }, files);
    }

    [Fact]
    public void Select_matches_by_filename()
    {
        var graph = Graph(
            Self("src/Models/Car.cs"),
            Self("src/App/Program.cs"));

        IReadOnlyList<string> files = FilesProjection.Select(graph, new[] { Filename("Car.cs") });

        Assert.Equal(new[] { "src/Models/Car.cs" }, files);
    }

    [Fact]
    public void Select_matches_by_folder()
    {
        var graph = Graph(
            Self("src/Models/Car.cs"),
            Self("src/App/Program.cs"));

        IReadOnlyList<string> files = FilesProjection.Select(graph, new[] { Folder("src/Models") });

        Assert.Equal(new[] { "src/Models/Car.cs" }, files);
    }

    [Fact]
    public void Select_matches_by_path()
    {
        var graph = Graph(
            Self("src/Models/Car.cs"),
            Self("src/Models/Truck.cs"));

        IReadOnlyList<string> files = FilesProjection.Select(graph, new[] { Path("src/Models/Car.cs") });

        Assert.Equal(new[] { "src/Models/Car.cs" }, files);
    }

    [Fact]
    public void Select_matches_by_classname()
    {
        var graph = Graph(
            Self("src/Models/Car.cs"),
            Self("src/App/Program.cs"));

        IReadOnlyList<string> files = FilesProjection.Select(graph, new[] { File("src.Models.Car") });

        Assert.Equal(new[] { "src/Models/Car.cs" }, files);
    }

    [Fact]
    public void Select_combines_filters_with_and()
    {
        var graph = Graph(
            Self("src/Models/Car.cs"),
            Self("src/Models/Truck.cs"),
            Self("src/App/Car.cs"));

        IReadOnlyList<string> files = FilesProjection.Select(
            graph,
            new[] { Filename("*.cs"), Folder("src/Models") });

        Assert.Equal(new[] { "src/Models/Car.cs", "src/Models/Truck.cs" }, files);
    }

    [Fact]
    public void Select_drops_a_file_that_fails_any_filter()
    {
        var graph = Graph(
            Self("src/Models/Car.cs"),
            Self("src/App/Program.cs"));

        IReadOnlyList<string> files = FilesProjection.Select(
            graph,
            new[] { Filename("Car.cs"), Folder("src/App") });

        Assert.Empty(files);
    }

    [Fact]
    public void Select_result_is_sorted_ordinally()
    {
        var graph = Graph(
            Self("Z/z.cs"),
            Self("A/a.cs"),
            Self("M/m.cs"));

        IReadOnlyList<string> files = FilesProjection.Select(graph, Array.Empty<Filter>());

        Assert.Equal(new[] { "A/a.cs", "M/m.cs", "Z/z.cs" }, files);
    }

    [Fact]
    public void Select_of_an_empty_graph_yields_no_files()
    {
        IReadOnlyList<string> files = FilesProjection.Select(Graph(), Array.Empty<Filter>());

        Assert.Empty(files);
    }

    [Fact]
    public void Select_rejects_a_null_graph()
    {
        Assert.Throws<ArgumentNullException>(() => FilesProjection.Select(null!, Array.Empty<Filter>()));
    }

    [Fact]
    public void Select_rejects_null_filters()
    {
        Assert.Throws<ArgumentNullException>(() => FilesProjection.Select(Graph(Self("a.cs")), null!));
    }

    [Fact]
    public void Cycles_rejects_a_null_graph()
    {
        Assert.Throws<ArgumentNullException>(() => FilesProjection.Cycles(null!, Array.Empty<Filter>()));
    }

    [Fact]
    public void Cycles_rejects_null_filters()
    {
        Assert.Throws<ArgumentNullException>(() => FilesProjection.Cycles(Graph(Using("a.cs", "b.cs")), null!));
    }

    [Fact]
    public void Cycles_reports_each_cycle_as_the_closed_file_path()
    {
        var graph = Graph(
            Using("A/a.cs", "B/b.cs"),
            Using("B/b.cs", "C/c.cs"),
            Using("C/c.cs", "A/a.cs"));

        IReadOnlyList<IReadOnlyList<string>> cycles = FilesProjection.Cycles(graph, Array.Empty<Filter>());

        var cycle = Assert.Single(cycles);
        Assert.Equal(new[] { "A/a.cs", "B/b.cs", "C/c.cs", "A/a.cs" }, cycle);
        Assert.Equal(cycle[0], cycle[^1]);
    }

    [Fact]
    public void Cycles_returns_nothing_for_an_acyclic_graph()
    {
        var graph = Graph(
            Using("A/a.cs", "B/b.cs"),
            Using("B/b.cs", "C/c.cs"));

        Assert.Empty(FilesProjection.Cycles(graph, Array.Empty<Filter>()));
    }

    [Fact]
    public void Cycles_reports_a_cycle_whose_files_are_all_selected()
    {
        var graph = Graph(
            Using("src/A.cs", "src/B.cs"),
            Using("src/B.cs", "src/A.cs"));

        IReadOnlyList<IReadOnlyList<string>> cycles = FilesProjection.Cycles(
            graph,
            new[] { Folder("src") });

        var cycle = Assert.Single(cycles);
        Assert.Equal(new[] { "src/A.cs", "src/B.cs", "src/A.cs" }, cycle);
    }

    [Fact]
    public void Cycles_does_not_report_a_cycle_that_leaves_the_selection()
    {
        var graph = Graph(
            Using("A/a.cs", "B/b.cs"),
            Using("B/b.cs", "A/a.cs"));

        IReadOnlyList<IReadOnlyList<string>> cycles = FilesProjection.Cycles(
            graph,
            new[] { Filename("A.cs") });

        Assert.Empty(cycles);
    }

    [Fact]
    public void Cycles_ignores_self_edges()
    {
        var graph = Graph(
            Self("A/a.cs"),
            Self("B/b.cs"),
            Using("A/a.cs", "B/b.cs"),
            Using("B/b.cs", "A/a.cs"));

        var cycle = Assert.Single(FilesProjection.Cycles(graph, Array.Empty<Filter>()));
        Assert.Equal(new[] { "A/a.cs", "B/b.cs", "A/a.cs" }, cycle);
    }

    [Fact]
    public void Cycles_reports_disjoint_cycles_in_the_cycle_projections_order()
    {
        var graph = Graph(
            Using("A/a.cs", "B/b.cs"),
            Using("B/b.cs", "A/a.cs"),
            Using("C/c.cs", "D/d.cs"),
            Using("D/d.cs", "C/c.cs"));

        IReadOnlyList<IReadOnlyList<string>> cycles = FilesProjection.Cycles(graph, Array.Empty<Filter>());

        Assert.Equal(2, cycles.Count);
        Assert.Equal(new[] { "A/a.cs", "B/b.cs", "A/a.cs" }, cycles[0]);
        Assert.Equal(new[] { "C/c.cs", "D/d.cs", "C/c.cs" }, cycles[1]);
    }

    private static Graph Graph(params Edge[] edges) => new(edges);

    private static Edge Self(string file) => new(file, file, external: false, ImportKind.None);

    private static Edge Using(string source, string target) =>
        new(source, target, external: false, ImportKind.Using);

    private static Filter Filename(string glob) => new(new Pattern(glob), MatchTarget.Filename);

    private static Filter Folder(string glob) => new(new Pattern(glob), MatchTarget.PathWithoutFilename);

    private static Filter Path(string glob) => new(new Pattern(glob), MatchTarget.Path);

    private static Filter File(string glob) => new(new Pattern(glob), MatchTarget.Classname);
}
