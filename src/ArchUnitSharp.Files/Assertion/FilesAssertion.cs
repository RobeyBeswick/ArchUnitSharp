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
/// library reaches. A non-empty selection passes the positive mood of the existence rule and, for the
/// negated mood, yields one <see cref="FileViolation"/> per selected file. The naming and location
/// predicates — <c>should (not) have name</c>, <c>should (not) be in folder</c>,
/// <c>should (not) be in path</c> — match each selected file's name, folder or path against the
/// rule's glob and yield one <see cref="FileViolation"/> per file that violates the mood, in either
/// mood. A selection whose dependencies form a cycle yields one <see cref="CycleViolation"/> per
/// cycle.
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

    /// <summary>
    /// Checks a <c>should have name</c> / <c>should not have name</c> rule over <paramref name="files"/>:
    /// each selected file's name is matched against <paramref name="filter"/>'s pattern, and a file
    /// whose match result contradicts the mood is reported as one <see cref="FileViolation"/>.
    /// </summary>
    /// <param name="files">The selection the rule asserts over. Must not be <see langword="null"/>.</param>
    /// <param name="filter">
    /// The rule's glob compiled to a name filter, matched against each selected file's name. Must not
    /// be <see langword="null"/>.
    /// </param>
    /// <param name="negate">
    /// <see langword="false"/> for the positive mood, <see langword="true"/> for the negated mood.
    /// </param>
    /// <param name="options">The options to check with; <see langword="null"/> means the defaults in <see cref="CheckOptions"/>.</param>
    /// <returns>The violations found; empty when the rule passed.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="files"/> or <paramref name="filter"/> is <see langword="null"/>.</exception>
    public static IReadOnlyList<Violation> HaveName(Files files, Filter filter, bool negate, CheckOptions? options) =>
        Match(files, filter, "have name", negate, options);

    /// <summary>
    /// Checks a <c>should be in folder</c> / <c>should not be in folder</c> rule over <paramref name="files"/>:
    /// each selected file's folder is matched against <paramref name="filter"/>'s pattern, and a file
    /// whose match result contradicts the mood is reported as one <see cref="FileViolation"/>.
    /// </summary>
    /// <param name="files">The selection the rule asserts over. Must not be <see langword="null"/>.</param>
    /// <param name="filter">
    /// The rule's glob compiled to a folder filter, matched against each selected file's folder. Must
    /// not be <see langword="null"/>.
    /// </param>
    /// <param name="negate">
    /// <see langword="false"/> for the positive mood, <see langword="true"/> for the negated mood.
    /// </param>
    /// <param name="options">The options to check with; <see langword="null"/> means the defaults in <see cref="CheckOptions"/>.</param>
    /// <returns>The violations found; empty when the rule passed.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="files"/> or <paramref name="filter"/> is <see langword="null"/>.</exception>
    public static IReadOnlyList<Violation> BeInFolder(Files files, Filter filter, bool negate, CheckOptions? options) =>
        Match(files, filter, "be in folder", negate, options);

    /// <summary>
    /// Checks a <c>should be in path</c> / <c>should not be in path</c> rule over <paramref name="files"/>:
    /// each selected file's whole path is matched against <paramref name="filter"/>'s pattern, and a
    /// file whose match result contradicts the mood is reported as one <see cref="FileViolation"/>.
    /// </summary>
    /// <param name="files">The selection the rule asserts over. Must not be <see langword="null"/>.</param>
    /// <param name="filter">
    /// The rule's glob compiled to a path filter, matched against each selected file's whole path.
    /// Must not be <see langword="null"/>.
    /// </param>
    /// <param name="negate">
    /// <see langword="false"/> for the positive mood, <see langword="true"/> for the negated mood.
    /// </param>
    /// <param name="options">The options to check with; <see langword="null"/> means the defaults in <see cref="CheckOptions"/>.</param>
    /// <returns>The violations found; empty when the rule passed.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="files"/> or <paramref name="filter"/> is <see langword="null"/>.</exception>
    public static IReadOnlyList<Violation> BeInPath(Files files, Filter filter, bool negate, CheckOptions? options) =>
        Match(files, filter, "be in path", negate, options);

    private static IReadOnlyList<Violation> Match(
        Files files,
        Filter filter,
        string predicatePhrase,
        bool negate,
        CheckOptions? options)
    {
        ArgumentNullException.ThrowIfNull(files);
        ArgumentNullException.ThrowIfNull(filter);

        IReadOnlyList<string> selected = files.Select();

        if (selected.Count == 0)
        {
            if (options?.AllowEmptyTests == true)
            {
                return new Violation[0];
            }

            string rule =
                $"{files.DescribeScope()} should{(negate ? " not" : string.Empty)} {predicatePhrase} '{filter.Pattern.Glob}'";
            return new Violation[] { new EmptyTestViolation(rule) };
        }

        return selected
            .Where(file => filter.Matches(file) == negate)
            .Select(static file => (Violation)new FileViolation(file))
            .ToArray();
    }
}
