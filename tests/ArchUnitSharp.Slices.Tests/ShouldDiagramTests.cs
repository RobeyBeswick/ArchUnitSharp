using ArchUnitSharp.Common.Extraction;

namespace ArchUnitSharp.Slices.Tests;

public class ShouldDiagramTests
{
    [Fact]
    public void AdhereToDiagram_adds_a_diagram_rule()
    {
        var policy = new Slices(Graph(Self("billing/order.cs")))
            .DefinedBy("(**)/*.cs")
            .Should()
            .AdhereToDiagram("[billing] --> [shared]");

        DiagramRule rule = Assert.Single(policy.DiagramRules);
        Assert.Equal("adhere to diagram", rule.Description);
        Assert.Equal("billing", rule.Diagram.Components[0]);
        Assert.False(rule.Options.IgnoreOrphanSlices);
        Assert.False(rule.Options.IgnoreExternalSlices);
        Assert.Empty(policy.Rules);
    }

    [Fact]
    public void AdhereToDiagramInFile_reads_and_parses_the_diagram_now()
    {
        using var directory = new TempDir();
        string path = directory.File("architecture.puml");
        File.WriteAllText(path, "component [billing]\n[billing] --> [shared]");

        var policy = new Slices(Graph(Self("billing/order.cs")))
            .DefinedBy("(**)/*.cs")
            .Should()
            .AdhereToDiagramInFile(path);

        DiagramRule rule = Assert.Single(policy.DiagramRules);
        Assert.Equal("adhere to diagram in file '" + path + "'", rule.Description);
        Assert.True(rule.Diagram.Allows("billing", "shared"));
    }

    [Fact]
    public void AdhereToDiagramInFile_with_a_missing_file_is_a_technical_error()
    {
        using var directory = new TempDir();

        Assert.Throws<TechnicalError>(() =>
            new Slices(Graph(Self("billing/order.cs"))).Should().AdhereToDiagramInFile(directory.File("missing.puml")));
    }

    [Fact]
    public void AdhereToDiagram_rejects_a_null_diagram()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new Slices(Graph(Self("billing/order.cs"))).Should().AdhereToDiagram(null!));
    }

    [Fact]
    public void AdhereToDiagram_rejects_a_blank_diagram()
    {
        Assert.Throws<ArgumentException>(() =>
            new Slices(Graph(Self("billing/order.cs"))).Should().AdhereToDiagram("   "));
    }

    [Fact]
    public void AdhereToDiagram_rejects_a_malformed_diagram()
    {
        Assert.Throws<UserError>(() =>
            new Slices(Graph(Self("billing/order.cs"))).Should().AdhereToDiagram("component billing"));
    }

    [Fact]
    public void AdhereToDiagramInFile_rejects_a_null_path()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new Slices(Graph(Self("billing/order.cs"))).Should().AdhereToDiagramInFile(null!));
    }

    [Fact]
    public void AdhereToDiagramInFile_rejects_an_empty_path()
    {
        Assert.Throws<ArgumentException>(() =>
            new Slices(Graph(Self("billing/order.cs"))).Should().AdhereToDiagramInFile(string.Empty));
    }

    [Fact]
    public void IgnoringOrphanSlices_sets_the_modifier_on_the_next_diagram_rule()
    {
        var policy = new Slices(Graph(Self("billing/order.cs")))
            .DefinedBy("(**)/*.cs")
            .Should()
            .IgnoringOrphanSlices()
            .AdhereToDiagram("[billing] --> [shared]");

        Assert.True(Assert.Single(policy.DiagramRules).Options.IgnoreOrphanSlices);
    }

    [Fact]
    public void IgnoringExternalSlices_sets_the_modifier_on_the_next_diagram_rule()
    {
        var policy = new Slices(Graph(Self("billing/order.cs")))
            .DefinedBy("(**)/*.cs")
            .Should()
            .IgnoringExternalSlices()
            .AdhereToDiagram("[billing] --> [shared]");

        Assert.True(Assert.Single(policy.DiagramRules).Options.IgnoreExternalSlices);
    }

    [Fact]
    public void The_two_modifiers_combine()
    {
        var policy = new Slices(Graph(Self("billing/order.cs")))
            .DefinedBy("(**)/*.cs")
            .Should()
            .IgnoringOrphanSlices()
            .IgnoringExternalSlices()
            .AdhereToDiagram("[billing] --> [shared]");

        DiagramRule rule = Assert.Single(policy.DiagramRules);
        Assert.True(rule.Options.IgnoreOrphanSlices);
        Assert.True(rule.Options.IgnoreExternalSlices);
    }

    [Fact]
    public void Applying_a_modifier_does_not_mutate_the_original_mood()
    {
        var slices = new Slices(Graph(Self("billing/order.cs"))).DefinedBy("(**)/*.cs");
        Should parent = slices.Should();

        Should withOrphan = parent.IgnoringOrphanSlices();
        Should withExternal = parent.IgnoringExternalSlices();

        Assert.False(Assert.Single(parent.AdhereToDiagram("component [api]").DiagramRules).Options.IgnoreOrphanSlices);
        Assert.True(Assert.Single(withOrphan.AdhereToDiagram("component [api]").DiagramRules).Options.IgnoreOrphanSlices);
        Assert.True(Assert.Single(withExternal.AdhereToDiagram("component [api]").DiagramRules).Options.IgnoreExternalSlices);
    }

    private static Graph Graph(params Edge[] edges) => new(edges);

    private static Edge Self(string file) => new(file, file, external: false, ImportKind.None);
}
