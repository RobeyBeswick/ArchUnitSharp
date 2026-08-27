namespace ArchUnitSharp.Extraction.Tests;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

public class IgnoreDirectiveReaderTests
{
    private static UsingDirectiveSyntax DirectiveOf(string source) =>
        CSharpSyntaxTree.ParseText(source)
            .GetRoot()
            .DescendantNodes()
            .OfType<UsingDirectiveSyntax>()
            .Single();

    [Fact]
    public void ShouldIgnore_ignores_a_directive_with_an_inline_comment()
    {
        Assert.True(IgnoreDirectiveReader.ShouldIgnore(DirectiveOf("using System; // archunit: ignore\n")));
    }

    [Fact]
    public void ShouldIgnore_ignores_a_directive_with_a_comment_on_the_line_above()
    {
        Assert.True(IgnoreDirectiveReader.ShouldIgnore(DirectiveOf("// archunit: ignore\nusing System;\n")));
    }

    [Fact]
    public void ShouldIgnore_ignores_a_directive_with_trailing_whitespace_in_the_comment()
    {
        Assert.True(IgnoreDirectiveReader.ShouldIgnore(DirectiveOf("using System; // archunit: ignore   \n")));
    }

    [Fact]
    public void ShouldIgnore_does_not_ignore_a_directive_with_a_plain_comment()
    {
        Assert.False(IgnoreDirectiveReader.ShouldIgnore(DirectiveOf("using System; // not an ignore\n")));
    }

    [Fact]
    public void ShouldIgnore_does_not_ignore_a_comment_on_the_line_after_the_directive()
    {
        Assert.False(IgnoreDirectiveReader.ShouldIgnore(DirectiveOf("using System;\n// archunit: ignore\n")));
    }

    [Fact]
    public void ShouldIgnore_does_not_ignore_a_standalone_comment_two_lines_above()
    {
        Assert.False(IgnoreDirectiveReader.ShouldIgnore(DirectiveOf("// archunit: ignore\n\nusing System;\n")));
    }

    [Fact]
    public void ShouldIgnore_does_not_ignore_a_directive_with_a_block_comment()
    {
        Assert.False(IgnoreDirectiveReader.ShouldIgnore(DirectiveOf("using System; /* archunit: ignore */\n")));
    }

    [Fact]
    public void ShouldIgnore_does_not_treat_a_doc_comment_as_an_ignore()
    {
        Assert.False(IgnoreDirectiveReader.ShouldIgnore(DirectiveOf("/// archunit: ignore\nusing System;\n")));
    }

    [Fact]
    public void ShouldIgnore_recognises_the_archunit_dash_form()
    {
        Assert.True(IgnoreDirectiveReader.ShouldIgnore(DirectiveOf("using System; // archunit-ignore\n")));
    }

    [Fact]
    public void ShouldIgnore_ignores_a_scoped_directive_when_the_module_matches_exactly()
    {
        Assert.True(IgnoreDirectiveReader.ShouldIgnore(
            DirectiveOf("using MyApp.Models; // archunit: ignore MyApp.Models\n")));
    }

    [Fact]
    public void ShouldIgnore_ignores_a_scoped_directive_when_the_module_is_an_ancestor()
    {
        Assert.True(IgnoreDirectiveReader.ShouldIgnore(
            DirectiveOf("using MyApp.Models.Car; // archunit: ignore MyApp\n")));
    }

    [Fact]
    public void ShouldIgnore_ignores_a_scoped_directive_when_any_listed_module_matches()
    {
        Assert.True(IgnoreDirectiveReader.ShouldIgnore(
            DirectiveOf("using MyApp.Models; // archunit: ignore Other.App MyApp.Models\n")));
    }

    [Fact]
    public void ShouldIgnore_keeps_a_scoped_directive_when_the_module_does_not_match()
    {
        Assert.False(IgnoreDirectiveReader.ShouldIgnore(
            DirectiveOf("using MyApp.Models; // archunit: ignore Other.App\n")));
    }

    [Fact]
    public void ShouldIgnore_does_not_prefix_match_a_sibling_name()
    {
        Assert.False(IgnoreDirectiveReader.ShouldIgnore(
            DirectiveOf("using MyAppModels; // archunit: ignore MyApp\n")));
    }

    [Fact]
    public void ShouldIgnore_ignores_a_scoped_standalone_comment()
    {
        Assert.True(IgnoreDirectiveReader.ShouldIgnore(
            DirectiveOf("// archunit: ignore MyApp.Models\nusing MyApp.Models;\n")));
    }

    [Fact]
    public void ShouldIgnore_ignores_only_the_directive_whose_line_the_comment_trails()
    {
        UsingDirectiveSyntax[] directives = CSharpSyntaxTree.ParseText(
                "using System; using MyApp.Models; // archunit: ignore\n")
            .GetRoot()
            .DescendantNodes()
            .OfType<UsingDirectiveSyntax>()
            .ToArray();

        Assert.Equal(2, directives.Length);
        Assert.False(IgnoreDirectiveReader.ShouldIgnore(directives[0]));
        Assert.True(IgnoreDirectiveReader.ShouldIgnore(directives[1]));
    }
}
