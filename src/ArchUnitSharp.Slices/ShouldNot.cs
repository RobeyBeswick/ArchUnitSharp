namespace ArchUnitSharp.Slices;

using ArchUnitSharp.Common.Extraction;

/// <summary>
/// The negated mood of a slices rule chain: <c>should not</c>. Built from <see cref="Slices.ShouldNot"/>;
/// its predicate method completes the rule and returns a new <see cref="Slices"/> with the rule added,
/// which is the terminal checked with <see cref="ICheckable.Check"/>.
/// </summary>
/// <remarks>
/// <para>
/// This type is the mood, nothing else: it carries no rule logic. A predicate method forwards the
/// policy and its <see langword="true"/> mood flag to the shared assertion in
/// <see cref="Assertion.SlicesAssertion"/>, which is the single place a slices rule's outcome is
/// computed — the negation is the flag, not a separate code path. The positive twin is
/// <see cref="Should"/>; there is no third mood.
/// </para>
/// <para>
/// This type is immutable and safe for concurrent use. Completing a rule never mutates the policy it
/// was built from, so a <see cref="ShouldNot"/> value can be stored and reused.
/// </para>
/// </remarks>
public sealed class ShouldNot
{
    private readonly Slices _slices;

    /// <summary>
    /// Creates the negated mood over <paramref name="slices"/>. Callers obtain a <see cref="ShouldNot"/>
    /// from <see cref="Slices.ShouldNot"/> rather than constructing one.
    /// </summary>
    /// <param name="slices">The policy the rule asserts over.</param>
    internal ShouldNot(Slices slices) => _slices = slices;

    /// <summary>
    /// <c>should not contain dependency(from, to)</c>: no slice may contain a dependency from a sliced
    /// file whose whole path matches <paramref name="fromGlob"/> to a file whose whole path matches
    /// <paramref name="toGlob"/>. Each forbidden dependency is reported as one
    /// <see cref="ForbiddenDependencyViolation"/> naming the slice that contains it, and the empty-test
    /// guard reports a policy whose slicing matched nothing or whose <c>from</c> or <c>to</c> glob
    /// matched no file.
    /// </summary>
    /// <param name="fromGlob">The glob the importing file of a forbidden dependency must match. Must not be <see langword="null"/> or empty.</param>
    /// <param name="toGlob">The glob the imported file of a forbidden dependency must match. Must not be <see langword="null"/> or empty.</param>
    /// <returns>A new policy with the rule asserted; checked with <see cref="ICheckable.Check"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="fromGlob"/> or <paramref name="toGlob"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="fromGlob"/> or <paramref name="toGlob"/> is empty.</exception>
    public Slices ContainDependency(string fromGlob, string toGlob) =>
        _slices.AddRule(new SliceRule(fromGlob, toGlob, negate: true));
}
