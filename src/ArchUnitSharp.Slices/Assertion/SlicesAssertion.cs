namespace ArchUnitSharp.Slices.Assertion;

using System.Text;
using ArchUnitSharp.Common.Extraction;
using ArchUnitSharp.Projection;
using ArchUnitSharp.Slices.Projection;

/// <summary>
/// The slices module's shared assertion: the one place a <c>contain dependency(from, to)</c> rule's and
/// an <c>adhere to diagram</c> rule's outcome is computed. The mood of a rule arrives as the
/// <c>negate</c> boolean — there is no separate code path for <c>should not</c> — and every rule routes
/// an empty selection through the shared <see cref="EmptyTestGuard"/>, so every terminal that calls in
/// here reaches the guard.
/// </summary>
/// <remarks>
/// <para>
/// A rule's subject is the whole slicing: the files the definitions assign to slices. A subject with
/// no sliced files is a violation (<see cref="EmptyTestViolation"/>) rather than a pass, unless
/// <see cref="CheckOptions.AllowEmptyTests"/> is set — a typo in a definition must not pass silently.
/// The contain-dependency predicate adds its own guard when the <c>from</c> filter matches no sliced
/// file or the <c>to</c> filter matches no file of the graph, and the adhere-to-diagram predicate adds
/// its own guard when the diagram declares no components and no arrows. A policy with no rules passes.
/// </para>
/// <para>
/// With the negated mood, each dependency from a sliced file matching <c>from</c> to a file matching
/// <c>to</c> is reported as one <see cref="ForbiddenDependencyViolation"/> naming the slice that
/// contains it and the two files. With the positive mood, each slice that contains no such dependency
/// is reported as one <see cref="MissingDependencyViolation"/>. The importing file must be sliced — a
/// dependency is contained in the slice of its importing file — while the imported file need not be,
/// because a dependency can leave the slicing. An external edge's target is not a file, and a self-edge
/// is not a dependency, so neither is ever counted.
/// </para>
/// <para>
/// The <c>adhere to diagram</c> predicate compares the slicing's projected slice-to-slice dependencies
/// against the diagram's allowed arrows: each dependency the diagram does not allow is reported as one
/// <see cref="DiagramAdherenceViolation"/> naming the two slices, one violation per slice pair. Its
/// modifiers ignore external dependencies (a dependency whose target lies outside the project) and
/// orphan endpoints (a dependency whose source or target the diagram does not declare as a component).
/// </para>
/// <para>
/// This type is stateless and safe for concurrent use. The lists it returns are fresh copies.
/// </para>
/// <para>
/// Each assertion is handed the check's <see cref="CheckLogger"/> by the terminal that calls it and
/// emits the fixed logging vocabulary: every rule is a <c>start check</c> naming the rule, the
/// dependency edges the slicing projects are progress, and every violation is logged as it is
/// produced. The logger only buffers lines — the assertion never touches the filesystem — and the
/// terminal's wrapper records the check's end and flushes the log after the assertion returns.
/// </para>
/// </remarks>
internal static class SlicesAssertion
{
    /// <summary>
    /// Checks every rule of a slices policy and returns the violations it found, in the order the
    /// rules were added. An empty list means the policy passed.
    /// </summary>
    /// <param name="slices">The policy to check. Must not be <see langword="null"/>.</param>
    /// <param name="options">The options to check with; <see langword="null"/> means the defaults in <see cref="CheckOptions"/>.</param>
    /// <param name="logger">The check's logger, created by the terminal; <see langword="null"/> means the check logs nothing.</param>
    /// <returns>The violations found; empty when the policy passed.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="slices"/> is <see langword="null"/>.</exception>
    public static IReadOnlyList<Violation> Check(
        Slices slices,
        CheckOptions? options,
        CheckLogger? logger = null)
    {
        ArgumentNullException.ThrowIfNull(slices);
        logger ??= CheckLogger.Create(null);

        var result = new List<Violation>();
        foreach (SliceRule rule in slices.Rules)
        {
            result.AddRange(CheckRule(slices, rule, options, logger));
        }

        foreach (DiagramRule rule in slices.DiagramRules)
        {
            result.AddRange(CheckDiagramRule(slices, rule, options, logger));
        }

        return result;
    }

    /// <summary>
    /// Checks one <c>contain dependency(from, to)</c> rule and returns the violations it found. Routes
    /// an empty slicing or an all-empty set of <c>from</c> or <c>to</c> files through the shared
    /// <see cref="EmptyTestGuard"/> before counting dependencies.
    /// </summary>
    /// <param name="slices">The policy the rule belongs to. Must not be <see langword="null"/>.</param>
    /// <param name="rule">The rule to check. Must not be <see langword="null"/>.</param>
    /// <param name="options">The options to check with; <see langword="null"/> means the defaults in <see cref="CheckOptions"/>.</param>
    /// <param name="logger">The check's logger, created by the terminal; <see langword="null"/> means the check logs nothing.</param>
    /// <returns>The violations found; empty when the rule passed.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="slices"/> or <paramref name="rule"/> is <see langword="null"/>.</exception>
    public static IReadOnlyList<Violation> CheckRule(
        Slices slices,
        SliceRule rule,
        CheckOptions? options,
        CheckLogger? logger = null)
    {
        ArgumentNullException.ThrowIfNull(slices);
        ArgumentNullException.ThrowIfNull(rule);
        logger ??= CheckLogger.Create(null);

        string description = DescribeRule(slices, rule);
        logger.StartCheck(description);

        if (SlicesProjection.SlicedFiles(slices.Graph, slices.Definitions).Count == 0)
        {
            IReadOnlyList<Violation> empty = EmptyTestGuard.Guard(description, options);
            logger.Violations(empty);
            return empty;
        }

        if (SlicesProjection.FilesOf(slices.Graph, slices.Definitions, rule.From).Count == 0
            || SlicesProjection.MatchingFiles(slices.Graph, rule.To).Count == 0)
        {
            IReadOnlyList<Violation> empty = EmptyTestGuard.Guard(description, options);
            logger.Violations(empty);
            return empty;
        }

        IReadOnlyList<SliceDependency> dependencies =
            SlicesProjection.Dependencies(slices.Graph, slices.Definitions, rule.From, rule.To);
        logger.Progress($"projected {dependencies.Count} dependency edge(s)");

        if (rule.Negate)
        {
            Violation[] violations = dependencies
                .Select(static dependency => (Violation)new ForbiddenDependencyViolation(
                    dependency.Slice,
                    dependency.Source,
                    dependency.Target))
                .ToArray();
            logger.Violations(violations);
            return violations;
        }

        IReadOnlyList<string> sliceNames = SlicesProjection.Slices(slices.Graph, slices.Definitions);
        var contained = new HashSet<string>(
            dependencies.Select(static dependency => dependency.Slice),
            StringComparer.Ordinal);

        Violation[] missing = sliceNames
            .Where(slice => !contained.Contains(slice))
            .Select(slice => (Violation)new MissingDependencyViolation(
                slice,
                rule.From.Pattern.Glob,
                rule.To.Pattern.Glob))
            .ToArray();
        logger.Violations(missing);
        return missing;
    }

    /// <summary>
    /// Checks one <c>adhere to diagram</c> rule and returns the violations it found: every projected
    /// dependency between slices that the diagram does not allow, one
    /// <see cref="DiagramAdherenceViolation"/> per slice pair. Routes an empty slicing or a diagram
    /// that declares no components and no arrows through the shared <see cref="EmptyTestGuard"/> before
    /// comparing dependencies. The rule's modifiers — ignoring external slices and ignoring orphan
    /// slices — drop dependencies from the comparison before it happens.
    /// </summary>
    /// <param name="slices">The policy the rule belongs to. Must not be <see langword="null"/>.</param>
    /// <param name="rule">The rule to check. Must not be <see langword="null"/>.</param>
    /// <param name="options">The options to check with; <see langword="null"/> means the defaults in <see cref="CheckOptions"/>.</param>
    /// <param name="logger">The check's logger, created by the terminal; <see langword="null"/> means the check logs nothing.</param>
    /// <returns>The violations found; empty when the rule passed.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="slices"/> or <paramref name="rule"/> is <see langword="null"/>.</exception>
    public static IReadOnlyList<Violation> CheckDiagramRule(
        Slices slices,
        DiagramRule rule,
        CheckOptions? options,
        CheckLogger? logger = null)
    {
        ArgumentNullException.ThrowIfNull(slices);
        ArgumentNullException.ThrowIfNull(rule);
        logger ??= CheckLogger.Create(null);

        string description = DescribeDiagramRule(slices, rule);
        logger.StartCheck(description);

        if (SlicesProjection.SlicedFiles(slices.Graph, slices.Definitions).Count == 0
            || (rule.Diagram.Components.Count == 0 && rule.Diagram.Dependencies.Count == 0))
        {
            IReadOnlyList<Violation> empty = EmptyTestGuard.Guard(description, options);
            logger.Violations(empty);
            return empty;
        }

        IReadOnlyList<ProjectedEdge> edges = Projection.Edges(
            slices.Graph,
            SlicesProjection.DiagramMap(identifier => SlicesProjection.SliceOf(slices.Definitions, identifier)));
        logger.Progress($"projected {edges.Count} dependency edge(s)");

        Violation[] violations = edges
            .Where(edge => !Ignored(edge, rule))
            .Where(edge => !rule.Diagram.Allows(edge.Source, edge.Target))
            .Select(static edge => (Violation)new DiagramAdherenceViolation(edge.Source, edge.Target))
            .ToArray();
        logger.Violations(violations);
        return violations;
    }

    /// <summary>
    /// Decides whether <paramref name="edge"/> is a dependency the rule ignores: an external dependency
    /// when external slices are ignored, or a dependency whose source or target the diagram does not
    /// declare as a component when orphan slices are ignored.
    /// </summary>
    private static bool Ignored(ProjectedEdge edge, DiagramRule rule)
    {
        if (rule.Options.IgnoreExternalSlices && edge.External)
        {
            return true;
        }

        if (!rule.Options.IgnoreOrphanSlices)
        {
            return false;
        }

        return !rule.Diagram.Components.Contains(edge.Source)
            || !rule.Diagram.Components.Contains(edge.Target);
    }

    /// <summary>
    /// Describes this rule as the subject of a report, for the empty-test guard: the entry phrase
    /// <c>project slices</c>, one clause per definition in the order they were added, and the rule's
    /// own predicate words — e.g. <c>project slices defined by 'src/features/(**)/*.cs' should adhere
    /// to diagram</c>.
    /// </summary>
    private static string DescribeDiagramRule(Slices slices, DiagramRule rule)
    {
        var builder = new StringBuilder("project slices");
        foreach (SliceDefinition definition in slices.Definitions)
        {
            builder.Append(' ');
            builder.Append(definition.Description);
        }

        builder.Append(" should ");
        builder.Append(rule.Description);
        return builder.ToString();
    }

    /// <summary>
    /// Describes this rule as the subject of a report, for the empty-test guard: the entry phrase
    /// <c>project slices</c>, one clause per definition in the order they were added, and the mood and
    /// dependency clause — e.g. <c>project slices defined by 'src/features/(**)/*.cs' should not
    /// contain dependency from 'src/legacy/**' to 'src/core/**'</c>.
    /// </summary>
    private static string DescribeRule(Slices slices, SliceRule rule)
    {
        var builder = new StringBuilder("project slices");
        foreach (SliceDefinition definition in slices.Definitions)
        {
            builder.Append(' ');
            builder.Append(definition.Description);
        }

        builder.Append(rule.Negate ? " should not" : " should");
        builder.Append(" contain dependency from '");
        builder.Append(rule.From.Pattern.Glob);
        builder.Append("' to '");
        builder.Append(rule.To.Pattern.Glob);
        builder.Append('\'');
        return builder.ToString();
    }
}
