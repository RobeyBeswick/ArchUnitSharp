namespace ArchUnitSharp.Testing.Xunit;

using ArchUnitSharp.Common.Extraction;

/// <summary>
/// The extensions that make a rule terminal read as a native xUnit assertion at the end of the fluent
/// chain: <c>rule.AssertPasses()</c> asserts the rule passes and <c>rule.AssertFails()</c> asserts it
/// fails, both through <see cref="XunitAssert"/>. Nothing here is rule logic; the extensions only
/// forward to the adapter.
/// </summary>
/// <remarks>
/// <para>
/// A rule chain ends in an <see cref="ICheckable"/> terminal — the value <c>should ...</c> predicates
/// return — and these extensions are the assertion you call on it, so the whole chain reads as one
/// xUnit test. A negated rule (the <c>should not</c> mood) is asserted with
/// <see cref="AssertPasses"/> exactly like any other, respecting xUnit's idiom that the method name —
/// not a chained modifier — carries the assertion's mood. The extensions work under any framework:
/// the adapter they forward to is native under xUnit and silently falls back to the framework-agnostic
/// helper otherwise.
/// </para>
/// <para>
/// This type is stateless and safe for concurrent use.
/// </para>
/// </remarks>
public static class RuleExtensions
{
    /// <summary>
    /// Asserts that <paramref name="rule"/> passes, translating the outcome through
    /// <see cref="XunitAssert.Passes(ICheckable, CheckOptions?)"/>.
    /// </summary>
    /// <param name="rule">The rule's terminal. Must not be <see langword="null"/>.</param>
    /// <param name="options">The options to check with; <see langword="null"/> means the defaults in <see cref="CheckOptions"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="rule"/> is <see langword="null"/>.</exception>
    /// <exception cref="global::Xunit.Sdk.XunitException">The rule found violations, reported with the shaped message.</exception>
    /// <exception cref="AssertionFailedException">The rule found violations while running outside xUnit.</exception>
    /// <exception cref="Error">The rule's check failed in a way that is not a rule outcome; propagated unchanged.</exception>
    public static void AssertPasses(this ICheckable rule, CheckOptions? options = null) =>
        XunitAssert.Passes(rule, options);

    /// <summary>
    /// Asserts that <paramref name="rule"/> fails, translating the outcome through
    /// <see cref="XunitAssert.Fails(ICheckable, CheckOptions?)"/>.
    /// </summary>
    /// <param name="rule">The rule's terminal. Must not be <see langword="null"/>.</param>
    /// <param name="options">The options to check with; <see langword="null"/> means the defaults in <see cref="CheckOptions"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="rule"/> is <see langword="null"/>.</exception>
    /// <exception cref="global::Xunit.Sdk.XunitException">The rule passed, reported with the shaped message.</exception>
    /// <exception cref="AssertionFailedException">The rule passed while running outside xUnit.</exception>
    /// <exception cref="Error">The rule's check failed in a way that is not a rule outcome; propagated unchanged.</exception>
    public static void AssertFails(this ICheckable rule, CheckOptions? options = null) =>
        XunitAssert.Fails(rule, options);
}
