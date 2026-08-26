using ArchUnitSharp.Common.Extraction;

namespace ArchUnitSharp.Common.Tests.Extraction;

public class ErrorTests
{
    [Fact]
    public void Is_abstract_so_no_third_error_kind_can_exist()
    {
        Assert.True(typeof(Error).IsAbstract);
    }

    [Fact]
    public void Is_an_exception_so_every_non_rule_failure_is_catchable()
    {
        Assert.True(typeof(Exception).IsAssignableFrom(typeof(Error)));
    }

    [Fact]
    public void Both_concrete_kinds_share_the_single_base()
    {
        Assert.True(typeof(TechnicalError).IsAssignableTo(typeof(Error)));
        Assert.True(typeof(UserError).IsAssignableTo(typeof(Error)));
    }

    [Fact]
    public void A_rule_failure_is_not_an_error()
    {
        var violation = new EmptyTestViolation("layers should be acyclic");

        Assert.IsNotAssignableFrom<Error>(violation);
        Assert.IsAssignableFrom<Violation>(violation);
    }
}
