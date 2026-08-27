namespace ArchUnitSharp.Extraction.Tests;

using ArchUnitSharp.Common.Extraction;

public class ImportResolverTests
{
    private static IReadOnlyList<Edge> Resolve(params (string Identifier, string Source)[] files)
    {
        SourceFile[] sourceFiles = files
            .Select(file => new SourceFile(file.Identifier, $"/repo/{file.Identifier}"))
            .ToArray();
        return ImportResolver.Resolve(sourceFiles, files.Select(file => file.Source).ToArray());
    }

    [Fact]
    public void Resolve_edges_to_the_file_declaring_the_imported_namespace()
    {
        IReadOnlyList<Edge> edges = Resolve(
            ("src/Models/Car.cs", "namespace MyApp.Models { public class Car { } }"),
            ("src/App/Program.cs", "using MyApp.Models; namespace MyApp.App { public class Program { } }"));

        Edge edge = Assert.Single(edges);
        Assert.Equal("src/App/Program.cs", edge.Source);
        Assert.Equal("src/Models/Car.cs", edge.Target);
        Assert.False(edge.External);
        Assert.Equal(ImportKind.Using, edge.ImportKinds);
    }

    [Fact]
    public void Resolve_resolves_a_file_scoped_namespace_target()
    {
        IReadOnlyList<Edge> edges = Resolve(
            ("src/Models/Car.cs", "namespace MyApp.Models; public class Car { }"),
            ("src/App/Program.cs", "using MyApp.Models; namespace MyApp.App; public class Program { }"));

        Edge edge = Assert.Single(edges);
        Assert.Equal("src/App/Program.cs", edge.Source);
        Assert.Equal("src/Models/Car.cs", edge.Target);
        Assert.False(edge.External);
    }

    [Fact]
    public void Resolve_resolves_a_namespace_declared_by_nested_namespace_blocks()
    {
        IReadOnlyList<Edge> edges = Resolve(
            ("src/Models/Car.cs", "namespace MyApp { namespace Models { public class Car { } } }"),
            ("src/App/Program.cs", "using MyApp.Models; namespace MyApp.App { public class Program { } }"));

        Edge edge = Assert.Single(edges);
        Assert.Equal("src/App/Program.cs", edge.Source);
        Assert.Equal("src/Models/Car.cs", edge.Target);
        Assert.False(edge.External);
    }

    [Fact]
    public void Resolve_edges_to_every_file_declaring_a_merged_namespace()
    {
        IReadOnlyList<Edge> edges = Resolve(
            ("src/Models/Car.cs", "namespace MyApp.Models { public class Car { } }"),
            ("src/Models/Bike.cs", "namespace MyApp.Models { public class Bike { } }"),
            ("src/App/Program.cs", "using MyApp.Models; namespace MyApp.App { public class Program { } }"));

        Assert.Equal(2, edges.Count);
        Assert.All(edges, edge =>
        {
            Assert.Equal("src/App/Program.cs", edge.Source);
            Assert.False(edge.External);
        });
        Assert.Equal(
            new[] { "src/Models/Bike.cs", "src/Models/Car.cs" },
            edges.Select(edge => edge.Target));
    }

    [Fact]
    public void Resolve_keeps_an_unresolved_directive_as_an_external_edge()
    {
        IReadOnlyList<Edge> edges = Resolve(
            ("src/App/Program.cs", "using System; namespace MyApp.App { public class Program { } }"));

        Edge edge = Assert.Single(edges);
        Assert.Equal("src/App/Program.cs", edge.Source);
        Assert.Equal("System", edge.Target);
        Assert.True(edge.External);
        Assert.Equal(ImportKind.Using, edge.ImportKinds);
    }

    [Fact]
    public void Resolve_keeps_the_written_name_as_the_external_target()
    {
        IReadOnlyList<Edge> edges = Resolve(
            ("src/App/Program.cs", "using System.Collections.Generic; namespace MyApp.App { public class Program { } }"));

        Edge edge = Assert.Single(edges);
        Assert.Equal("System.Collections.Generic", edge.Target);
        Assert.True(edge.External);
    }

    [Fact]
    public void Resolve_keeps_an_unresolved_static_using_as_external()
    {
        IReadOnlyList<Edge> edges = Resolve(
            ("src/App/Program.cs", "using static System.Math; namespace MyApp.App { public class Program { } }"));

        Edge edge = Assert.Single(edges);
        Assert.Equal("src/App/Program.cs", edge.Source);
        Assert.Equal("System.Math", edge.Target);
        Assert.True(edge.External);
        Assert.Equal(ImportKind.UsingStatic, edge.ImportKinds);
    }

    [Fact]
    public void Resolve_keeps_an_unresolved_alias_rhs_as_the_external_target()
    {
        IReadOnlyList<Edge> edges = Resolve(
            ("src/App/Program.cs", "using Sys = System.Text; namespace MyApp.App { public class Program { } }"));

        Edge edge = Assert.Single(edges);
        Assert.Equal("src/App/Program.cs", edge.Source);
        Assert.Equal("System.Text", edge.Target);
        Assert.True(edge.External);
        Assert.Equal(ImportKind.AliasUsing, edge.ImportKinds);
    }

    [Fact]
    public void Resolve_keeps_an_unresolved_global_using_as_an_external_edge()
    {
        IReadOnlyList<Edge> edges = Resolve(
            ("src/App/Program.cs", "global using System.IO; namespace MyApp.App { public class Program { } }"));

        Edge edge = Assert.Single(edges);
        Assert.Equal("System.IO", edge.Target);
        Assert.True(edge.External);
        Assert.Equal(ImportKind.GlobalUsing, edge.ImportKinds);
    }

    [Fact]
    public void Resolve_classifies_a_project_declared_parent_namespace_as_internal()
    {
        IReadOnlyList<Edge> edges = Resolve(
            ("src/Models/Car.cs", "namespace MyApp.Models { public class Car { } }"),
            ("src/App/Program.cs", "using MyApp; namespace Other.App { public class Program { } }"));

        Edge edge = Assert.Single(edges);
        Assert.Equal("src/App/Program.cs", edge.Source);
        Assert.Equal("src/Models/Car.cs", edge.Target);
        Assert.False(edge.External);
    }

    [Fact]
    public void Resolve_binds_a_static_using_to_an_internal_type()
    {
        IReadOnlyList<Edge> edges = Resolve(
            ("src/Models/Text.cs", "namespace MyApp.Models { public static class Text { } }"),
            ("src/App/Program.cs", "using static MyApp.Models.Text; namespace MyApp.App { public class Program { } }"));

        Edge edge = Assert.Single(edges);
        Assert.Equal("src/App/Program.cs", edge.Source);
        Assert.Equal("src/Models/Text.cs", edge.Target);
        Assert.False(edge.External);
        Assert.Equal(ImportKind.UsingStatic, edge.ImportKinds);
    }

    [Fact]
    public void Resolve_keeps_an_alias_kind_for_an_internal_alias_using()
    {
        IReadOnlyList<Edge> edges = Resolve(
            ("src/Models/Car.cs", "namespace MyApp.Models { public class Car { } }"),
            ("src/App/Program.cs", "using Car = MyApp.Models.Car; namespace MyApp.App { public class Program { } }"));

        Edge edge = Assert.Single(edges);
        Assert.Equal("src/App/Program.cs", edge.Source);
        Assert.Equal("src/Models/Car.cs", edge.Target);
        Assert.False(edge.External);
        Assert.Equal(ImportKind.AliasUsing, edge.ImportKinds);
    }

    [Fact]
    public void Resolve_keeps_a_global_using_kind()
    {
        IReadOnlyList<Edge> edges = Resolve(
            ("src/Models/Car.cs", "namespace MyApp.Models { public class Car { } }"),
            ("src/App/Program.cs", "global using MyApp.Models; namespace MyApp.App { public class Program { } }"));

        Edge edge = Assert.Single(edges);
        Assert.Equal("src/App/Program.cs", edge.Source);
        Assert.Equal("src/Models/Car.cs", edge.Target);
        Assert.Equal(ImportKind.GlobalUsing, edge.ImportKinds);
    }

    [Fact]
    public void Resolve_keeps_a_directive_that_references_the_files_own_namespace_as_a_self_edge()
    {
        IReadOnlyList<Edge> edges = Resolve(
            ("src/App/Program.cs", "using MyApp.App; namespace MyApp.App { public class Program { } }"));

        Edge edge = Assert.Single(edges);
        Assert.Equal("src/App/Program.cs", edge.Source);
        Assert.Equal("src/App/Program.cs", edge.Target);
        Assert.False(edge.External);
    }

    [Fact]
    public void Resolve_skips_a_file_that_fails_to_parse_and_treats_its_namespace_as_unresolvable()
    {
        IReadOnlyList<Edge> edges = Resolve(
            ("src/Broken/Broken.cs", "namespace MyApp.Models { public class Car { "),
            ("src/App/Program.cs", "using MyApp.Models; namespace MyApp.App { public class Program { } }"));

        Edge edge = Assert.Single(edges);
        Assert.Equal("src/App/Program.cs", edge.Source);
        Assert.Equal("MyApp.Models", edge.Target);
        Assert.True(edge.External);
    }

    [Fact]
    public void Resolve_skips_a_directive_inside_an_inactive_if_region()
    {
        IReadOnlyList<Edge> edges = Resolve(
            ("src/App/Program.cs",
             "#if WINDOWS\nusing Windows.Special;\n#endif\n"
             + "using System;\n"
             + "#if !WINDOWS\nusing System.IO;\n#endif\n"
             + "namespace MyApp.App { public class Program { } }"));

        Assert.Equal(2, edges.Count);
        Assert.Equal(new[] { "System", "System.IO" }, edges.Select(edge => edge.Target));
        Assert.All(edges, edge => Assert.True(edge.External));
    }

    [Fact]
    public void Resolve_produces_no_edges_from_a_file_with_no_usings()
    {
        IReadOnlyList<Edge> edges = Resolve(
            ("src/Models/Car.cs", "namespace MyApp.Models { public class Car { } }"));

        Assert.Empty(edges);
    }

    [Fact]
    public void Resolve_sorts_edges_by_source_then_target()
    {
        IReadOnlyList<Edge> edges = Resolve(
            ("b/File.cs", "using Zeta; using Alpha; namespace B { class C { } }"),
            ("a/File.cs", "using Beta; namespace A { class C { } }"));

        Assert.Equal(
            new[] { "a/File.cs", "b/File.cs", "b/File.cs" },
            edges.Select(edge => edge.Source));
        Assert.Equal(
            new[] { "Beta", "Alpha", "Zeta" },
            edges.Select(edge => edge.Target));
    }

    [Fact]
    public void Resolve_returns_a_fresh_list_on_every_call()
    {
        IReadOnlyList<Edge> first = Resolve(("a/File.cs", "using System; namespace A { }"));
        IReadOnlyList<Edge> second = Resolve(("a/File.cs", "using System; namespace A { }"));

        Assert.NotSame(first, second);
    }

    [Fact]
    public void Resolve_rejects_a_null_file_list()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ImportResolver.Resolve(null!, Array.Empty<string>()));
    }

    [Fact]
    public void Resolve_rejects_a_null_code_list()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ImportResolver.Resolve(Array.Empty<SourceFile>(), null!));
    }

    [Fact]
    public void Resolve_rejects_mismatched_list_lengths()
    {
        var file = new SourceFile("a/File.cs", "/repo/a/File.cs");

        Assert.Throws<ArgumentException>(() =>
            ImportResolver.Resolve(new[] { file }, Array.Empty<string>()));
    }

    [Fact]
    public void Resolve_rejects_a_null_code_entry()
    {
        var file = new SourceFile("a/File.cs", "/repo/a/File.cs");

        Assert.Throws<ArgumentException>(() =>
            ImportResolver.Resolve(new[] { file }, new string[] { null! }));
    }
}
