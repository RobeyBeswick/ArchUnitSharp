namespace ArchUnitSharp.Extraction.Tests;

using ArchUnitSharp.Common.Extraction;

public class ImportParserTests
{
    [Fact]
    public void Parse_finds_a_simple_using()
    {
        IReadOnlyList<Import> imports = ImportParser.Parse("using System;");

        Import import = Assert.Single(imports);
        Assert.Equal(ImportKind.Using, import.Kind);
        Assert.Equal("System", import.Name);
    }

    [Fact]
    public void Parse_finds_a_qualified_using()
    {
        IReadOnlyList<Import> imports = ImportParser.Parse("using System.Linq;");

        Import import = Assert.Single(imports);
        Assert.Equal(ImportKind.Using, import.Kind);
        Assert.Equal("System.Linq", import.Name);
    }

    [Fact]
    public void Parse_finds_a_static_using()
    {
        IReadOnlyList<Import> imports = ImportParser.Parse("using static System.Math;");

        Import import = Assert.Single(imports);
        Assert.Equal(ImportKind.UsingStatic, import.Kind);
        Assert.Equal("System.Math", import.Name);
    }

    [Fact]
    public void Parse_finds_a_global_using()
    {
        IReadOnlyList<Import> imports = ImportParser.Parse("global using System;");

        Import import = Assert.Single(imports);
        Assert.Equal(ImportKind.GlobalUsing, import.Kind);
        Assert.Equal("System", import.Name);
    }

    [Fact]
    public void Parse_finds_an_aliased_using()
    {
        IReadOnlyList<Import> imports = ImportParser.Parse("using Models = System.Data.Models;");

        Import import = Assert.Single(imports);
        Assert.Equal(ImportKind.AliasUsing, import.Kind);
        Assert.Equal("System.Data.Models", import.Name);
    }

    [Fact]
    public void Parse_classifies_a_global_static_using_as_global()
    {
        IReadOnlyList<Import> imports = ImportParser.Parse("global using static System.Math;");

        Import import = Assert.Single(imports);
        Assert.Equal(ImportKind.GlobalUsing, import.Kind);
        Assert.Equal("System.Math", import.Name);
    }

    [Fact]
    public void Parse_ignores_non_using_declarations()
    {
        string source = "namespace App { public class Car { public void Drive() { } } }";

        Assert.Empty(ImportParser.Parse(source));
    }

    [Fact]
    public void Parse_finds_usings_inside_a_block_namespace()
    {
        string source = "namespace App { using System.Text; class C { } }";

        Import import = Assert.Single(ImportParser.Parse(source));
        Assert.Equal(ImportKind.Using, import.Kind);
        Assert.Equal("System.Text", import.Name);
    }

    [Fact]
    public void Parse_finds_usings_before_a_file_scoped_namespace()
    {
        string source = "using System.IO;\nnamespace App;\nclass C { }";

        Import import = Assert.Single(ImportParser.Parse(source));
        Assert.Equal("System.IO", import.Name);
    }

    [Fact]
    public void Parse_sorts_imports_by_name_then_kind()
    {
        string source = """
            using Zeta;
            using Alpha;
            using static System.Math;
            using System;
            """;

        Assert.Equal(
            new[] { "Alpha", "System", "System.Math", "Zeta" },
            ImportParser.Parse(source).Select(import => import.Name));
    }

    [Fact]
    public void Parse_sorts_same_named_imports_by_kind()
    {
        string source = """
            using static System.Math;
            using System.Math;
            """;

        Assert.Equal(
            new[] { ImportKind.Using, ImportKind.UsingStatic },
            ImportParser.Parse(source).Select(import => import.Kind));
    }

    [Fact]
    public void Parse_returns_empty_for_an_empty_source()
    {
        Assert.Empty(ImportParser.Parse(string.Empty));
    }

    [Fact]
    public void Parse_returns_empty_for_a_source_that_fails_to_parse()
    {
        Assert.Empty(ImportParser.Parse("namespace App { public class Car { "));
    }

    [Fact]
    public void Parse_returns_a_fresh_list_on_every_call()
    {
        IReadOnlyList<Import> first = ImportParser.Parse("using System;");
        IReadOnlyList<Import> second = ImportParser.Parse("using System;");

        Assert.NotSame(first, second);
    }

    [Fact]
    public void Parse_rejects_a_null_source()
    {
        Assert.Throws<ArgumentNullException>(() => ImportParser.Parse(null!));
    }
}
