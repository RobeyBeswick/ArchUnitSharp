namespace ArchUnitSharp.Testing.Tests;

public class ColouriserTests
{
    [Theory]
    [InlineData(Colour.Black, 30)]
    [InlineData(Colour.Red, 31)]
    [InlineData(Colour.Green, 32)]
    [InlineData(Colour.Yellow, 33)]
    [InlineData(Colour.Blue, 34)]
    [InlineData(Colour.Magenta, 35)]
    [InlineData(Colour.Cyan, 36)]
    [InlineData(Colour.White, 37)]
    public void Apply_wraps_text_in_the_colours_sgr_code_and_resets_after(Colour colour, int code)
    {
        string wrapped = Colouriser.Apply("message", colour);

        Assert.Equal($"\u001b[{code}mmessage\u001b[0m", wrapped);
    }

    [Fact]
    public void Apply_rejects_null_text()
    {
        Assert.Throws<ArgumentNullException>(() => Colouriser.Apply(null!, Colour.Green));
    }

    [Fact]
    public void A_passed_result_is_coloured_green()
    {
        var result = new CheckResult(Passed: true, Message: "The rule passed.");

        CheckResult coloured = Colouriser.Apply(result);

        Assert.True(coloured.Passed);
        Assert.Equal("\u001b[32mThe rule passed.\u001b[0m", coloured.Message);
    }

    [Fact]
    public void A_failed_result_is_coloured_red()
    {
        var result = new CheckResult(Passed: false, Message: "File 'a.cs' violates the rule.");

        CheckResult coloured = Colouriser.Apply(result);

        Assert.False(coloured.Passed);
        Assert.Equal("\u001b[31mFile 'a.cs' violates the rule.\u001b[0m", coloured.Message);
    }

    [Fact]
    public void Colouring_a_result_leaves_the_original_unchanged()
    {
        var result = new CheckResult(Passed: false, Message: "File 'a.cs' violates the rule.");

        CheckResult coloured = Colouriser.Apply(result);

        Assert.Equal("File 'a.cs' violates the rule.", result.Message);
        Assert.NotEqual(coloured.Message, result.Message);
    }

    [Fact]
    public void Applying_the_result_overload_rejects_a_null_result()
    {
        Assert.Throws<ArgumentNullException>(() => Colouriser.Apply((CheckResult)null!));
    }
}
