namespace ArchUnitSharp.Layers.Assertion;

using ArchUnitSharp.Common.Extraction;
using ArchUnitSharp.Layers.Projection;

/// <summary>
/// The layers module's shared assertion: the one place a layers rule's outcome is computed. A rule is
/// a <see cref="LayerConstraint"/> — the subject layer, the target layers, and the mood
/// (<c>may only depend on</c> versus <c>may not depend on</c>) — and every constraint routes an empty
/// selection through the shared <see cref="EmptyTestGuard"/>. Checking a whole <see cref="Layers"/>
/// policy evaluates its blocklist constraints before its allowlist constraints, and a dependency a
/// blocklist already reported is not reported again by an allowlist on the same subject layer.
/// </summary>
/// <remarks>
/// <para>
/// A constraint's subject is empty — its layer selects no files — or its named target layers all
/// select no files, is a violation (<see cref="EmptyTestViolation"/>) rather than a pass, unless
/// <see cref="CheckOptions.AllowEmptyTests"/> is set. A <c>may only depend on layers()</c> constraint
/// with no target layers is a sealed layer and is <em>not</em> empty: it means the subject may depend
/// on no other layer, so every cross-layer dependency of the subject is a violation.
/// </para>
/// <para>
/// Intra-layer dependencies are always allowed — the projection never produces them — and an edge
/// whose endpoint belongs to no declared layer is ignored. With the blocklist mood each cross-layer
/// dependency onto a named target layer is reported; with the allowlist mood each cross-layer
/// dependency onto a layer <em>outside</em> the named targets is reported (all of them, for a sealed
/// layer). Each reported dependency is one <see cref="LayerViolation"/>.
/// </para>
/// <para>
/// This type is stateless and safe for concurrent use. The lists it returns are fresh copies.
/// </para>
/// <para>
/// Each assertion is handed the check's <see cref="CheckLogger"/> by the terminal that calls it and
/// emits the fixed logging vocabulary: every constraint is a <c>start check</c> naming the rule, the
/// cross-layer dependencies the policy projects are progress, and every violation is logged as it is
/// produced. The logger only buffers lines — the assertion never touches the filesystem — and the
/// terminal's wrapper records the check's end and flushes the log after the assertion returns.
/// </para>
/// </remarks>
internal static class LayersAssertion
{
    /// <summary>
    /// Checks every constraint of a layers policy and returns the violations found, blocklist
    /// constraints first. A dependency already reported by a blocklist constraint is not reported
    /// again by an allowlist constraint on the same subject layer.
    /// </summary>
    /// <param name="layers">The policy to check. Must not be <see langword="null"/>.</param>
    /// <param name="options">The options to check with; <see langword="null"/> means the defaults in <see cref="CheckOptions"/>.</param>
    /// <param name="logger">The check's logger, created by the terminal; <see langword="null"/> means the check logs nothing.</param>
    /// <returns>The violations found; empty when the policy passed.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="layers"/> is <see langword="null"/>.</exception>
    public static IReadOnlyList<Violation> Check(
        Layers layers,
        CheckOptions? options,
        CheckLogger? logger = null)
    {
        ArgumentNullException.ThrowIfNull(layers);
        logger ??= CheckLogger.Create(null);

        IReadOnlyList<LayerConstraint> constraints = layers.Constraints;
        logger.Progress($"checking {constraints.Count} layer rule(s)");
        var blocked = new HashSet<LayerViolation>();
        var result = new List<Violation>();

        foreach (LayerConstraint constraint in constraints)
        {
            if (!constraint.Negate)
            {
                continue;
            }

            IReadOnlyList<Violation> violations = CheckConstraint(layers, constraint, options, logger);
            foreach (Violation violation in violations)
            {
                if (violation is LayerViolation layerViolation)
                {
                    blocked.Add(layerViolation);
                }
            }

            result.AddRange(violations);
        }

        foreach (LayerConstraint constraint in constraints)
        {
            if (constraint.Negate)
            {
                continue;
            }

            IReadOnlyList<Violation> violations = CheckConstraint(layers, constraint, options, logger);
            foreach (Violation violation in violations)
            {
                if (violation is LayerViolation layerViolation && blocked.Contains(layerViolation))
                {
                    continue;
                }

                result.Add(violation);
            }
        }

        return result;
    }

    /// <summary>
    /// Checks one layer constraint and returns the violations it found. Routes an empty subject or an
    /// all-empty set of named target layers through the shared <see cref="EmptyTestGuard"/> before
    /// computing cross-layer dependencies.
    /// </summary>
    /// <param name="layers">The policy the constraint belongs to. Must not be <see langword="null"/>.</param>
    /// <param name="constraint">The constraint to check. Must not be <see langword="null"/>.</param>
    /// <param name="options">The options to check with; <see langword="null"/> means the defaults in <see cref="CheckOptions"/>.</param>
    /// <param name="logger">The check's logger, created by the terminal; <see langword="null"/> means the check logs nothing.</param>
    /// <returns>The violations found; empty when the constraint passed.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="layers"/> or <paramref name="constraint"/> is <see langword="null"/>.</exception>
    public static IReadOnlyList<Violation> CheckConstraint(
        Layers layers,
        LayerConstraint constraint,
        CheckOptions? options,
        CheckLogger? logger = null)
    {
        ArgumentNullException.ThrowIfNull(layers);
        ArgumentNullException.ThrowIfNull(constraint);
        logger ??= CheckLogger.Create(null);

        string rule = constraint.DescribeRule();
        logger.StartCheck(rule);

        Graph graph = layers.Graph;
        IReadOnlyList<LayerDeclaration> declarations = layers.Declarations;
        string subjectLayer = constraint.SubjectLayer;

        if (LayersProjection.FilesOfLayer(graph, declarations, subjectLayer).Count == 0)
        {
            IReadOnlyList<Violation> empty = EmptyTestGuard.Guard(rule, options);
            logger.Violations(empty);
            return empty;
        }

        string[] targets = constraint.TargetLayers.ToArray();
        if (targets.Length > 0
            && targets.All(name => LayersProjection.FilesOfLayer(graph, declarations, name).Count == 0))
        {
            IReadOnlyList<Violation> empty = EmptyTestGuard.Guard(rule, options);
            logger.Violations(empty);
            return empty;
        }

        IReadOnlyList<CrossLayerDependency> dependencies =
            LayersProjection.CrossLayerDependencies(graph, declarations);
        logger.Progress($"projected {dependencies.Count} cross-layer dependencies");

        var targetSet = new HashSet<string>(targets, StringComparer.Ordinal);
        bool sealedLayer = !constraint.Negate && targets.Length == 0;

        Violation[] violations = dependencies
            .Where(dependency =>
                dependency.SourceLayer == subjectLayer
                && (constraint.Negate
                    ? targetSet.Contains(dependency.TargetLayer)
                    : sealedLayer || !targetSet.Contains(dependency.TargetLayer)))
            .Select(static dependency => (Violation)new LayerViolation(
                dependency.SourceLayer,
                dependency.TargetLayer,
                dependency.Source,
                dependency.Target))
            .ToArray();
        logger.Violations(violations);
        return violations;
    }
}
