namespace ArchUnitSharp.Testing;

using ArchUnitSharp.Common.Extraction;

/// <summary>
/// The framework-agnostic assert helper: <c>assert passes(rule)</c> and <c>assert fails(rule)</c> for
/// a consumer with no adapter for its test framework. <see cref="Passes"/> and <see cref="Fails"/>
/// check a rule, shape the violations it found with <see cref="ResultFactory"/> and, when the asserted
/// outcome does not hold, raise an <see cref="AssertionFailedException"/> — the assertion failure every
/// test framework reports as a failed test.
/// </summary>
/// <remarks>
/// <para>
/// This is the documented fallback for asserting a rule: it calls
/// <see cref="ICheckable.Check(CheckOptions?)"/> on the rule, hands the resulting violations to
/// <see cref="ResultFactory"/> and throws when the shaped result is a fail. A passing rule returns
/// normally. Because every test framework treats an exception thrown from a test as a failure, these
/// two methods are the whole integration — no adapter, package or configuration. The rule's own
/// semantics are preserved unchanged: a rule that matched nothing is a failure unless
/// <see cref="CheckOptions.AllowEmptyTests"/> is set (the empty-test guard runs inside the rule's
/// check), and failures that are not rule outcomes — <see cref="TechnicalError"/> and
/// <see cref="UserError"/> — propagate as the errors they are rather than being re-shaped into an
/// assertion failure.
/// </para>
/// <para>
/// The two methods are the agnostic twin of a framework's <c>True</c> / <c>False</c> assertion pair:
/// <see cref="Passes"/> asserts the rule holds, <see cref="Fails"/> asserts it does not. There is no
/// chained negation — the method name carries the mood, and a negated rule (the <c>should not</c>
/// mood) is asserted with <see cref="Passes"/> like any other.
/// </para>
/// <para>
/// The helper is named <c>RuleAssert</c>, not <c>Assert</c>, so it never shadows a test framework's
/// own <c>Assert</c> type: a consumer can import this namespace and, say, <c>Xunit</c> in the same
/// file and use both assertion surfaces unqualified. That is what makes the helper work with every
/// test framework rather than colliding with the one already in use.
/// </para>
/// <para>
/// This type is stateless and safe for concurrent use.
/// </para>
/// </remarks>
public static class RuleAssert
{
    /// <summary>
    /// Asserts that <paramref name="rule"/> passes: checks it, shapes the violations it found and,
    /// when the rule failed, raises <see cref="AssertionFailedException"/> carrying the shaped result.
    /// </summary>
    /// <param name="rule">The rule to assert on. Must not be <see langword="null"/>.</param>
    /// <param name="options">The options to check with; <see langword="null"/> means the defaults in <see cref="CheckOptions"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="rule"/> is <see langword="null"/>.</exception>
    /// <exception cref="AssertionFailedException">The rule found violations.</exception>
    /// <exception cref="Error">The rule's check failed in a way that is not a rule outcome, such as a project that cannot be located; propagated unchanged.</exception>
    public static void Passes(ICheckable rule, CheckOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(rule);

        CheckResult result = ResultFactory.Create(rule.Check(options));
        if (!result.Passed)
        {
            throw new AssertionFailedException(result);
        }
    }

    /// <summary>
    /// Asserts that <paramref name="rule"/> fails: checks it, shapes the violations it found and,
    /// when the rule passed, raises <see cref="AssertionFailedException"/>.
    /// </summary>
    /// <param name="rule">The rule to assert on. Must not be <see langword="null"/>.</param>
    /// <param name="options">The options to check with; <see langword="null"/> means the defaults in <see cref="CheckOptions"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="rule"/> is <see langword="null"/>.</exception>
    /// <exception cref="AssertionFailedException">The rule passed, but the assertion expected it to fail.</exception>
    /// <exception cref="Error">The rule's check failed in a way that is not a rule outcome, such as a project that cannot be located; propagated unchanged.</exception>
    public static void Fails(ICheckable rule, CheckOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(rule);

        CheckResult result = ResultFactory.Create(rule.Check(options));
        if (result.Passed)
        {
            throw new AssertionFailedException(new CheckResult(
                Passed: false,
                Message: "The rule passed, but the assertion expected it to fail."));
        }
    }
}
