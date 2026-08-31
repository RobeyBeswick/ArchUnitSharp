using ArchUnitSharp.Common.Extraction;
using ArchUnitSharp.Metrics.Projection;

namespace ArchUnitSharp.Metrics.Tests;

public class DistanceProjectionTests
{
    [Fact]
    public void Build_carries_the_file_facts_into_the_distance_info()
    {
        var files = new[] { File("src/Models/Car.cs", types: 2, abstractTypes: 1, lines: 40) };

        IReadOnlyList<DistanceInfo> infos = DistanceProjection.Build(
            files,
            Graph(Self("src/Models/Car.cs")));

        DistanceInfo info = Assert.Single(infos);
        Assert.Equal("src/Models/Car.cs", info.File);
        Assert.Equal(2, info.TypeCount);
        Assert.Equal(1, info.AbstractTypeCount);
        Assert.Equal(40, info.LinesOfCode);
        Assert.Equal(0, info.AfferentCoupling);
        Assert.Equal(0, info.EfferentCoupling);
        Assert.Equal(1, info.ProjectFileCount);
    }

    [Fact]
    public void Build_counts_a_files_outgoing_and_incoming_couplings()
    {
        var files = new[]
        {
            File("src/A.cs"),
            File("src/B.cs"),
            File("src/C.cs"),
        };

        IReadOnlyList<DistanceInfo> infos = DistanceProjection.Build(
            files,
            Graph(
                Self("src/A.cs"),
                Self("src/B.cs"),
                Self("src/C.cs"),
                Edge("src/A.cs", "src/B.cs"),
                Edge("src/A.cs", "src/C.cs")));

        Assert.Equal(2, infos.Single(info => info.File == "src/A.cs").EfferentCoupling);
        Assert.Equal(0, infos.Single(info => info.File == "src/A.cs").AfferentCoupling);
        Assert.Equal(1, infos.Single(info => info.File == "src/B.cs").AfferentCoupling);
        Assert.Equal(1, infos.Single(info => info.File == "src/C.cs").AfferentCoupling);
    }

    [Fact]
    public void Build_does_not_count_self_edges_as_couplings()
    {
        var files = new[] { File("src/A.cs"), File("src/B.cs") };

        IReadOnlyList<DistanceInfo> infos = DistanceProjection.Build(
            files,
            Graph(
                Self("src/A.cs"),
                Self("src/B.cs"),
                Edge("src/A.cs", "src/B.cs")));

        Assert.Equal(1, infos.Single(info => info.File == "src/A.cs").EfferentCoupling);
        Assert.Equal(0, infos.Single(info => info.File == "src/A.cs").AfferentCoupling);
    }

    [Fact]
    public void Build_does_not_count_external_edges_as_couplings()
    {
        var files = new[] { File("src/A.cs") };

        IReadOnlyList<DistanceInfo> infos = DistanceProjection.Build(
            files,
            Graph(
                Self("src/A.cs"),
                new Edge("src/A.cs", "System", external: true, ImportKind.Using)));

        Assert.Equal(0, Assert.Single(infos).EfferentCoupling);
    }

    [Fact]
    public void Build_counts_a_subjects_partners_over_the_whole_graph()
    {
        var files = new[] { File("src/A.cs") };

        IReadOnlyList<DistanceInfo> infos = DistanceProjection.Build(
            files,
            Graph(
                Self("src/A.cs"),
                Self("src/B.cs"),
                Edge("src/A.cs", "src/B.cs")));

        DistanceInfo info = Assert.Single(infos);
        Assert.Equal("src/A.cs", info.File);
        Assert.Equal(1, info.EfferentCoupling);
        Assert.Equal(2, info.ProjectFileCount);
    }

    [Fact]
    public void Build_does_not_count_an_edge_whose_target_is_not_a_project_file()
    {
        var files = new[] { File("src/A.cs"), File("src/B.cs") };

        IReadOnlyList<DistanceInfo> infos = DistanceProjection.Build(
            files,
            Graph(
                Self("src/A.cs"),
                Self("src/B.cs"),
                Edge("src/A.cs", "src/Orphan.cs")));

        Assert.Equal(0, infos.Single(info => info.File == "src/A.cs").EfferentCoupling);
    }

    [Fact]
    public void Build_counts_each_coupling_target_once()
    {
        var files = new[] { File("src/A.cs"), File("src/B.cs") };

        IReadOnlyList<DistanceInfo> infos = DistanceProjection.Build(
            files,
            Graph(
                Self("src/A.cs"),
                Self("src/B.cs"),
                Edge("src/A.cs", "src/B.cs"),
                Edge("src/A.cs", "src/B.cs")));

        Assert.Equal(1, infos.Single(info => info.File == "src/A.cs").EfferentCoupling);
        Assert.Equal(1, infos.Single(info => info.File == "src/B.cs").AfferentCoupling);
    }

    [Fact]
    public void Build_counts_the_project_files_as_the_distinct_sources()
    {
        var files = new[] { File("src/A.cs"), File("src/B.cs"), File("src/C.cs") };

        IReadOnlyList<DistanceInfo> infos = DistanceProjection.Build(
            files,
            Graph(
                Self("src/A.cs"),
                Self("src/B.cs"),
                Edge("src/C.cs", "src/A.cs")));

        Assert.All(infos, static info => Assert.Equal(3, info.ProjectFileCount));
    }

    [Fact]
    public void Build_keeps_the_order_of_the_supplied_files()
    {
        var files = new[] { File("src/Z.cs"), File("src/A.cs") };

        IReadOnlyList<DistanceInfo> infos = DistanceProjection.Build(
            files,
            Graph(Self("src/Z.cs"), Self("src/A.cs")));

        Assert.Equal(new[] { "src/Z.cs", "src/A.cs" }, infos.Select(static info => info.File));
    }

    [Fact]
    public void Build_rejects_null_files()
    {
        Assert.Throws<ArgumentNullException>(() =>
            DistanceProjection.Build(null!, Graph(Self("src/A.cs"))));
    }

    [Fact]
    public void Build_rejects_a_null_graph()
    {
        Assert.Throws<ArgumentNullException>(() =>
            DistanceProjection.Build(new[] { File("src/A.cs") }, null!));
    }

    private static FileInfo File(string path, int types = 1, int abstractTypes = 0, int lines = 0) =>
        new(path, lines, 0, 0, types, 0, types, abstractTypes, Array.Empty<ClassInfo>());

    private static Graph Graph(params Edge[] edges) => new(edges);

    private static Edge Self(string file) => new(file, file, external: false, ImportKind.None);

    private static Edge Edge(string source, string target) =>
        new(source, target, external: false, ImportKind.Using);
}
