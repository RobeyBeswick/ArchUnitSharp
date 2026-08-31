using ArchUnitSharp.Common.Extraction;
using ArchUnitSharp.Slices.Uml;

namespace ArchUnitSharp.Slices.Tests.Uml;

public class PlantUmlParserTests
{
    [Fact]
    public void Parses_declarations_arrows_comments_directives_and_implicit_components()
    {
        const string text = """
            @startuml Architecture
            ' whole-line comment
            // another comment
            component [api]
            component [services]
            [api] -> [services]
            [services] --> [models] ' inline comment
            [api] -> [services]
            skinparam componentStyle rectangle
            @enduml
            """;

        PlantUmlDiagram diagram = PlantUmlParser.Parse(text);

        Assert.Equal(new[] { "api", "services", "models" }, diagram.Components);
        Assert.Equal(
            new[]
            {
                new PlantUmlDependency("api", "services"),
                new PlantUmlDependency("services", "models"),
            },
            diagram.Dependencies);
    }

    [Fact]
    public void Allows_returns_true_only_for_a_declared_arrow()
    {
        PlantUmlDiagram diagram = PlantUmlParser.Parse("[api] --> [services]");

        Assert.True(diagram.Allows("api", "services"));
        Assert.False(diagram.Allows("services", "api"));
        Assert.False(diagram.Allows("api", "models"));
    }

    [Fact]
    public void Both_arrow_forms_parse_to_the_same_dependency()
    {
        PlantUmlDiagram single = PlantUmlParser.Parse("[api] -> [services]");
        PlantUmlDiagram twoDash = PlantUmlParser.Parse("[api] --> [services]");

        Assert.Equal(single.Dependencies, twoDash.Dependencies);
    }

    [Fact]
    public void A_duplicate_component_declaration_collapses()
    {
        PlantUmlDiagram diagram = PlantUmlParser.Parse(
            "component [api]\ncomponent [api]\ncomponent [services]");

        Assert.Equal(new[] { "api", "services" }, diagram.Components);
    }

    [Fact]
    public void A_duplicate_arrow_collapses()
    {
        PlantUmlDiagram diagram = PlantUmlParser.Parse("[api] --> [services]\n[api] -> [services]");

        Assert.Equal(new[] { new PlantUmlDependency("api", "services") }, diagram.Dependencies);
    }

    [Fact]
    public void Component_names_are_trimmed()
    {
        PlantUmlDiagram diagram = PlantUmlParser.Parse("component [ api ]");

        Assert.Equal(new[] { "api" }, diagram.Components);
    }

    [Fact]
    public void An_arrow_declares_its_endpoints_as_components()
    {
        PlantUmlDiagram diagram = PlantUmlParser.Parse("[api] --> [services]");

        Assert.Equal(new[] { "api", "services" }, diagram.Components);
    }

    [Fact]
    public void The_delimiters_are_optional()
    {
        PlantUmlDiagram without = PlantUmlParser.Parse("[api] --> [services]");

        Assert.Equal(new[] { new PlantUmlDependency("api", "services") }, without.Dependencies);
    }

    [Fact]
    public void A_malformed_component_line_names_its_line_number()
    {
        var exception = Assert.Throws<UserError>(() =>
            PlantUmlParser.Parse("component [api]\ncomponent services\n[api] --> [services]"));

        Assert.Contains("line 2", exception.Message);
    }

    [Fact]
    public void An_empty_component_name_is_malformed()
    {
        Assert.Throws<UserError>(() => PlantUmlParser.Parse("component []"));
    }

    [Fact]
    public void A_whitespace_only_component_name_names_its_line_number()
    {
        var exception = Assert.Throws<UserError>(() =>
            PlantUmlParser.Parse("component [api]\ncomponent [ ]"));

        Assert.Contains("line 2", exception.Message);
    }

    [Fact]
    public void A_whitespace_only_arrow_endpoint_is_malformed()
    {
        var exception = Assert.Throws<UserError>(() =>
            PlantUmlParser.Parse("[ ] --> [services]"));

        Assert.Contains("line 1", exception.Message);
    }

    [Fact]
    public void A_malformed_arrow_line_names_its_line_number()
    {
        var exception = Assert.Throws<UserError>(() =>
            PlantUmlParser.Parse("[api] --> [services]\n[api] -- [services]"));

        Assert.Contains("line 2", exception.Message);
    }

    [Fact]
    public void An_unbracketed_arrow_target_is_malformed()
    {
        Assert.Throws<UserError>(() => PlantUmlParser.Parse("[api] --> services"));
    }

    [Fact]
    public void A_line_the_subset_does_not_recognise_is_ignored()
    {
        PlantUmlDiagram diagram = PlantUmlParser.Parse(
            "title Architecture\n[api] --> [services]\nlegend\nsome random line\nendlegend");

        Assert.Equal(new[] { new PlantUmlDependency("api", "services") }, diagram.Dependencies);
    }

    [Fact]
    public void Null_text_is_rejected()
    {
        Assert.Throws<ArgumentNullException>(() => PlantUmlParser.Parse(null!));
    }

    [Fact]
    public void Blank_text_is_rejected()
    {
        Assert.Throws<ArgumentException>(() => PlantUmlParser.Parse("   "));
    }

    [Fact]
    public void Parsing_the_same_text_twice_yields_equal_diagrams()
    {
        const string text = "component [api]\n[api] --> [services]";

        PlantUmlDiagram first = PlantUmlParser.Parse(text);
        PlantUmlDiagram second = PlantUmlParser.Parse(text);

        Assert.Equal(first.Components, second.Components);
        Assert.Equal(first.Dependencies, second.Dependencies);
    }
}
