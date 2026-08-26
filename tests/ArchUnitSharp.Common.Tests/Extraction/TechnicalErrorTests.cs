using ArchUnitSharp.Common.Extraction;

namespace ArchUnitSharp.Common.Tests.Extraction;

public class TechnicalErrorTests
{
    [Fact]
    public void Is_an_error_and_an_exception()
    {
        var error = new TechnicalError();

        Assert.IsAssignableFrom<Error>(error);
        Assert.IsAssignableFrom<Exception>(error);
    }

    [Fact]
    public void Is_a_distinct_kind_from_user_error()
    {
        var technical = new TechnicalError();
        var user = new UserError();

        Assert.IsNotType<UserError>(technical);
        Assert.IsNotType<TechnicalError>(user);
    }

    [Fact]
    public void Carries_the_message_it_was_given()
    {
        var error = new TechnicalError("the graph cache could not be written");

        Assert.Equal("the graph cache could not be written", error.Message);
    }

    [Fact]
    public void Carries_the_inner_exception_it_was_given()
    {
        var cause = new IOException("disk full");
        var error = new TechnicalError("the graph cache could not be written", cause);

        Assert.Same(cause, error.InnerException);
    }

    [Fact]
    public void Is_sealed_so_the_error_family_stays_closed()
    {
        Assert.True(typeof(TechnicalError).IsSealed);
    }
}
