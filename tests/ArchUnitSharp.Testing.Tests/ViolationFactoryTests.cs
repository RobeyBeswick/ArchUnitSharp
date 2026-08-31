using ArchUnitSharp.Common.Extraction;
using ArchUnitSharp.Files;
using ArchUnitSharp.Layers;
using ArchUnitSharp.Slices;

namespace ArchUnitSharp.Testing.Tests;

public class ViolationFactoryTests
{
    private sealed record FakeViolation : Violation
    {
        public FakeViolation(ViolationKind kind) : base(kind) { }
    }

    [Fact]
    public void An_empty_test_violation_names_the_rule_that_matched_nothing()
    {
        var violation = new EmptyTestViolation("project files should not depend on themselves");

        string message = ViolationFactory.Format(violation);

        Assert.Equal(
            "The rule matched nothing: project files should not depend on themselves.",
            message);
    }

    [Fact]
    public void A_file_violation_names_the_file()
    {
        var violation = new FileViolation("src/App/Program.cs");

        string message = ViolationFactory.Format(violation);

        Assert.Equal("File 'src/App/Program.cs' violates the rule.", message);
    }

    [Fact]
    public void An_adhere_to_violation_names_the_file_and_the_rules_message()
    {
        var violation = new AdhereToViolation("src/App/Program.cs", "must log through the facade");

        string message = ViolationFactory.Format(violation);

        Assert.Equal("File 'src/App/Program.cs' violates the rule: must log through the facade", message);
    }

    [Fact]
    public void A_dependency_violation_names_the_forbidden_dependency()
    {
        var violation = new DependencyViolation("src/App/Program.cs", "src/Models/Car.cs");

        string message = ViolationFactory.Format(violation);

        Assert.Equal("File 'src/App/Program.cs' must not depend on 'src/Models/Car.cs'.", message);
    }

    [Fact]
    public void A_cycle_violation_renders_the_cycle_as_a_path()
    {
        var violation = new CycleViolation(new[] { "src/A.cs", "src/B.cs", "src/A.cs" });

        string message = ViolationFactory.Format(violation);

        Assert.Equal("Cycle: src/A.cs → src/B.cs → src/A.cs", message);
    }

    [Fact]
    public void A_layer_violation_names_the_layers_and_the_two_files()
    {
        var violation = new LayerViolation("App", "Models", "src/App/Program.cs", "src/Models/Car.cs");

        string message = ViolationFactory.Format(violation);

        Assert.Equal(
            "Layer 'App' must not depend on layer 'Models': 'src/App/Program.cs' depends on 'src/Models/Car.cs'.",
            message);
    }

    [Fact]
    public void A_forbidden_dependency_violation_names_the_slice_and_the_dependency()
    {
        var violation = new ForbiddenDependencyViolation(
            "billing",
            "src/features/billing/order.cs",
            "src/legacy/Old.cs");

        string message = ViolationFactory.Format(violation);

        Assert.Equal(
            "Slice 'billing' must not contain a dependency from 'src/features/billing/order.cs' "
            + "to 'src/legacy/Old.cs'.",
            message);
    }

    [Fact]
    public void A_missing_dependency_violation_names_the_slice_and_the_two_patterns()
    {
        var violation = new MissingDependencyViolation("auth", "src/features/billing/**", "src/shared/**");

        string message = ViolationFactory.Format(violation);

        Assert.Equal(
            "Slice 'auth' must contain a dependency from a file matching 'src/features/billing/**' "
            + "to a file matching 'src/shared/**'.",
            message);
    }

    [Fact]
    public void A_diagram_adherence_violation_names_the_two_slices()
    {
        var violation = new DiagramAdherenceViolation("billing", "System.Linq");

        string message = ViolationFactory.Format(violation);

        Assert.Equal(
            "Slice 'billing' must not depend on 'System.Linq' (not in the diagram).",
            message);
    }

    [Fact]
    public void A_null_violation_is_rejected()
    {
        Assert.Throws<ArgumentNullException>(() => ViolationFactory.Format(null!));
    }

    [Fact]
    public void An_unknown_violation_subtype_is_rejected()
    {
        var violation = new FakeViolation(ViolationKind.Rule);

        Assert.Throws<ArgumentOutOfRangeException>(() => ViolationFactory.Format(violation));
    }
}
