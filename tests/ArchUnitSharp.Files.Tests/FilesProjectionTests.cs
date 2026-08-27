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

    [Fact]
    public void Dependencies_returns_every_edge_from_a_subject_file_to_an_object_file()
    {
        var graph = Graph(
            Self("src/Models/Car.cs"),
            Self("src/Models/Truck.cs"),
            Using("src/App/Program.cs", "src/Models/Car.cs"),
            Using("src/App/Program.cs", "src/Models/Truck.cs"),
            Using("src/App/Program.cs", "src/Util/Helper.cs"));

        IReadOnlyList<Edge> dependencies = FilesProjection.Dependencies(
            graph,
            new[] { Folder("src/App") },
            new[] { Folder("src/Models") });

        Assert.Equal(
            new[]
            {
                new Edge("src/App/Program.cs", "src/Models/Car.cs", external: false, ImportKind.Using),
                new Edge("src/App/Program.cs", "src/Models/Truck.cs", external: false, ImportKind.Using),
            },
            dependencies);
    }

    [Fact]
    public void Dependencies_ignores_edges_from_unselected_files()
    {
        var graph = Graph(
            Self("src/Models/Car.cs"),
            Using("src/App/Program.cs", "src/Models/Car.cs"),
            Using("src/Other/Other.cs", "src/Models/Car.cs"));

        IReadOnlyList<Edge> dependencies = FilesProjection.Dependencies(
            graph,
            new[] { Folder("src/App") },
            new[] { Folder("src/Models") });

        Assert.Equal(
            new[] { new Edge("src/App/Program.cs", "src/Models/Car.cs", external: false, ImportKind.Using) },
            dependencies);
    }

    [Fact]
    public void Dependencies_ignores_edges_to_unselected_files()
    {
        var graph = Graph(
            Self("src/Models/Car.cs"),
            Using("src/App/Program.cs", "src/Models/Car.cs"),
            Using("src/App/Program.cs", "src/Util/Helper.cs"));

        IReadOnlyList<Edge> dependencies = FilesProjection.Dependencies(
            graph,
            new[] { Folder("src/App") },
            new[] { Folder("src/Models") });

        Assert.Equal(
            new[] { new Edge("src/App/Program.cs", "src/Models/Car.cs", external: false, ImportKind.Using) },
            dependencies);
    }

    [Fact]
    public void Dependencies_combines_subject_filters_with_and()
    {
        var graph = Graph(
            Self("src/Models/Car.cs"),
            Using("src/App/Program.cs", "src/Models/Car.cs"),
            Using("src/App/Truck.cs", "src/Models/Car.cs"));

        IReadOnlyList<Edge> dependencies = FilesProjection.Dependencies(
            graph,
            new[] { Folder("src/App"), Filename("Program.cs") },
            new[] { Folder("src/Models") });

        Assert.Equal(
            new[] { new Edge("src/App/Program.cs", "src/Models/Car.cs", external: false, ImportKind.Using) },
            dependencies);
    }

    [Fact]
    public void Dependencies_combines_object_filters_with_and()
    {
        var graph = Graph(
            Self("src/Models/Car.cs"),
            Using("src/App/Program.cs", "src/Models/Car.cs"),
            Using("src/App/Program.cs", "src/Models/Truck.cs"));

        IReadOnlyList<Edge> dependencies = FilesProjection.Dependencies(
            graph,
            new[] { Folder("src/App") },
            new[] { Folder("src/Models"), Filename("Car.cs") });

        Assert.Equal(
            new[] { new Edge("src/App/Program.cs", "src/Models/Car.cs", external: false, ImportKind.Using) },
            dependencies);
    }

    [Fact]
    public void Dependencies_ignores_self_edges()
    {
        var graph = Graph(
            Self("src/App/Program.cs"),
            Self("src/Models/Car.cs"),
            Using("src/App/Program.cs", "src/Models/Car.cs"));

        IReadOnlyList<Edge> dependencies = FilesProjection.Dependencies(
            graph,
            Array.Empty<Filter>(),
            Array.Empty<Filter>());

        Assert.Equal(
            new[] { new Edge("src/App/Program.cs", "src/Models/Car.cs", external: false, ImportKind.Using) },
            dependencies);
    }

    [Fact]
    public void Dependencies_ignores_external_edges()
    {
        var graph = Graph(
            Self("src/App/Program.cs"),
            Self("src/Models/Car.cs"),
            new Edge("src/App/Program.cs", "src/Models/Car.cs", external: true, ImportKind.Using));

        IReadOnlyList<Edge> dependencies = FilesProjection.Dependencies(
            graph,
            Array.Empty<Filter>(),
            Array.Empty<Filter>());

        Assert.Empty(dependencies);
    }

    [Fact]
    public void Dependencies_result_is_sorted_by_source_then_target()
    {
        var graph = Graph(
            Self("M/m.cs"),
            Self("B/b.cs"),
            Using("Z/z.cs", "A/a.cs"),
            Using("A/a.cs", "M/m.cs"),
            Using("A/a.cs", "B/b.cs"));

        IReadOnlyList<Edge> dependencies = FilesProjection.Dependencies(
            graph,
            Array.Empty<Filter>(),
            Array.Empty<Filter>());

        Assert.Equal(
            new[]
            {
                new Edge("A/a.cs", "B/b.cs", external: false, ImportKind.Using),
                new Edge("A/a.cs", "M/m.cs", external: false, ImportKind.Using),
                new Edge("Z/z.cs", "A/a.cs", external: false, ImportKind.Using),
            },
            dependencies);
    }

    [Fact]
    public void Dependencies_rejects_a_null_graph()
    {
        Assert.Throws<ArgumentNullException>(() =>
            FilesProjection.Dependencies(null!, Array.Empty<Filter>(), Array.Empty<Filter>()));
    }

    [Fact]
    public void Dependencies_rejects_null_subject_filters()
    {
        Assert.Throws<ArgumentNullException>(() =>
            FilesProjection.Dependencies(Graph(Using("a.cs", "b.cs")), null!, Array.Empty<Filter>()));
    }

    [Fact]
    public void Dependencies_rejects_null_object_filters()
    {
        Assert.Throws<ArgumentNullException>(() =>
            FilesProjection.Dependencies(Graph(Using("a.cs", "b.cs")), Array.Empty<Filter>(), null!));
    }

    [Fact]
    public void ExternalModules_returns_every_external_module_when_there_are_no_filters()
    {
        var graph = Graph(
            Self("src/App/Program.cs"),
            External("src/App/Program.cs", "Newtonsoft.Json"),
            External("src/App/Program.cs", "System.Linq"));

        IReadOnlyList<string> modules = FilesProjection.ExternalModules(graph, Array.Empty<Filter>());

        Assert.Equal(new[] { "Newtonsoft.Json", "System.Linq" }, modules);
    }

    [Fact]
    public void ExternalModules_returns_only_distinct_names()
    {
        var graph = Graph(
            External("src/App/Program.cs", "System.Linq"),
            External("src/App/Other.cs", "System.Linq"),
            External("src/App/Other.cs", "Newtonsoft.Json"));

        IReadOnlyList<string> modules = FilesProjection.ExternalModules(graph, Array.Empty<Filter>());

        Assert.Equal(new[] { "Newtonsoft.Json", "System.Linq" }, modules);
    }

    [Fact]
    public void ExternalModules_ignores_internal_targets()
    {
        var graph = Graph(
            Self("src/App/Program.cs"),
            Self("src/Models/Car.cs"),
            Using("src/App/Program.cs", "src/Models/Car.cs"));

        IReadOnlyList<string> modules = FilesProjection.ExternalModules(graph, Array.Empty<Filter>());

        Assert.Empty(modules);
    }

    [Fact]
    public void ExternalModules_matches_a_module_name_glob()
    {
        var graph = Graph(
            External("src/App/Program.cs", "System.Linq"),
            External("src/App/Program.cs", "System.Collections.Generic"),
            External("src/App/Program.cs", "Newtonsoft.Json"));

        IReadOnlyList<string> modules = FilesProjection.ExternalModules(graph, new[] { Module("System.*") });

        Assert.Equal(new[] { "System.Collections.Generic", "System.Linq" }, modules);
    }

    [Fact]
    public void ExternalModules_combines_filters_with_or()
    {
        var graph = Graph(
            External("src/App/Program.cs", "System.Linq"),
            External("src/App/Program.cs", "Newtonsoft.Json"),
            External("src/App/Program.cs", "NUnit"));

        IReadOnlyList<string> modules = FilesProjection.ExternalModules(
            graph,
            new[] { Module("System.*"), Module("Newtonsoft.*") });

        Assert.Equal(new[] { "Newtonsoft.Json", "System.Linq" }, modules);
    }

    [Fact]
    public void ExternalModules_result_is_sorted_ordinally()
    {
        var graph = Graph(
            External("src/App/Program.cs", "Zeta"),
            External("src/App/Program.cs", "Alpha"),
            External("src/App/Program.cs", "Mike"));

        IReadOnlyList<string> modules = FilesProjection.ExternalModules(graph, Array.Empty<Filter>());

        Assert.Equal(new[] { "Alpha", "Mike", "Zeta" }, modules);
    }

    [Fact]
    public void ExternalModules_rejects_a_null_graph()
    {
        Assert.Throws<ArgumentNullException>(() => FilesProjection.ExternalModules(null!, Array.Empty<Filter>()));
    }

    [Fact]
    public void ExternalModules_rejects_null_filters()
    {
        Assert.Throws<ArgumentNullException>(() => FilesProjection.ExternalModules(Graph(), null!));
    }

    [Fact]
    public void ExternalDependencies_returns_every_edge_from_a_subject_file_to_a_matching_module()
    {
        var graph = Graph(
            Self("src/App/Program.cs"),
            External("src/App/Program.cs", "System.Linq"),
            External("src/App/Program.cs", "System.Collections.Generic"),
            External("src/App/Program.cs", "Newtonsoft.Json"));

        IReadOnlyList<Edge> dependencies = FilesProjection.ExternalDependencies(
            graph,
            new[] { Folder("src/App") },
            new[] { Module("System.*") });

        Assert.Equal(
            new[]
            {
                new Edge("src/App/Program.cs", "System.Collections.Generic", external: true, ImportKind.Using),
                new Edge("src/App/Program.cs", "System.Linq", external: true, ImportKind.Using),
            },
            dependencies);
    }

    [Fact]
    public void ExternalDependencies_ignores_edges_from_unselected_files()
    {
        var graph = Graph(
            Self("src/App/Program.cs"),
            Self("src/Other/Other.cs"),
            External("src/App/Program.cs", "System.Linq"),
            External("src/Other/Other.cs", "System.Linq"));

        IReadOnlyList<Edge> dependencies = FilesProjection.ExternalDependencies(
            graph,
            new[] { Folder("src/App") },
            new[] { Module("System.*") });

        Assert.Equal(
            new[] { new Edge("src/App/Program.cs", "System.Linq", external: true, ImportKind.Using) },
            dependencies);
    }

    [Fact]
    public void ExternalDependencies_ignores_internal_edges()
    {
        var graph = Graph(
            Self("src/App/Program.cs"),
            Self("src/Models/Car.cs"),
            Using("src/App/Program.cs", "src/Models/Car.cs"));

        IReadOnlyList<Edge> dependencies = FilesProjection.ExternalDependencies(
            graph,
            Array.Empty<Filter>(),
            new[] { Module("**/*") });

        Assert.Empty(dependencies);
    }

    [Fact]
    public void ExternalDependencies_combines_object_filters_with_or()
    {
        var graph = Graph(
            Self("src/App/Program.cs"),
            External("src/App/Program.cs", "System.Linq"),
            External("src/App/Program.cs", "Newtonsoft.Json"));

        IReadOnlyList<Edge> dependencies = FilesProjection.ExternalDependencies(
            graph,
            new[] { Folder("src/App") },
            new[] { Module("System.*"), Module("Newtonsoft.*") });

        Assert.Equal(
            new[]
            {
                new Edge("src/App/Program.cs", "Newtonsoft.Json", external: true, ImportKind.Using),
                new Edge("src/App/Program.cs", "System.Linq", external: true, ImportKind.Using),
            },
            dependencies);
    }

    [Fact]
    public void ExternalDependencies_with_no_object_filters_returns_every_external_edge()
    {
        var graph = Graph(
            Self("src/App/Program.cs"),
            External("src/App/Program.cs", "System.Linq"),
            External("src/App/Program.cs", "Newtonsoft.Json"));

        IReadOnlyList<Edge> dependencies = FilesProjection.ExternalDependencies(
            graph,
            Array.Empty<Filter>(),
            Array.Empty<Filter>());

        Assert.Equal(
            new[]
            {
                new Edge("src/App/Program.cs", "Newtonsoft.Json", external: true, ImportKind.Using),
                new Edge("src/App/Program.cs", "System.Linq", external: true, ImportKind.Using),
            },
            dependencies);
    }

    [Fact]
    public void ExternalDependencies_result_is_sorted_by_source_then_target()
    {
        var graph = Graph(
            Self("Z/z.cs"),
            Self("A/a.cs"),
            External("Z/z.cs", "Beta"),
            External("A/a.cs", "Mike"),
            External("A/a.cs", "Alpha"));

        IReadOnlyList<Edge> dependencies = FilesProjection.ExternalDependencies(
            graph,
            Array.Empty<Filter>(),
            Array.Empty<Filter>());

        Assert.Equal(
            new[]
            {
                new Edge("A/a.cs", "Alpha", external: true, ImportKind.Using),
                new Edge("A/a.cs", "Mike", external: true, ImportKind.Using),
                new Edge("Z/z.cs", "Beta", external: true, ImportKind.Using),
            },
            dependencies);
    }

    [Fact]
    public void ExternalDependencies_rejects_a_null_graph()
    {
        Assert.Throws<ArgumentNullException>(() =>
            FilesProjection.ExternalDependencies(null!, Array.Empty<Filter>(), Array.Empty<Filter>()));
    }

    [Fact]
    public void ExternalDependencies_rejects_null_subject_filters()
    {
        Assert.Throws<ArgumentNullException>(() =>
            FilesProjection.ExternalDependencies(Graph(), null!, Array.Empty<Filter>()));
    }

    [Fact]
    public void ExternalDependencies_rejects_null_object_filters()
    {
        Assert.Throws<ArgumentNullException>(() =>
            FilesProjection.ExternalDependencies(Graph(), Array.Empty<Filter>(), null!));
    }

    [Fact]
    public void Detail_derives_the_name_extension_and_directory_from_the_identifier()
    {
        FileDetail detail = FilesProjection.Detail("src/Models/Car.cs", "source text");

        Assert.Equal("src/Models/Car.cs", detail.Path);
        Assert.Equal("Car", detail.NameWithoutExtension);
        Assert.Equal(".cs", detail.Extension);
        Assert.Equal("src/Models", detail.Directory);
        Assert.Equal("source text", detail.SourceText);
    }

    [Fact]
    public void Detail_keeps_a_root_level_files_directory_empty()
    {
        FileDetail detail = FilesProjection.Detail("Car.cs", "source text");

        Assert.Equal("Car", detail.NameWithoutExtension);
        Assert.Equal(".cs", detail.Extension);
        Assert.Equal(string.Empty, detail.Directory);
    }

    [Fact]
    public void Detail_keeps_a_dotless_files_extension_empty()
    {
        FileDetail detail = FilesProjection.Detail("Makefile", "source text");

        Assert.Equal("Makefile", detail.NameWithoutExtension);
        Assert.Equal(string.Empty, detail.Extension);
        Assert.Equal(string.Empty, detail.Directory);
    }

    [Fact]
    public void Detail_counts_every_line_that_is_not_blank_or_whitespace_only()
    {
        FileDetail detail = FilesProjection.Detail(
            "src/Models/Car.cs",
            "namespace App.Models;\n\npublic class Car { }\n   \n\t\npublic class Truck { }\n");

        Assert.Equal(3, detail.NonBlankLineCount);
    }

    [Fact]
    public void Detail_counts_windows_line_endings_once()
    {
        FileDetail detail = FilesProjection.Detail("src/Models/Car.cs", "a\r\n\r\nb\r\n");

        Assert.Equal(2, detail.NonBlankLineCount);
    }

    [Fact]
    public void Detail_reports_zero_non_blank_lines_for_an_empty_source()
    {
        FileDetail detail = FilesProjection.Detail("src/Models/Car.cs", string.Empty);

        Assert.Equal(0, detail.NonBlankLineCount);
    }

    [Fact]
    public void Detail_rejects_a_null_identifier()
    {
        Assert.Throws<ArgumentNullException>(() => FilesProjection.Detail(null!, "text"));
    }

    [Fact]
    public void Detail_rejects_null_source_text()
    {
        Assert.Throws<ArgumentNullException>(() => FilesProjection.Detail("a.cs", null!));
    }

    private static Graph Graph(params Edge[] edges) => new(edges);

    private static Edge Self(string file) => new(file, file, external: false, ImportKind.None);

    private static Edge Using(string source, string target) =>
        new(source, target, external: false, ImportKind.Using);

    private static Edge External(string source, string module) =>
        new(source, module, external: true, ImportKind.Using);

    private static Filter Filename(string glob) => new(new Pattern(glob), MatchTarget.Filename);

    private static Filter Folder(string glob) => new(new Pattern(glob), MatchTarget.PathWithoutFilename);

    private static Filter Path(string glob) => new(new Pattern(glob), MatchTarget.Path);

    private static Filter File(string glob) => new(new Pattern(glob), MatchTarget.Classname);

    private static Filter Module(string glob) => new(new Pattern(glob), MatchTarget.Path);
}
