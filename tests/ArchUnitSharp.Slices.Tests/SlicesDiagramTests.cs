using ArchUnitSharp.Common.Extraction;
using ArchUnitSharp.Slices.Uml;

namespace ArchUnitSharp.Slices.Tests;

public class SlicesDiagramTests
{
    [Fact]
    public void A_full_diagram_rule_passes_through_the_fluent_chain()
    {
        var policy = new Slices(Fixture())
            .DefinedBy("(**)/*.cs")
            .Should()
            .AdhereToDiagram(
                """
                component [billing]
                component [auth]
                component [shared]
                component [System.Linq]
                [billing] --> [shared]
                [auth] --> [shared]
                [billing] --> [System.Linq]
                """);

        Assert.Empty(policy.Check());
    }

    [Fact]
    public void A_full_diagram_rule_reports_a_violation_through_the_fluent_chain()
    {
        var policy = new Slices(Fixture())
            .DefinedBy("(**)/*.cs")
            .Should()
            .AdhereToDiagram("[billing] --> [shared]\n[auth] --> [shared]");

        IReadOnlyList<Violation> violations = policy.Check();

        Assert.Equal(
            new Violation[] { new DiagramAdherenceViolation("billing", "System.Linq") },
            violations);
    }

    [Fact]
    public void A_full_diagram_rule_with_modifiers_still_reports_a_disallowed_internal_dependency()
    {
        var policy = new Slices(Fixture())
            .DefinedBy("(**)/*.cs")
            .Should()
            .IgnoringExternalSlices()
            .AdhereToDiagram(
                """
                component [billing]
                component [auth]
                component [shared]
                [billing] --> [shared]
                """);

        IReadOnlyList<Violation> violations = policy.Check();

        Assert.Equal(
            new Violation[] { new DiagramAdherenceViolation("auth", "shared") },
            violations);
    }

    [Fact]
    public void ToPlantUml_renders_the_slicing_as_a_component_diagram()
    {
        string rendered = new Slices(Fixture())
            .DefinedBy("(**)/*.cs")
            .ToPlantUml();

        Assert.StartsWith("@startuml\n", rendered);
        Assert.Contains("  component [billing]", rendered);
        Assert.Contains("  component [auth]", rendered);
        Assert.Contains("  component [shared]", rendered);
        Assert.Contains("  component [System.Linq]", rendered);
        Assert.Contains("  [billing] --> [shared]", rendered);
        Assert.Contains("  [billing] --> [System.Linq]", rendered);
        Assert.EndsWith("@enduml\n", rendered);
    }

    [Fact]
    public void ToPlantUml_declares_an_orphan_slice_with_no_dependencies()
    {
        string rendered = new Slices(Fixture())
            .DefinedBy("(**)/*.cs")
            .ToPlantUml();

        Assert.Contains("  component [orphan]", rendered);
    }

    [Fact]
    public void ToPlantUml_round_trips_back_to_a_diagram_that_allows_the_actual_dependencies()
    {
        string rendered = new Slices(Fixture()).DefinedBy("(**)/*.cs").ToPlantUml();

        PlantUmlDiagram parsed = PlantUmlParser.Parse(rendered);

        Assert.True(parsed.Allows("billing", "shared"));
        Assert.True(parsed.Allows("auth", "shared"));
        Assert.True(parsed.Allows("billing", "System.Linq"));
        Assert.Contains("orphan", parsed.Components);
    }

    [Fact]
    public void ToPlantUml_of_an_empty_policy_renders_a_valid_empty_document()
    {
        string rendered = new Slices(Graph(Self("billing/order.cs"))).ToPlantUml();

        Assert.Equal("@startuml\n@enduml\n", rendered);
    }

    [Fact]
    public void ExportAsPlantUml_writes_the_diagram_to_disk()
    {
        using var directory = new TempDir();
        string path = directory.File("architecture.puml");

        var slices = new Slices(Fixture()).DefinedBy("(**)/*.cs");

        string written = slices.ExportAsPlantUml(path);

        Assert.Equal(path, written);
        Assert.Equal(slices.ToPlantUml(), File.ReadAllText(path));
    }

    [Fact]
    public void ExportAsPlantUml_with_a_missing_directory_is_a_technical_error()
    {
        using var directory = new TempDir();

        Assert.Throws<TechnicalError>(() =>
            new Slices(Fixture())
                .DefinedBy("(**)/*.cs")
                .ExportAsPlantUml(directory.File("nested/missing.puml")));
    }

    [Fact]
    public void ExportAsPlantUml_rejects_a_null_path()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new Slices(Fixture()).ExportAsPlantUml(null!));
    }

    [Fact]
    public void ExportAsPlantUml_rejects_an_empty_path()
    {
        Assert.Throws<ArgumentException>(() =>
            new Slices(Fixture()).ExportAsPlantUml(string.Empty));
    }

    private static Graph Fixture() => Graph(
        Self("billing/order.cs"),
        Self("billing/invoice.cs"),
        Self("auth/login.cs"),
        Self("shared/Util.cs"),
        Self("orphan/thing.cs"),
        Using("billing/order.cs", "shared/Util.cs"),
        Using("auth/login.cs", "shared/Util.cs"),
        External("billing/order.cs", "System.Linq"));

    private static Graph Graph(params Edge[] edges) => new(edges);

    private static Edge Self(string file) => new(file, file, external: false, ImportKind.None);

    private static Edge Using(string source, string target) =>
        new(source, target, external: false, ImportKind.Using);

    private static Edge External(string source, string module) =>
        new(source, module, external: true, ImportKind.Using);
}
