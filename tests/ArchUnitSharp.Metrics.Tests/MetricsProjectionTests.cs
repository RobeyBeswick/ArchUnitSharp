using ArchUnitSharp.Common.Extraction;
using ArchUnitSharp.Metrics.Projection;

namespace ArchUnitSharp.Metrics.Tests;

public class MetricsProjectionTests
{
    [Fact]
    public void SelectFiles_returns_every_file_when_there_are_no_filters()
    {
        var graph = Graph(
            Self("src/App/Program.cs"),
            Self("src/Models/Car.cs"));

        IReadOnlyList<string> files = MetricsProjection.SelectFiles(graph, Array.Empty<Filter>());

        Assert.Equal(new[] { "src/App/Program.cs", "src/Models/Car.cs" }, files);
    }

    [Fact]
    public void SelectFiles_returns_only_distinct_sources_so_external_targets_are_not_files()
    {
        var graph = Graph(
            Self("src/App/Program.cs"),
            Using("src/App/Program.cs", "System"));

        IReadOnlyList<string> files = MetricsProjection.SelectFiles(graph, Array.Empty<Filter>());

        Assert.Equal(new[] { "src/App/Program.cs" }, files);
    }

    [Fact]
    public void SelectFiles_matches_by_filename()
    {
        var graph = Graph(
            Self("src/Models/Car.cs"),
            Self("src/App/Program.cs"));

        IReadOnlyList<string> files = MetricsProjection.SelectFiles(graph, new[] { Filename("Car.cs") });

        Assert.Equal(new[] { "src/Models/Car.cs" }, files);
    }

    [Fact]
    public void SelectFiles_matches_by_folder()
    {
        var graph = Graph(
            Self("src/Models/Car.cs"),
            Self("src/App/Program.cs"));

        IReadOnlyList<string> files = MetricsProjection.SelectFiles(graph, new[] { Folder("src/Models") });

        Assert.Equal(new[] { "src/Models/Car.cs" }, files);
    }

    [Fact]
    public void SelectFiles_matches_by_path()
    {
        var graph = Graph(
            Self("src/Models/Car.cs"),
            Self("src/Models/Truck.cs"));

        IReadOnlyList<string> files = MetricsProjection.SelectFiles(graph, new[] { Path("src/Models/Car.cs") });

        Assert.Equal(new[] { "src/Models/Car.cs" }, files);
    }

    [Fact]
    public void SelectFiles_combines_filters_with_and()
    {
        var graph = Graph(
            Self("src/Models/Car.cs"),
            Self("src/Models/Truck.cs"),
            Self("src/App/Car.cs"));

        IReadOnlyList<string> files = MetricsProjection.SelectFiles(
            graph,
            new[] { Filename("*.cs"), Folder("src/Models") });

        Assert.Equal(new[] { "src/Models/Car.cs", "src/Models/Truck.cs" }, files);
    }

    [Fact]
    public void SelectFiles_result_is_sorted_ordinally()
    {
        var graph = Graph(
            Self("Z/z.cs"),
            Self("A/a.cs"),
            Self("M/m.cs"));

        IReadOnlyList<string> files = MetricsProjection.SelectFiles(graph, Array.Empty<Filter>());

        Assert.Equal(new[] { "A/a.cs", "M/m.cs", "Z/z.cs" }, files);
    }

    [Fact]
    public void SelectFiles_of_an_empty_graph_yields_no_files()
    {
        IReadOnlyList<string> files = MetricsProjection.SelectFiles(Graph(), Array.Empty<Filter>());

        Assert.Empty(files);
    }

    [Fact]
    public void SelectFiles_rejects_a_null_graph()
    {
        Assert.Throws<ArgumentNullException>(() => MetricsProjection.SelectFiles(null!, Array.Empty<Filter>()));
    }

    [Fact]
    public void SelectFiles_rejects_null_filters()
    {
        Assert.Throws<ArgumentNullException>(() => MetricsProjection.SelectFiles(Graph(Self("a.cs")), null!));
    }

    [Fact]
    public void SelectFileSubjects_returns_every_file_when_there_are_no_class_filters()
    {
        var files = new[]
        {
            File("src/A.cs", "App.A", "App.Nested"),
            File("src/B.cs"),
        };

        IReadOnlyList<FileInfo> selected = MetricsProjection.SelectFileSubjects(files, Array.Empty<Filter>());

        Assert.Equal(new[] { "src/A.cs", "src/B.cs" }, selected.Select(static file => file.Path));
    }

    [Fact]
    public void SelectFileSubjects_keeps_files_that_contain_a_matching_class()
    {
        var files = new[]
        {
            File("src/A.cs", "App.Controllers.CarController"),
            File("src/B.cs", "App.Models.Car"),
        };

        IReadOnlyList<FileInfo> selected = MetricsProjection.SelectFileSubjects(
            files,
            new[] { ClassFilter("*Controller") });

        Assert.Equal(new[] { "src/A.cs" }, selected.Select(static file => file.Path));
    }

    [Fact]
    public void SelectFileSubjects_drops_a_file_with_no_matching_class()
    {
        var files = new[]
        {
            File("src/A.cs", "App.Models.Car"),
            File("src/B.cs"),
        };

        IReadOnlyList<FileInfo> selected = MetricsProjection.SelectFileSubjects(
            files,
            new[] { ClassFilter("*Controller") });

        Assert.Empty(selected);
    }

    [Fact]
    public void SelectFileSubjects_combines_class_filters_with_and()
    {
        var files = new[]
        {
            File("src/A.cs", "App.Controllers.CarController"),
            File("src/B.cs", "Other.Controllers.CarController"),
        };

        IReadOnlyList<FileInfo> selected = MetricsProjection.SelectFileSubjects(
            files,
            new[] { ClassFilter("App.*"), ClassFilter("*Controller") });

        Assert.Equal(new[] { "src/A.cs" }, selected.Select(static file => file.Path));
    }

    [Fact]
    public void SelectFileSubjects_result_is_sorted_by_path()
    {
        var files = new[]
        {
            File("src/Z.cs", "App.C"),
            File("src/A.cs", "App.B"),
        };

        IReadOnlyList<FileInfo> selected = MetricsProjection.SelectFileSubjects(
            files,
            new[] { ClassFilter("**") });

        Assert.Equal(new[] { "src/A.cs", "src/Z.cs" }, selected.Select(static file => file.Path));
    }

    [Fact]
    public void SelectFileSubjects_rejects_null_files()
    {
        Assert.Throws<ArgumentNullException>(() => MetricsProjection.SelectFileSubjects(null!, Array.Empty<Filter>()));
    }

    [Fact]
    public void SelectFileSubjects_rejects_null_class_filters()
    {
        Assert.Throws<ArgumentNullException>(() => MetricsProjection.SelectFileSubjects(new[] { File("a.cs") }, null!));
    }

    [Fact]
    public void SelectClasses_returns_every_class_when_there_are_no_class_filters()
    {
        var files = new[]
        {
            File("src/A.cs", "App.A", "App.B"),
            File("src/C.cs", "App.C"),
        };

        IReadOnlyList<ClassInfo> classes = MetricsProjection.SelectClasses(files, Array.Empty<Filter>());

        Assert.Equal(
            new[] { "src/A.cs:App.A", "src/A.cs:App.B", "src/C.cs:App.C" },
            classes.Select(static info => info.Identifier));
    }

    [Fact]
    public void SelectClasses_keeps_the_classes_whose_name_matches()
    {
        var files = new[]
        {
            File("src/A.cs", "App.Controllers.CarController", "App.Models.Car"),
            File("src/B.cs", "Other.Controllers.CarController"),
        };

        IReadOnlyList<ClassInfo> classes = MetricsProjection.SelectClasses(
            files,
            new[] { ClassFilter("App.*") });

        Assert.Equal(
            new[] { "src/A.cs:App.Controllers.CarController", "src/A.cs:App.Models.Car" },
            classes.Select(static info => info.Identifier));
    }

    [Fact]
    public void SelectClasses_combines_filters_with_and()
    {
        var files = new[]
        {
            File("src/A.cs", "App.Controllers.CarController", "App.Models.Car"),
        };

        IReadOnlyList<ClassInfo> classes = MetricsProjection.SelectClasses(
            files,
            new[] { ClassFilter("App.*"), ClassFilter("*.Car") });

        Assert.Equal(
            new[] { "src/A.cs:App.Models.Car" },
            classes.Select(static info => info.Identifier));
    }

    [Fact]
    public void SelectClasses_result_is_sorted_by_identifier()
    {
        var files = new[]
        {
            File("src/C.cs", "App.Mike"),
            File("src/B.cs", "App.Alpha"),
            File("src/A.cs", "App.Zeta"),
        };

        IReadOnlyList<ClassInfo> classes = MetricsProjection.SelectClasses(files, Array.Empty<Filter>());

        Assert.Equal(
            new[] { "src/A.cs:App.Zeta", "src/B.cs:App.Alpha", "src/C.cs:App.Mike" },
            classes.Select(static info => info.Identifier));
    }

    [Fact]
    public void SelectClasses_rejects_null_files()
    {
        Assert.Throws<ArgumentNullException>(() => MetricsProjection.SelectClasses(null!, Array.Empty<Filter>()));
    }

    [Fact]
    public void SelectClasses_rejects_null_class_filters()
    {
        Assert.Throws<ArgumentNullException>(() => MetricsProjection.SelectClasses(new[] { File("a.cs") }, null!));
    }

    private static FileInfo File(string path, params string[] classNames)
    {
        ClassInfo[] classInfos = classNames
            .Select(name => new ClassInfo(name, path, Array.Empty<MethodInfo>(), Array.Empty<FieldInfo>()))
            .ToArray();
        return new FileInfo(path, 0, 0, 0, classInfos.Length, 0, classInfos);
    }

    private static Graph Graph(params Edge[] edges) => new(edges);

    private static Edge Self(string file) => new(file, file, external: false, ImportKind.None);

    private static Edge Using(string source, string target) =>
        new(source, target, external: false, ImportKind.Using);

    private static Filter Filename(string glob) => new(new Pattern(glob), MatchTarget.Filename);

    private static Filter Folder(string glob) => new(new Pattern(glob), MatchTarget.PathWithoutFilename);

    private static Filter Path(string glob) => new(new Pattern(glob), MatchTarget.Path);

    private static Filter ClassFilter(string glob) => new(new Pattern(glob), MatchTarget.Path);
}
