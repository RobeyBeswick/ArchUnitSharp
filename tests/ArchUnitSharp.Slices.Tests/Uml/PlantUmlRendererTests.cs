using ArchUnitSharp.Common.Extraction;
using ArchUnitSharp.Projection;
using ArchUnitSharp.Slices.Uml;

namespace ArchUnitSharp.Slices.Tests.Uml;

public class PlantUmlRendererTests
{
    [Fact]
    public void Renders_components_and_dependencies_in_stable_sorted_order()
    {
        var edges = new[]
        {
            Edge("services", "models"),
            Edge("api", "services"),
        };

        string rendered = PlantUmlRenderer.Render(edges, new[] { "orphan" });

        Assert.Equal(
            """
            @startuml
              component [api]
              component [models]
              component [orphan]
              component [services]
              [api] --> [services]
              [services] --> [models]
            @enduml
            """ + "\n",
            rendered);
    }

    [Fact]
    public void Declares_an_isolated_component_that_participates_in_no_arrow()
    {
        string rendered = PlantUmlRenderer.Render(Array.Empty<ProjectedEdge>(), new[] { "lonely" });

        Assert.Contains("  component [lonely]", rendered);
        Assert.Contains("@startuml", rendered);
        Assert.Contains("@enduml", rendered);
    }

    [Fact]
    public void Declares_every_edge_endpoint_even_when_absent_from_the_component_list()
    {
        string rendered = PlantUmlRenderer.Render(new[] { Edge("api", "services") }, Array.Empty<string>());

        Assert.Contains("  component [api]", rendered);
        Assert.Contains("  component [services]", rendered);
        Assert.Contains("  [api] --> [services]", rendered);
    }

    [Fact]
    public void Every_arrow_uses_the_two_dash_form()
    {
        string rendered = PlantUmlRenderer.Render(new[] { Edge("api", "services") }, Array.Empty<string>());

        Assert.Contains("  [api] --> [services]", rendered);
        Assert.DoesNotContain("->", rendered.Replace("-->", string.Empty));
    }

    [Fact]
    public void A_component_name_with_a_closing_bracket_is_rejected()
    {
        Assert.Throws<UserError>(() =>
            PlantUmlRenderer.Render(Array.Empty<ProjectedEdge>(), new[] { "bad]name" }));
    }

    [Fact]
    public void An_empty_component_name_is_rejected()
    {
        Assert.Throws<UserError>(() =>
            PlantUmlRenderer.Render(Array.Empty<ProjectedEdge>(), new[] { "   " }));
    }

    [Fact]
    public void An_edge_endpoint_that_is_not_a_valid_name_is_rejected()
    {
        Assert.Throws<UserError>(() =>
            PlantUmlRenderer.Render(new[] { Edge("bad]name", "services") }, Array.Empty<string>()));
    }

    [Fact]
    public void Null_edges_are_rejected()
    {
        Assert.Throws<ArgumentNullException>(() => PlantUmlRenderer.Render(null!, Array.Empty<string>()));
    }

    [Fact]
    public void Null_components_are_rejected()
    {
        Assert.Throws<ArgumentNullException>(() =>
            PlantUmlRenderer.Render(Array.Empty<ProjectedEdge>(), null!));
    }

    [Fact]
    public void Rendering_the_same_graph_twice_yields_identical_text()
    {
        var edges = new[] { Edge("services", "models"), Edge("api", "services") };

        string first = PlantUmlRenderer.Render(edges, new[] { "orphan" });
        string second = PlantUmlRenderer.Render(edges, new[] { "orphan" });

        Assert.Equal(first, second);
    }

    private static ProjectedEdge Edge(string source, string target) =>
        new(source, target, external: false, ImportKind.Using, new[] { Raw(source, target) });

    private static Edge Raw(string source, string target) =>
        new(source, target, external: false, ImportKind.Using);
}
