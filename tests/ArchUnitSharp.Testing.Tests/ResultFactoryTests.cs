using ArchUnitSharp.Common.Extraction;
using ArchUnitSharp.Files;

namespace ArchUnitSharp.Testing.Tests;

public class ResultFactoryTests
{
    [Fact]
    public void An_empty_violation_list_is_a_pass_with_the_pass_line()
    {
        CheckResult result = ResultFactory.Create(Array.Empty<Violation>());

        Assert.True(result.Passed);
        Assert.Equal(ResultFactory.PassLine, result.Message);
    }

    [Fact]
    public void A_single_violation_is_a_fail_with_its_rendered_message()
    {
        var violations = new Violation[] { new FileViolation("src/App/Program.cs") };

        CheckResult result = ResultFactory.Create(violations);

        Assert.False(result.Passed);
        Assert.Equal("File 'src/App/Program.cs' violates the rule.", result.Message);
    }

    [Fact]
    public void Several_violations_are_joined_with_a_newline_in_the_given_order()
    {
        var violations = new Violation[]
        {
            new DependencyViolation("src/App/Program.cs", "src/Models/Car.cs"),
            new CycleViolation(new[] { "src/A.cs", "src/B.cs", "src/A.cs" }),
        };

        CheckResult result = ResultFactory.Create(violations);

        Assert.False(result.Passed);
        Assert.Equal(
            "File 'src/App/Program.cs' must not depend on 'src/Models/Car.cs'.\n"
            + "Cycle: src/A.cs → src/B.cs → src/A.cs",
            result.Message);
    }

    [Fact]
    public void A_null_violation_list_is_rejected()
    {
        Assert.Throws<ArgumentNullException>(() => ResultFactory.Create(null!));
    }
}
