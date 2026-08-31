namespace ArchUnitSharp.Slices.Assertion;

using System.Text;
using ArchUnitSharp.Common.Extraction;
using ArchUnitSharp.Slices.Projection;

/// <summary>
/// The slices module's shared assertion: the one place a <c>contain dependency(from, to)</c> rule's
/// outcome is computed. The mood of a rule arrives as the <c>negate</c> boolean — there is no separate
/// code path for <c>should not</c> — and every rule routes an empty selection through the shared
/// <see cref="EmptyTestGuard"/>, so every terminal that calls in here reaches the guard.
/// </summary>
/// <remarks>
/// <para>
/// A rule's subject is the whole slicing: the files the definitions assign to slices. A subject with
/// no sliced files, whose <c>from</c> filter matches no sliced file, or whose <c>to</c> filter matches
/// no file of the graph is a violation (<see cref="EmptyTestViolation"/>) rather than a pass, unless
/// <see cref="CheckOptions.AllowEmptyTests"/> is set — a typo in a definition or a <c>from</c>/<c>to</c>
/// glob must not pass silently. A policy with no rules passes.
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
/// This type is stateless and safe for concurrent use. The lists it returns are fresh copies.
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
    /// <returns>The violations found; empty when the policy passed.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="slices"/> is <see langword="null"/>.</exception>
    public static IReadOnlyList<Violation> Check(Slices slices, CheckOptions? options)
    {
        ArgumentNullException.ThrowIfNull(slices);

        var result = new List<Violation>();
        foreach (SliceRule rule in slices.Rules)
        {
            result.AddRange(CheckRule(slices, rule, options));
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
    /// <returns>The violations found; empty when the rule passed.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="slices"/> or <paramref name="rule"/> is <see langword="null"/>.</exception>
    public static IReadOnlyList<Violation> CheckRule(Slices slices, SliceRule rule, CheckOptions? options)
    {
        ArgumentNullException.ThrowIfNull(slices);
        ArgumentNullException.ThrowIfNull(rule);

        if (SlicesProjection.SlicedFiles(slices.Graph, slices.Definitions).Count == 0)
        {
            return EmptyTestGuard.Guard(DescribeRule(slices, rule), options);
        }

        if (SlicesProjection.FilesOf(slices.Graph, slices.Definitions, rule.From).Count == 0
            || SlicesProjection.MatchingFiles(slices.Graph, rule.To).Count == 0)
        {
            return EmptyTestGuard.Guard(DescribeRule(slices, rule), options);
        }

        IReadOnlyList<SliceDependency> dependencies =
            SlicesProjection.Dependencies(slices.Graph, slices.Definitions, rule.From, rule.To);

        if (rule.Negate)
        {
            return dependencies
                .Select(static dependency => (Violation)new ForbiddenDependencyViolation(
                    dependency.Slice,
                    dependency.Source,
                    dependency.Target))
                .ToArray();
        }

        IReadOnlyList<string> sliceNames = SlicesProjection.Slices(slices.Graph, slices.Definitions);
        var contained = new HashSet<string>(
            dependencies.Select(static dependency => dependency.Slice),
            StringComparer.Ordinal);

        return sliceNames
            .Where(slice => !contained.Contains(slice))
            .Select(slice => (Violation)new MissingDependencyViolation(
                slice,
                rule.From.Pattern.Glob,
                rule.To.Pattern.Glob))
            .ToArray();
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
