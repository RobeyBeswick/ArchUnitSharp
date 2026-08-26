using ArchUnitSharp.Common.Extraction;

namespace ArchUnitSharp.Common.Tests.Extraction;

public class ViolationTests
{
    private sealed record FakeViolation : Violation
    {
        public FakeViolation(ViolationKind kind) : base(kind) { }
    }

    [Fact]
    public void Concrete_subtype_exposes_its_kind()
    {
        var violation = new FakeViolation(ViolationKind.Rule);

        Assert.Equal(ViolationKind.Rule, violation.Kind);
    }

    [Fact]
    public void Abstract_type_cannot_be_instantiated()
    {
        Assert.True(typeof(Violation).IsAbstract);
    }
}
