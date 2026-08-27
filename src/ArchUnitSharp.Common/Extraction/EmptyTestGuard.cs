namespace ArchUnitSharp.Common.Extraction;

/// <summary>
/// The shared empty-test guard: the single decision point every terminal reaches when its rule matched
/// nothing. A rule that matched nothing is a failure rather than a pass — a selector that matches zero
/// files is almost always a typo — so the guard reports one <see cref="EmptyTestViolation"/> naming
/// the rule, unless the consumer opted out with <see cref="CheckOptions.AllowEmptyTests"/>, in which
/// case it reports nothing.
/// </summary>
/// <remarks>
/// <para>
/// The guard knows nothing about scopes, moods or selectors: a terminal decides what "matched nothing"
/// means for its rule and renders the rule description; the guard decides the consequence. Every
/// terminal in every domain module routes an empty rule through this one type, so no rule can silently
/// pass on zero matches and no module can hand-roll a second, weaker guard.
/// </para>
/// <para>
/// This type is stateless and safe for concurrent use. The violation lists it returns are fresh
/// copies on every call; when empty tests are allowed the shared empty array is returned, which is
/// safe to share because it is immutable.
/// </para>
/// </remarks>
public static class EmptyTestGuard
{
    /// <summary>
    /// The consequence of a rule that matched nothing: one <see cref="EmptyTestViolation"/> carrying
    /// <paramref name="ruleDescription"/> when <see cref="CheckOptions.AllowEmptyTests"/> is not set,
    /// otherwise an empty list — a pass. The violation list is a fresh copy on every call; the empty
    /// list is the shared empty array.
    /// </summary>
    /// <param name="ruleDescription">The rule that matched nothing, in the form a report would show. Must not be <see langword="null"/> or empty unless <see cref="CheckOptions.AllowEmptyTests"/> is set, in which case it is not inspected.</param>
    /// <param name="options">The options to check with; <see langword="null"/> means the defaults in <see cref="CheckOptions"/>.</param>
    /// <returns>An <see cref="EmptyTestViolation"/> naming the rule, or an empty list when empty tests are allowed.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="ruleDescription"/> is <see langword="null"/> and <see cref="CheckOptions.AllowEmptyTests"/> is not set.</exception>
    /// <exception cref="ArgumentException"><paramref name="ruleDescription"/> is empty and <see cref="CheckOptions.AllowEmptyTests"/> is not set.</exception>
    public static IReadOnlyList<Violation> Guard(string ruleDescription, CheckOptions? options)
    {
        if (options?.AllowEmptyTests == true)
        {
            return Array.Empty<Violation>();
        }

        return new Violation[] { new EmptyTestViolation(ruleDescription) };
    }
}
