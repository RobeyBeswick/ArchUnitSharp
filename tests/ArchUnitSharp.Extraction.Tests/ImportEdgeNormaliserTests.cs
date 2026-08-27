namespace ArchUnitSharp.Extraction.Tests;

using ArchUnitSharp.Common.Extraction;

public class ImportEdgeNormaliserTests
{
    private static SourceFile File(string identifier) =>
        new(identifier, $"/repo/{identifier}");

    private static Edge Edge(string source, string target, bool external = false, ImportKind kinds = ImportKind.Using) =>
        new(source, target, external, kinds);

    [Fact]
    public void Normalise_emits_a_self_edge_for_every_file()
    {
        IReadOnlyList<Edge> edges = ImportEdgeNormaliser.Normalise(
            new[] { File("src/Models/Car.cs"), File("src/App/Program.cs") },
            Array.Empty<Edge>());

        Assert.Equal(2, edges.Count);
        Assert.All(edges, edge =>
        {
            Assert.Equal(edge.Source, edge.Target);
            Assert.False(edge.External);
            Assert.Equal(ImportKind.None, edge.ImportKinds);
        });
    }

    [Fact]
    public void Normalise_makes_a_file_with_no_dependencies_a_node()
    {
        IReadOnlyList<Edge> edges = ImportEdgeNormaliser.Normalise(
            new[] { File("src/Models/Car.cs") },
            Array.Empty<Edge>());

        Edge edge = Assert.Single(edges);
        Assert.Equal("src/Models/Car.cs", edge.Source);
        Assert.Equal("src/Models/Car.cs", edge.Target);
        Assert.False(edge.External);
    }

    [Fact]
    public void Normalise_merges_parallel_edges_and_unions_their_import_kinds()
    {
        IReadOnlyList<Edge> edges = ImportEdgeNormaliser.Normalise(
            new[] { File("src/App/Program.cs"), File("src/Models/Text.cs") },
            new[]
            {
                Edge("src/App/Program.cs", "src/Models/Text.cs", kinds: ImportKind.Using),
                Edge("src/App/Program.cs", "src/Models/Text.cs", kinds: ImportKind.UsingStatic),
            });

        Edge merged = Assert.Single(edges, edge => edge.Target == "src/Models/Text.cs" && edge.Source != edge.Target);
        Assert.Equal("src/App/Program.cs", merged.Source);
        Assert.False(merged.External);
        Assert.Equal(ImportKind.Using | ImportKind.UsingStatic, merged.ImportKinds);
    }

    [Fact]
    public void Normalise_merges_three_parallel_edges_into_one()
    {
        IReadOnlyList<Edge> edges = ImportEdgeNormaliser.Normalise(
            new[] { File("src/App/Program.cs") },
            new[]
            {
                Edge("src/App/Program.cs", "System", external: true, kinds: ImportKind.Using),
                Edge("src/App/Program.cs", "System", external: true, kinds: ImportKind.UsingStatic),
                Edge("src/App/Program.cs", "System", external: true, kinds: ImportKind.GlobalUsing),
            });

        Edge merged = Assert.Single(edges, edge => edge.Source != edge.Target);
        Assert.Equal("System", merged.Target);
        Assert.True(merged.External);
        Assert.Equal(ImportKind.Using | ImportKind.UsingStatic | ImportKind.GlobalUsing, merged.ImportKinds);
    }

    [Fact]
    public void Normalise_keeps_edges_between_distinct_files_apart()
    {
        IReadOnlyList<Edge> edges = ImportEdgeNormaliser.Normalise(
            new[] { File("a.cs"), File("b.cs"), File("c.cs") },
            new[]
            {
                Edge("a.cs", "b.cs", kinds: ImportKind.Using),
                Edge("a.cs", "c.cs", kinds: ImportKind.UsingStatic),
                Edge("b.cs", "c.cs", kinds: ImportKind.AliasUsing),
            });

        Assert.Equal(6, edges.Count);
        Assert.Equal(3, edges.Count(edge => edge.Source != edge.Target));
    }

    [Fact]
    public void Normalise_outputs_a_unique_source_target_pair()
    {
        IReadOnlyList<Edge> edges = ImportEdgeNormaliser.Normalise(
            new[] { File("a.cs"), File("b.cs") },
            new[]
            {
                Edge("a.cs", "b.cs", kinds: ImportKind.Using),
                Edge("a.cs", "b.cs", kinds: ImportKind.UsingStatic),
                Edge("a.cs", "b.cs", kinds: ImportKind.GlobalUsing),
            });

        Assert.Equal(edges.Count, edges.Select(edge => (edge.Source, edge.Target)).Distinct().Count());
    }

    [Fact]
    public void Normalise_merges_a_real_self_import_with_the_emitted_self_edge()
    {
        IReadOnlyList<Edge> edges = ImportEdgeNormaliser.Normalise(
            new[] { File("src/App/Program.cs") },
            new[] { Edge("src/App/Program.cs", "src/App/Program.cs", kinds: ImportKind.Using) });

        Edge self = Assert.Single(edges);
        Assert.Equal("src/App/Program.cs", self.Source);
        Assert.Equal("src/App/Program.cs", self.Target);
        Assert.False(self.External);
        Assert.Equal(ImportKind.Using, self.ImportKinds);
    }

    [Fact]
    public void Normalise_keeps_a_merged_edge_internal_when_any_parallel_edge_is_internal()
    {
        IReadOnlyList<Edge> edges = ImportEdgeNormaliser.Normalise(
            new[] { File("a.cs"), File("b.cs") },
            new[]
            {
                Edge("a.cs", "b.cs", external: false, kinds: ImportKind.Using),
                Edge("a.cs", "b.cs", external: true, kinds: ImportKind.UsingStatic),
            });

        Edge merged = Assert.Single(edges, edge => edge.Source == "a.cs" && edge.Target == "b.cs");
        Assert.False(merged.External);
        Assert.Equal(ImportKind.Using | ImportKind.UsingStatic, merged.ImportKinds);
    }

    [Fact]
    public void Normalise_keeps_a_merged_edge_external_when_every_parallel_edge_is_external()
    {
        IReadOnlyList<Edge> edges = ImportEdgeNormaliser.Normalise(
            new[] { File("a.cs") },
            new[]
            {
                Edge("a.cs", "System", external: true, kinds: ImportKind.Using),
                Edge("a.cs", "System", external: true, kinds: ImportKind.GlobalUsing),
            });

        Edge merged = Assert.Single(edges, edge => edge.Source != edge.Target);
        Assert.True(merged.External);
        Assert.Equal(ImportKind.Using | ImportKind.GlobalUsing, merged.ImportKinds);
    }

    [Fact]
    public void Normalise_emits_a_self_edge_for_a_file_that_contributed_no_edges()
    {
        IReadOnlyList<Edge> edges = ImportEdgeNormaliser.Normalise(
            new[] { File("src/Broken/Broken.cs"), File("src/App/Program.cs") },
            new[] { Edge("src/App/Program.cs", "MyApp.Models", external: true, kinds: ImportKind.Using) });

        Edge self = Assert.Single(edges, edge => edge.Target == "src/Broken/Broken.cs");
        Assert.Equal("src/Broken/Broken.cs", self.Source);
        Assert.Equal("src/Broken/Broken.cs", self.Target);
    }

    [Fact]
    public void Normalise_sorts_by_source_then_target()
    {
        IReadOnlyList<Edge> edges = ImportEdgeNormaliser.Normalise(
            new[] { File("b/File.cs"), File("a/File.cs") },
            new[]
            {
                Edge("b/File.cs", "zeta", external: true, kinds: ImportKind.Using),
                Edge("a/File.cs", "alpha", external: true, kinds: ImportKind.Using),
            });

        Assert.Equal(
            new (string Source, string Target)[]
            {
                ("a/File.cs", "a/File.cs"),
                ("a/File.cs", "alpha"),
                ("b/File.cs", "b/File.cs"),
                ("b/File.cs", "zeta"),
            },
            edges.Select(edge => (edge.Source, edge.Target)));
    }

    [Fact]
    public void Normalise_returns_a_fresh_list_on_every_call()
    {
        IReadOnlyList<Edge> first = ImportEdgeNormaliser.Normalise(new[] { File("a.cs") }, Array.Empty<Edge>());
        IReadOnlyList<Edge> second = ImportEdgeNormaliser.Normalise(new[] { File("a.cs") }, Array.Empty<Edge>());

        Assert.NotSame(first, second);
    }

    [Fact]
    public void Normalise_rejects_a_null_file_list()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ImportEdgeNormaliser.Normalise(null!, Array.Empty<Edge>()));
    }

    [Fact]
    public void Normalise_rejects_a_null_edge_list()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ImportEdgeNormaliser.Normalise(Array.Empty<SourceFile>(), null!));
    }
}
