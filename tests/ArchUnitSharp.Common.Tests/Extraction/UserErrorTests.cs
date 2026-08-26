using ArchUnitSharp.Common.Extraction;

namespace ArchUnitSharp.Common.Tests.Extraction;

public class UserErrorTests
{
    [Fact]
    public void Is_an_error_and_an_exception()
    {
        var error = new UserError();

        Assert.IsAssignableFrom<Error>(error);
        Assert.IsAssignableFrom<Exception>(error);
    }

    [Fact]
    public void Is_a_distinct_kind_from_technical_error()
    {
        Assert.IsNotType<TechnicalError>(new UserError());
    }

    [Fact]
    public void Carries_the_message_it_was_given()
    {
        var error = new UserError("a pattern may not mix glob and regex syntax");

        Assert.Equal("a pattern may not mix glob and regex syntax", error.Message);
    }

    [Fact]
    public void Carries_the_inner_exception_it_was_given()
    {
        var cause = new ArgumentException("invalid glob");
        var error = new UserError("a pattern may not mix glob and regex syntax", cause);

        Assert.Same(cause, error.InnerException);
    }

    [Fact]
    public void Is_sealed_so_the_error_family_stays_closed()
    {
        Assert.True(typeof(UserError).IsSealed);
    }
}
