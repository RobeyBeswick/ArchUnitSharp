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

    private static Graph Graph(params Edge[] edges) => new(edges);

    private static Edge Self(string file) => new(file, file, external: false, ImportKind.None);

    private static Edge Using(string source, string target) =>
        new(source, target, external: false, ImportKind.Using);

    private static Filter Filename(string glob) => new(new Pattern(glob), MatchTarget.Filename);

    private static Filter Folder(string glob) => new(new Pattern(glob), MatchTarget.PathWithoutFilename);

    private static Filter Path(string glob) => new(new Pattern(glob), MatchTarget.Path);

    private static Filter File(string glob) => new(new Pattern(glob), MatchTarget.Classname);
}
