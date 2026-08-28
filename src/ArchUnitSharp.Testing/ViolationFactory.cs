namespace ArchUnitSharp.Testing;

using ArchUnitSharp.Common.Extraction;
using ArchUnitSharp.Files;
using ArchUnitSharp.Layers;

/// <summary>
/// The single place a <see cref="Violation"/> becomes a human-readable message: given a violation's
/// structured data — the offending file, dependency, cycle, value or rule — it renders the report line
/// a consumer shows. All message formatting lives here and only here; no adapter formats its own prose.
/// </summary>
/// <remarks>
/// <para>
/// A violation carries data, not prose, so the exact wording of a report is this factory's decision:
/// the template for each concrete violation subtype is fixed here, and a consumer who wants different
/// wording supplies it over the data, not by editing the module that produced the violation. The rule
/// text a module hands the empty-test guard arrives as the violation's <em>data</em>; this factory
/// turns it into the report sentence. The concrete subtypes the library defines today are each
/// handled — <see cref="EmptyTestViolation"/>, <see cref="FileViolation"/>,
/// <see cref="AdhereToViolation"/>, <see cref="DependencyViolation"/>,
/// <see cref="CycleViolation"/> and <see cref="LayerViolation"/> — and a violation subtype this
/// factory does not know is a defect, so it throws rather than fabricate a message.
/// </para>
/// <para>
/// This type is stateless and safe for concurrent use; the strings it returns are freshly built on
/// every call.
/// </para>
/// </remarks>
public static class ViolationFactory
{
    /// <summary>
    /// Renders <paramref name="violation"/> as the message a report prints.
    /// </summary>
    /// <param name="violation">The violation to render. Must not be <see langword="null"/>.</param>
    /// <returns>The violation's report message.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="violation"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="violation"/> is a subtype this factory does not know how to render.</exception>
    public static string Format(Violation violation)
    {
        ArgumentNullException.ThrowIfNull(violation);
        return violation switch
        {
            EmptyTestViolation v => $"The rule matched nothing: {v.RuleDescription}.",
            FileViolation v => $"File '{v.File}' violates the rule.",
            AdhereToViolation v => $"File '{v.File}' violates the rule: {v.Message}",
            DependencyViolation v => $"File '{v.Source}' must not depend on '{v.Target}'.",
            LayerViolation v =>
                $"Layer '{v.SubjectLayer}': file '{v.Source}' must not depend on file '{v.Target}' "
                + $"in layer '{v.TargetLayer}'.",
            CycleViolation v => $"Cycle: {v.Path}",
            _ => throw new ArgumentOutOfRangeException(
                nameof(violation),
                violation,
                "Violation is not a defined Violation subtype."),
        };
    }
}
