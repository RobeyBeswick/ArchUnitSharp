namespace ArchUnitSharp.Files.Assertion;

using ArchUnitSharp.Common.Extraction;

/// <summary>
/// The files module's shared assertion: the one place a files rule's outcome is computed. The mood of
/// a rule arrives as the <c>negate</c> boolean — there is no separate code path for
/// <c>should not</c> — and the empty-test guard runs for every assertion, so every terminal that
/// calls in here reaches it. The <c>should have no cycles</c> predicate, which the public surface
/// exposes only in the positive mood, arrives without a mood flag.
/// </summary>
/// <remarks>
/// <para>
/// An assertion checks the <see cref="Files.Select"/> result of the rule's selection. A selection
/// that matched nothing is a violation (<see cref="EmptyTestViolation"/>) unless
/// <see cref="CheckOptions.AllowEmptyTests"/> is set, which is the guard every terminal in the
/// library reaches. A non-empty selection passes the positive mood and, for the negated mood, yields
/// one <see cref="FileViolation"/> per selected file; a selection whose dependencies form a cycle
/// yields one <see cref="CycleViolation"/> per cycle.
/// </para>
/// <para>
/// This type is stateless and safe for concurrent use. The lists it returns are fresh copies.
/// </para>
/// </remarks>
internal static class FilesAssertion
{
    /// <summary>
    /// Checks a <c>should exist</c> / <c>should not exist</c> rule over <paramref name="files"/>.
    /// </summary>
    /// <param name="files">The selection the rule asserts over. Must not be <see langword="null"/>.</param>
    /// <param name="negate">
    /// <see langword="false"/> for the positive mood, <see langword="true"/> for the negated mood.
    /// </param>
    /// <param name="options">The options to check with; <see langword="null"/> means the defaults in <see cref="CheckOptions"/>.</param>
    /// <returns>The violations found; empty when the rule passed.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="files"/> is <see langword="null"/>.</exception>
    public static IReadOnlyList<Violation> Exist(Files files, bool negate, CheckOptions? options)
    {
        ArgumentNullException.ThrowIfNull(files);

        IReadOnlyList<string> selected = files.Select();

        if (selected.Count == 0)
        {
            if (options?.AllowEmptyTests == true)
            {
                return new Violation[0];
            }

            string rule = $"{files.DescribeScope()} should{(negate ? " not" : string.Empty)} exist";
            return new Violation[] { new EmptyTestViolation(rule) };
        }

        if (!negate)
        {
            return new Violation[0];
        }

        return selected
            .Select(static file => (Violation)new FileViolation(file))
            .ToArray();
    }

    /// <summary>
    /// Checks a <c>should have no cycles</c> rule over <paramref name="files"/>: the projected
    /// dependency graph of the selected files must be acyclic. Each cycle the selection forms is
    /// reported as one <see cref="CycleViolation"/>. This predicate exists only in the positive mood,
    /// so no mood flag is threaded.
    /// </summary>
    /// <param name="files">The selection the rule asserts over. Must not be <see langword="null"/>.</param>
    /// <param name="options">The options to check with; <see langword="null"/> means the defaults in <see cref="CheckOptions"/>.</param>
    /// <returns>The violations found; empty when the rule passed.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="files"/> is <see langword="null"/>.</exception>
    public static IReadOnlyList<Violation> Cycles(Files files, CheckOptions? options)
    {
        ArgumentNullException.ThrowIfNull(files);

        IReadOnlyList<string> selected = files.Select();

        if (selected.Count == 0)
        {
            if (options?.AllowEmptyTests == true)
            {
                return new Violation[0];
            }

            string rule = $"{files.DescribeScope()} should have no cycles";
            return new Violation[] { new EmptyTestViolation(rule) };
        }

        return files.Cycles()
            .Select(static cycle => (Violation)new CycleViolation(cycle))
            .ToArray();
    }
}
