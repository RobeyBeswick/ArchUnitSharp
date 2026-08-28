namespace ArchUnitSharp.Layers.Assertion;

using System.Text;
using ArchUnitSharp.Common.Extraction;
using ArchUnitSharp.Layers.Projection;

/// <summary>
/// The layers module's shared assertion: the one place a layers rule's outcome is computed. A
/// <see cref="LayerRule"/> carries one subject layer and any number of constraints — an allowlist
/// (<c>may only depend on layers(...)</c>) or a blocklist (<c>may not depend on layers(...)</c>) —
/// and every constraint is routed through the shared <see cref="EmptyTestGuard"/>, so every layers
/// terminal reaches the guard.
/// </summary>
/// <remarks>
/// <para>
/// A dependency of the subject layer is allowed when its target belongs to the subject layer itself
/// (intra-layer dependencies are always allowed) or to no declared layer at all (edges where either
/// end belongs to no declared layer are ignored). Every other dependency is checked against the rule's
/// constraints, blocklists before allowlists: a dependency the blocklist forbids is a
/// <see cref="LayerViolation"/> no matter what the allowlist permits, and a dependency an allowlist
/// forbids — its target belongs to a declared layer the allowlist does not name — is a
/// <see cref="LayerViolation"/> too. An allowlist with no names is the sealed-layer idiom: the subject
/// layer may depend on nothing outside itself.
/// </para>
/// <para>
/// The <see cref="EmptyTestGuard"/> reports a rule whose subject layer matched no files, or whose
/// constraints are all vacuous — a blocklist with no names, or a blocklist whose named layers all
/// match no files. An allowlist is never vacuous: an allowlist with no names is the sealed-layer
/// idiom, and an allowlist whose named layers all match no files still forbids every dependency on
/// any other declared layer, so both are checked rather than guarded.
/// </para>
/// <para>
/// This type is stateless and safe for concurrent use. The lists it returns are fresh copies.
/// </para>
/// </remarks>
internal static class LayersAssertion
{
    /// <summary>
    /// Checks a <see cref="LayerRule"/>: each forbidden dependency is reported as one
    /// <see cref="LayerViolation"/>, and the <see cref="EmptyTestGuard"/> reports a rule whose subject
    /// layer matched nothing or whose constraints are all vacuous.
    /// </summary>
    /// <param name="rule">The rule to check. Must not be <see langword="null"/>.</param>
    /// <param name="options">The options to check with; <see langword="null"/> means the defaults in <see cref="CheckOptions"/>.</param>
    /// <returns>The violations found; empty when the rule passed.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="rule"/> is <see langword="null"/>.</exception>
    /// <exception cref="UserError"><paramref name="rule"/> carries no constraint, so there is nothing to assert.</exception>
    public static IReadOnlyList<Violation> Check(LayerRule rule, CheckOptions? options)
    {
        ArgumentNullException.ThrowIfNull(rule);

        if (rule.Constraints.Count == 0)
        {
            throw new UserError(
                "A layer rule needs at least one 'may only depend on layers(...)' or "
                + "'may not depend on layers(...)' constraint before it is checked.");
        }

        IReadOnlyList<string> subject = LayersProjection.FilesOf(rule.Graph, rule.Subject.Filter);

        if (subject.Count == 0 || rule.Constraints.All(constraint => IsVacuous(constraint, rule)))
        {
            return EmptyTestGuard.Guard(DescribeRule(rule), options);
        }

        var violations = new List<Violation>();
        foreach (CrossLayerDependency dependency in LayersProjection.CrossLayerDependencies(
            rule.Graph,
            rule.Subject,
            rule.Layers))
        {
            string? targetLayer = ViolatingTargetLayer(rule, dependency);
            if (targetLayer is not null)
            {
                violations.Add(new LayerViolation(
                    rule.Subject.Name,
                    dependency.Source,
                    dependency.Target,
                    targetLayer));
            }
        }

        return violations.ToArray();
    }

    /// <summary>
    /// A constraint is vacuous when it can never forbid anything: a blocklist with no names at all,
    /// or a blocklist whose named layers all match no files. An allowlist is never vacuous — an
    /// allowlist with no names is the sealed-layer idiom, and an allowlist whose named layers all
    /// match no files still forbids every dependency on any other declared layer, so both forbid
    /// something and must be checked.
    /// </summary>
    private static bool IsVacuous(LayerConstraint constraint, LayerRule rule)
    {
        if (constraint.Kind == LayerConstraintKind.AllowOnly)
        {
            return false;
        }

        return constraint.LayerNames.All(name =>
            LayersProjection.FilesOf(rule.Graph, DeclaredLayer(rule, name).Filter).Count == 0);
    }

    /// <summary>
    /// Returns the name of the layer that makes <paramref name="dependency"/> a violation of
    /// <paramref name="rule"/>, or <see langword="null"/> when no constraint forbids it. Blocklist
    /// constraints are evaluated before allowlist constraints, so a dependency both forbid and
    /// permitted is a violation of the blocklist.
    /// </summary>
    private static string? ViolatingTargetLayer(LayerRule rule, CrossLayerDependency dependency)
    {
        foreach (LayerConstraint constraint in rule.Constraints)
        {
            if (constraint.Kind != LayerConstraintKind.Forbid)
            {
                continue;
            }

            foreach (string name in constraint.LayerNames)
            {
                if (dependency.TargetLayers.Contains(name))
                {
                    return name;
                }
            }
        }

        foreach (LayerConstraint constraint in rule.Constraints)
        {
            if (constraint.Kind != LayerConstraintKind.AllowOnly)
            {
                continue;
            }

            foreach (string name in dependency.TargetLayers)
            {
                if (!constraint.LayerNames.Contains(name))
                {
                    return name;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// The layer declared under <paramref name="name"/>. Every name a <see cref="LayerRule"/>
    /// references was validated against the declared layers when the constraint was built, so this
    /// lookup always succeeds.
    /// </summary>
    private static Layer DeclaredLayer(LayerRule rule, string name) => rule.Find(name)!;

    /// <summary>
    /// Describes the rule for a report: the subject layer's declaration followed by one clause per
    /// constraint. A rule over the layer <c>Model</c> defined by <c>src/Models/**</c> that may not
    /// depend on the layers <c>Service</c> and <c>Repository</c> is described as
    /// <c>layer 'Model' defined by 'src/Models/**' may not depend on layers 'Service' 'Repository'</c>.
    /// </summary>
    private static string DescribeRule(LayerRule rule)
    {
        var builder = new StringBuilder();
        builder.Append("layer '");
        builder.Append(rule.Subject.Name);
        builder.Append('\'');

        Filter filter = rule.Subject.Filter;
        builder.Append(filter.Target == MatchTarget.Path ? " defined by '" : " defined by folder '");
        builder.Append(filter.Pattern.Glob);
        builder.Append('\'');

        foreach (LayerConstraint constraint in rule.Constraints)
        {
            builder.Append(constraint.Kind == LayerConstraintKind.Forbid
                ? " may not depend on layers"
                : " may only depend on layers");
            foreach (string name in constraint.LayerNames)
            {
                builder.Append(" '");
                builder.Append(name);
                builder.Append('\'');
            }
        }

        return builder.ToString();
    }
}
