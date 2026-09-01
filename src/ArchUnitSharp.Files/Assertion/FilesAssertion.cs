namespace ArchUnitSharp.Files.Assertion;

using ArchUnitSharp.Common.Extraction;
using ArchUnitSharp.Files.Projection;

/// <summary>
/// The files module's shared assertion: the one place a files rule's outcome is computed. The mood of
/// a rule arrives as the <c>negate</c> boolean — there is no separate code path for
/// <c>should not</c> — and every assertion routes an empty selection through the shared
/// <see cref="EmptyTestGuard"/>, so every terminal that calls in here reaches the guard. The
/// <c>should have no cycles</c> predicate, which the public surface exposes only in the positive mood,
/// arrives without a mood flag.
/// </summary>
/// <remarks>
/// <para>
/// An assertion checks the <see cref="Files.Select"/> result of the rule's selection. A selection
/// that matched nothing is a violation (<see cref="EmptyTestViolation"/>) unless
/// <see cref="CheckOptions.AllowEmptyTests"/> is set, which is the shared
/// <see cref="EmptyTestGuard"/> every terminal in the library reaches. A non-empty selection passes
/// the positive mood of the existence rule and, for the negated mood, yields one
/// <see cref="FileViolation"/> per selected file. The naming and location
/// predicates — <c>should (not) have name</c>, <c>should (not) be in folder</c>,
/// <c>should (not) be in path</c> — match each selected file's name, folder or path against the
/// rule's glob and yield one <see cref="FileViolation"/> per file that violates the mood, in either
/// mood. The depend-on predicate — <c>should (not) depend on files</c> — matches each selected
/// file's dependencies against the object's selectors and yields one <see cref="FileViolation"/> per
/// selected file that depends on none of them (positive mood) or one
/// <see cref="DependencyViolation"/> per offending dependency (negated mood), and the
/// <see cref="EmptyTestGuard"/> reports a rule whose selection or object matched nothing. The
/// external-modules predicate —
/// <c>should (not) depend on external modules</c> — matches each selected file's external
/// dependencies against the object's selectors the same way: one <see cref="FileViolation"/> per
/// selected file that depends on no matching module (positive mood), or one
/// <see cref="DependencyViolation"/> per offending dependency (negated mood), and the
/// <see cref="EmptyTestGuard"/> reports a rule whose selection or object matched nothing. The
/// adhere-to predicate — <c>should (not) adhere
/// to</c> — hands each selected file's <see cref="FileDetail"/> to the rule's custom predicate and
/// yields one <see cref="AdhereToViolation"/> per file whose verdict contradicts the mood, and the
/// <see cref="EmptyTestGuard"/> reports a selection that matched nothing. A selection whose
/// dependencies form a cycle
/// yields one <see cref="CycleViolation"/> per cycle.
/// </para>
/// <para>
/// This type is stateless and safe for concurrent use. The lists it returns are fresh copies.
/// </para>
/// <para>
/// Each assertion is handed the check's <see cref="CheckLogger"/> by the terminal that calls it and
/// emits the fixed logging vocabulary: the rule being checked starts on entry, the number of files
/// the scope selected is progress, and every violation the rule reports is logged as it is produced.
/// The logger only buffers lines — the assertion never touches the filesystem — and the terminal's
/// wrapper records the check's end and flushes the log after the assertion returns.
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
    /// <param name="logger">The check's logger, created by the terminal; <see langword="null"/> means the check logs nothing.</param>
    /// <returns>The violations found; empty when the rule passed.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="files"/> is <see langword="null"/>.</exception>
    public static IReadOnlyList<Violation> Exist(
        Files files,
        bool negate,
        CheckOptions? options,
        CheckLogger? logger = null)
    {
        ArgumentNullException.ThrowIfNull(files);
        logger ??= CheckLogger.Create(null);

        string rule = $"{files.DescribeScope()} should{(negate ? " not" : string.Empty)} exist";
        logger.StartCheck(rule);

        IReadOnlyList<string> selected = files.Select();
        logger.Progress($"selected {selected.Count} file(s)");

        if (selected.Count == 0)
        {
            IReadOnlyList<Violation> empty = EmptyTestGuard.Guard(rule, options);
            logger.Violations(empty);
            return empty;
        }

        if (!negate)
        {
            return new Violation[0];
        }

        Violation[] violations = selected
            .Select(static file => (Violation)new FileViolation(file))
            .ToArray();
        logger.Violations(violations);
        return violations;
    }

    /// <summary>
    /// Checks a <c>should have no cycles</c> rule over <paramref name="files"/>: the projected
    /// dependency graph of the selected files must be acyclic. Each cycle the selection forms is
    /// reported as one <see cref="CycleViolation"/>. This predicate exists only in the positive mood,
    /// so no mood flag is threaded.
    /// </summary>
    /// <param name="files">The selection the rule asserts over. Must not be <see langword="null"/>.</param>
    /// <param name="options">The options to check with; <see langword="null"/> means the defaults in <see cref="CheckOptions"/>.</param>
    /// <param name="logger">The check's logger, created by the terminal; <see langword="null"/> means the check logs nothing.</param>
    /// <returns>The violations found; empty when the rule passed.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="files"/> is <see langword="null"/>.</exception>
    public static IReadOnlyList<Violation> Cycles(Files files, CheckOptions? options, CheckLogger? logger = null)
    {
        ArgumentNullException.ThrowIfNull(files);
        logger ??= CheckLogger.Create(null);

        string rule = $"{files.DescribeScope()} should have no cycles";
        logger.StartCheck(rule);

        IReadOnlyList<string> selected = files.Select();
        logger.Progress($"selected {selected.Count} file(s)");

        if (selected.Count == 0)
        {
            IReadOnlyList<Violation> empty = EmptyTestGuard.Guard(rule, options);
            logger.Violations(empty);
            return empty;
        }

        IReadOnlyList<IReadOnlyList<string>> cycles = files.Cycles();
        logger.Progress($"projected {cycles.Count} cycle(s)");

        Violation[] violations = cycles
            .Select(static cycle => (Violation)new CycleViolation(cycle))
            .ToArray();
        logger.Violations(violations);
        return violations;
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
    /// <param name="logger">The check's logger, created by the terminal; <see langword="null"/> means the check logs nothing.</param>
    /// <returns>The violations found; empty when the rule passed.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="files"/> or <paramref name="filter"/> is <see langword="null"/>.</exception>
    public static IReadOnlyList<Violation> HaveName(
        Files files,
        Filter filter,
        bool negate,
        CheckOptions? options,
        CheckLogger? logger = null) =>
        Match(files, filter, "have name", negate, options, logger);

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
    /// <param name="logger">The check's logger, created by the terminal; <see langword="null"/> means the check logs nothing.</param>
    /// <returns>The violations found; empty when the rule passed.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="files"/> or <paramref name="filter"/> is <see langword="null"/>.</exception>
    public static IReadOnlyList<Violation> BeInFolder(
        Files files,
        Filter filter,
        bool negate,
        CheckOptions? options,
        CheckLogger? logger = null) =>
        Match(files, filter, "be in folder", negate, options, logger);

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
    /// <param name="logger">The check's logger, created by the terminal; <see langword="null"/> means the check logs nothing.</param>
    /// <returns>The violations found; empty when the rule passed.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="files"/> or <paramref name="filter"/> is <see langword="null"/>.</exception>
    public static IReadOnlyList<Violation> BeInPath(
        Files files,
        Filter filter,
        bool negate,
        CheckOptions? options,
        CheckLogger? logger = null) =>
        Match(files, filter, "be in path", negate, options, logger);

    /// <summary>
    /// Checks a <c>should adhere to</c> / <c>should not adhere to</c> rule over <paramref name="files"/>:
    /// each selected file's detail — its path, name without extension, extension, directory, full
    /// source text and non-blank line count — is handed to the rule's custom predicate. With the
    /// positive mood a selected file the predicate rejects is reported as one
    /// <see cref="AdhereToViolation"/> carrying the rule's message; with the negated mood a selected
    /// file the predicate accepts is. The <see cref="EmptyTestGuard"/> reports a selection that matched
    /// nothing.
    /// </summary>
    /// <param name="files">The selection the rule asserts over. Must not be <see langword="null"/>.</param>
    /// <param name="predicate">The rule's custom predicate. Must not be <see langword="null"/>.</param>
    /// <param name="message">The rule's message, carried by every violation. Must not be <see langword="null"/>.</param>
    /// <param name="negate">
    /// <see langword="false"/> for the positive mood, <see langword="true"/> for the negated mood.
    /// </param>
    /// <param name="options">The options to check with; <see langword="null"/> means the defaults in <see cref="CheckOptions"/>.</param>
    /// <param name="logger">The check's logger, created by the terminal; <see langword="null"/> means the check logs nothing.</param>
    /// <returns>The violations found; empty when the rule passed.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="files"/>, <paramref name="predicate"/> or <paramref name="message"/> is <see langword="null"/>.</exception>
    /// <exception cref="UserError"><paramref name="files"/> was built without a source provider, so a selected file's source text is unavailable.</exception>
    public static IReadOnlyList<Violation> AdhereTo(
        Files files,
        Func<FileDetail, bool> predicate,
        string message,
        bool negate,
        CheckOptions? options,
        CheckLogger? logger = null)
    {
        ArgumentNullException.ThrowIfNull(files);
        ArgumentNullException.ThrowIfNull(predicate);
        ArgumentNullException.ThrowIfNull(message);
        logger ??= CheckLogger.Create(null);

        string rule =
            $"{files.DescribeScope()} should{(negate ? " not" : string.Empty)} adhere to '{message}'";
        logger.StartCheck(rule);

        IReadOnlyList<string> selected = files.Select();
        logger.Progress($"selected {selected.Count} file(s)");

        if (selected.Count == 0)
        {
            IReadOnlyList<Violation> empty = EmptyTestGuard.Guard(rule, options);
            logger.Violations(empty);
            return empty;
        }

        Violation[] violations = selected
            .Where(identifier => predicate(files.FileDetailOf(identifier)) == negate)
            .Select(identifier => (Violation)new AdhereToViolation(identifier, message))
            .ToArray();
        logger.Violations(violations);
        return violations;
    }

    /// <summary>
    /// Checks a <c>should depend on files</c> / <c>should not depend on files</c> rule: each selected
    /// file's dependencies are matched against the rule's object selectors. With the positive mood a
    /// selected file that depends on no file matching every object selector is reported as one
    /// <see cref="FileViolation"/>; with the negated mood each dependency on a file matching every
    /// object selector is reported as one <see cref="DependencyViolation"/>. The
    /// <see cref="EmptyTestGuard"/> reports a rule whose selection or object matched nothing.
    /// </summary>
    /// <param name="rule">The rule to check. Must not be <see langword="null"/>.</param>
    /// <param name="options">The options to check with; <see langword="null"/> means the defaults in <see cref="CheckOptions"/>.</param>
    /// <param name="logger">The check's logger, created by the terminal; <see langword="null"/> means the check logs nothing.</param>
    /// <returns>The violations found; empty when the rule passed.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="rule"/> is <see langword="null"/>.</exception>
    public static IReadOnlyList<Violation> DependOn(DependOn rule, CheckOptions? options, CheckLogger? logger = null)
    {
        ArgumentNullException.ThrowIfNull(rule);
        logger ??= CheckLogger.Create(null);

        Files files = rule.Subject;
        string description =
            $"{files.DescribeScope()} should{(rule.Negate ? " not" : string.Empty)} depend on {rule.DescribeObject()}";
        logger.StartCheck(description);

        IReadOnlyList<string> subject = files.Select();
        logger.Progress($"selected {subject.Count} file(s)");
        IReadOnlyList<string> objects = FilesProjection.Select(files.Graph, rule.ObjectFilters);
        logger.Progress($"object matched {objects.Count} file(s)");

        if (subject.Count == 0 || objects.Count == 0)
        {
            IReadOnlyList<Violation> empty = EmptyTestGuard.Guard(description, options);
            logger.Violations(empty);
            return empty;
        }

        IReadOnlyList<Edge> dependencies =
            FilesProjection.Dependencies(files.Graph, files.Filters, rule.ObjectFilters);
        logger.Progress($"projected {dependencies.Count} dependency edge(s)");

        if (rule.Negate)
        {
            Violation[] violations = dependencies
                .Select(static edge => (Violation)new DependencyViolation(edge.Source, edge.Target))
                .ToArray();
            logger.Violations(violations);
            return violations;
        }

        var satisfied = new HashSet<string>(
            dependencies.Select(static edge => edge.Source),
            StringComparer.Ordinal);
        Violation[] missing = subject
            .Where(file => !satisfied.Contains(file))
            .Select(static file => (Violation)new FileViolation(file))
            .ToArray();
        logger.Violations(missing);
        return missing;
    }

    /// <summary>
    /// Checks a <c>should depend on external modules</c> / <c>should not depend on external modules</c>
    /// rule: each selected file's external dependencies are matched against the rule's object
    /// selectors. With the positive mood a selected file that depends on no external module matching
    /// any object selector is reported as one <see cref="FileViolation"/>; with the negated mood each
    /// dependency on an external module matching any object selector is reported as one
    /// <see cref="DependencyViolation"/>. The <see cref="EmptyTestGuard"/> reports a rule whose selection
    /// or object matched nothing.
    /// </summary>
    /// <param name="rule">The rule to check. Must not be <see langword="null"/>.</param>
    /// <param name="options">The options to check with; <see langword="null"/> means the defaults in <see cref="CheckOptions"/>.</param>
    /// <param name="logger">The check's logger, created by the terminal; <see langword="null"/> means the check logs nothing.</param>
    /// <returns>The violations found; empty when the rule passed.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="rule"/> is <see langword="null"/>.</exception>
    public static IReadOnlyList<Violation> DependOnExternalModules(
        DependOnExternalModules rule,
        CheckOptions? options,
        CheckLogger? logger = null)
    {
        ArgumentNullException.ThrowIfNull(rule);
        logger ??= CheckLogger.Create(null);

        Files files = rule.Subject;
        string description =
            $"{files.DescribeScope()} should{(rule.Negate ? " not" : string.Empty)} depend on {rule.DescribeObject()}";
        logger.StartCheck(description);

        IReadOnlyList<string> subject = files.Select();
        logger.Progress($"selected {subject.Count} file(s)");
        IReadOnlyList<string> modules = FilesProjection.ExternalModules(files.Graph, rule.ObjectFilters);
        logger.Progress($"object matched {modules.Count} external module(s)");

        if (subject.Count == 0 || modules.Count == 0)
        {
            IReadOnlyList<Violation> empty = EmptyTestGuard.Guard(description, options);
            logger.Violations(empty);
            return empty;
        }

        IReadOnlyList<Edge> dependencies =
            FilesProjection.ExternalDependencies(files.Graph, files.Filters, rule.ObjectFilters);
        logger.Progress($"projected {dependencies.Count} dependency edge(s)");

        if (rule.Negate)
        {
            Violation[] violations = dependencies
                .Select(static edge => (Violation)new DependencyViolation(edge.Source, edge.Target))
                .ToArray();
            logger.Violations(violations);
            return violations;
        }

        var satisfied = new HashSet<string>(
            dependencies.Select(static edge => edge.Source),
            StringComparer.Ordinal);
        Violation[] missing = subject
            .Where(file => !satisfied.Contains(file))
            .Select(static file => (Violation)new FileViolation(file))
            .ToArray();
        logger.Violations(missing);
        return missing;
    }

    private static IReadOnlyList<Violation> Match(
        Files files,
        Filter filter,
        string predicatePhrase,
        bool negate,
        CheckOptions? options,
        CheckLogger? logger)
    {
        ArgumentNullException.ThrowIfNull(files);
        ArgumentNullException.ThrowIfNull(filter);
        logger ??= CheckLogger.Create(null);

        string rule =
            $"{files.DescribeScope()} should{(negate ? " not" : string.Empty)} {predicatePhrase} '{filter.Pattern.Glob}'";
        logger.StartCheck(rule);

        IReadOnlyList<string> selected = files.Select();
        logger.Progress($"selected {selected.Count} file(s)");

        if (selected.Count == 0)
        {
            IReadOnlyList<Violation> empty = EmptyTestGuard.Guard(rule, options);
            logger.Violations(empty);
            return empty;
        }

        Violation[] violations = selected
            .Where(file => filter.Matches(file) == negate)
            .Select(static file => (Violation)new FileViolation(file))
            .ToArray();
        logger.Violations(violations);
        return violations;
    }
}
