namespace ArchUnitSharp.Testing.Xunit;

using ArchUnitSharp.Common.Extraction;
using ArchUnitSharp.Testing;

/// <summary>
/// The xUnit-native assert helper: <c>assert passes(rule)</c> and <c>assert fails(rule)</c> for a
/// consumer running under xUnit. It is the path users should actually reach for, so a rule reads as a
/// native xUnit assertion: a rule terminal can be asserted directly through the
/// <see cref="RuleExtensions.AssertPasses"/> / <see cref="RuleExtensions.AssertFails"/> extensions,
/// which delegate here.
/// </summary>
/// <remarks>
/// <para>
/// There is no rule logic in this adapter. It does exactly three things: calls
/// <see cref="ICheckable.Check(CheckOptions?)"/> on the rule, maps the resulting violations through
/// the shared <see cref="ResultFactory"/>, and translates the shaped outcome to xUnit's own result
/// shape — <c>Xunit.Assert.True(result.Passed, result.Message)</c> for <see cref="Passes"/> and
/// <c>Xunit.Assert.False</c> for <see cref="Fails"/>, so a failure surfaces as xUnit's native
/// <c>TrueException</c> / <c>FalseException</c> with the rule's report message. The negation idiom is
/// xUnit's: the method name carries the mood, exactly as <c>Assert.True</c> / <c>Assert.False</c> do —
/// there is no chained negation, and the rule's own <c>should not</c> mood is asserted with
/// <see cref="Passes"/> like any other rule.
/// </para>
/// <para>
/// Zero setup: importing this package is all that is needed. A module initializer in
/// <see cref="XunitAdapter"/> silently detects whether this process is actually running under xUnit —
/// the <c>xunit.runner.*</c> / <c>xunit.execution.*</c> assemblies are present only during a real xUnit
/// run — and records the outcome. Under xUnit the assertions above are native. When the same package is
/// referenced from a run that is not xUnit (NUnit or MSTest), the methods silently fall back to the
/// framework-agnostic <see cref="RuleAssert"/>, which is what covers those frameworks; either way an
/// empty-test guard runs inside the rule's check and <see cref="TechnicalError"/> /
/// <see cref="UserError"/> propagate as the errors they are.
/// </para>
/// <para>
/// This type is stateless and safe for concurrent use.
/// </para>
/// </remarks>
public static class XunitAssert
{
    /// <summary>
    /// Asserts that <paramref name="rule"/> passes: under xUnit, translates the shaped outcome through
    /// <c>Xunit.Assert.True</c>, so a failure is xUnit's native <c>TrueException</c> carrying the rule's
    /// report message. Outside an xUnit run, falls back to <see cref="RuleAssert.Passes(ICheckable, CheckOptions?)"/>.
    /// </summary>
    /// <param name="rule">The rule to assert on. Must not be <see langword="null"/>.</param>
    /// <param name="options">The options to check with; <see langword="null"/> means the defaults in <see cref="CheckOptions"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="rule"/> is <see langword="null"/>.</exception>
    /// <exception cref="global::Xunit.Sdk.XunitException">The rule found violations, reported with the shaped message.</exception>
    /// <exception cref="AssertionFailedException">The rule found violations while running outside xUnit.</exception>
    /// <exception cref="Error">The rule's check failed in a way that is not a rule outcome; propagated unchanged.</exception>
    public static void Passes(ICheckable rule, CheckOptions? options = null) =>
        PassesCore(rule, options, XunitAdapter.Native);

    /// <summary>
    /// Asserts that <paramref name="rule"/> fails: under xUnit, translates the shaped outcome through
    /// <c>Xunit.Assert.False</c>, so a rule that unexpectedly passed is xUnit's native
    /// <c>FalseException</c>. Outside an xUnit run, falls back to
    /// <see cref="RuleAssert.Fails(ICheckable, CheckOptions?)"/>.
    /// </summary>
    /// <param name="rule">The rule to assert on. Must not be <see langword="null"/>.</param>
    /// <param name="options">The options to check with; <see langword="null"/> means the defaults in <see cref="CheckOptions"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="rule"/> is <see langword="null"/>.</exception>
    /// <exception cref="global::Xunit.Sdk.XunitException">The rule passed, reported with the shaped message.</exception>
    /// <exception cref="AssertionFailedException">The rule passed while running outside xUnit.</exception>
    /// <exception cref="Error">The rule's check failed in a way that is not a rule outcome; propagated unchanged.</exception>
    public static void Fails(ICheckable rule, CheckOptions? options = null) =>
        FailsCore(rule, options, XunitAdapter.Native);

    /// <summary>
    /// The shared assertion with the run mode made explicit, so both branches are testable: the native
    /// branch (<paramref name="native"/> is <see langword="true"/>) translates through xUnit's own
    /// assertions, and the fallback branch delegates to the framework-agnostic helper. Internal: the
    /// public methods pass <see cref="XunitAdapter.Native"/>.
    /// </summary>
    internal static void PassesCore(ICheckable rule, CheckOptions? options, bool native)
    {
        ArgumentNullException.ThrowIfNull(rule);

        if (native)
        {
            CheckResult result = ResultFactory.Create(rule.Check(options));
            global::Xunit.Assert.True(result.Passed, result.Message);
        }
        else
        {
            RuleAssert.Passes(rule, options);
        }
    }

    /// <summary>
    /// The shared assertion with the run mode made explicit, so both branches are testable. Internal:
    /// the public methods pass <see cref="XunitAdapter.Native"/>.
    /// </summary>
    internal static void FailsCore(ICheckable rule, CheckOptions? options, bool native)
    {
        ArgumentNullException.ThrowIfNull(rule);

        if (native)
        {
            CheckResult result = ResultFactory.Create(rule.Check(options));
            global::Xunit.Assert.False(result.Passed, result.Message);
        }
        else
        {
            RuleAssert.Fails(rule, options);
        }
    }
}
