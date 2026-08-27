namespace ArchUnitSharp.Testing;

/// <summary>
/// The assertion failure <see cref="RuleAssert.Passes"/> raises when a rule found violations: the
/// shaped outcome of the failed check. Because every test framework treats an exception thrown from a
/// test as a failure, this type is the framework-agnostic signal — its message carries the report
/// text the framework shows, and <see cref="Result"/> carries the shaped outcome that report was
/// built from.
/// </summary>
/// <remarks>
/// <para>
/// This is a testing-side signal, deliberately not one of the library's
/// <see cref="Common.Extraction.TechnicalError"/> / <see cref="Common.Extraction.UserError"/>
/// exceptions: those signal failures that are not rule outcomes, while this one is the deliberate
/// conversion of a rule outcome — a list of violations — into the assertion failure the
/// <c>assert passes(rule)</c> contract calls for. It carries the shaped <see cref="CheckResult"/> as
/// data, so a consumer catching it can re-render the outcome — for example colour it with
/// <see cref="Colouriser"/> — without re-running the rule.
/// </para>
/// <para>
/// This type is sealed and immutable, and safe for concurrent use.
/// </para>
/// </remarks>
public sealed class AssertionFailedException : Exception
{
    /// <summary>
    /// The shaped outcome of the check that failed: the verdict — <see cref="CheckResult.Passed"/> is
    /// always <see langword="false"/> for a failure this exception represents — and the report message
    /// that goes with it. The message is also this exception's <see cref="Exception.Message"/>.
    /// </summary>
    public CheckResult Result { get; }

    /// <summary>
    /// Creates the assertion failure for a failed check.
    /// </summary>
    /// <param name="result">The shaped outcome of the failed check; must not be <see langword="null"/> and must not have passed.</param>
    /// <exception cref="ArgumentNullException"><paramref name="result"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="result"/> passed, which is no failure.</exception>
    public AssertionFailedException(CheckResult result)
        : base(Require(result).Message)
    {
        Result = result;
    }

    private static CheckResult Require(CheckResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        if (result.Passed)
        {
            throw new ArgumentException(
                "An assertion failure must carry a failed result.",
                nameof(result));
        }

        return result;
    }
}
