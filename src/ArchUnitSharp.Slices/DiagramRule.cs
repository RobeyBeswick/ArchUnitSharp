namespace ArchUnitSharp.Slices;

using ArchUnitSharp.Common.Extraction;
using ArchUnitSharp.Slices.Uml;

/// <summary>
/// The internal data model of one <c>adhere to diagram</c> rule: the parsed diagram the rule validates
/// the slicing against, the modifiers that decide which actual dependencies are ignored, and the rule's
/// own words for a report. It is produced by <c>should adhere to diagram</c> and
/// <c>should adhere to diagram in file</c> and checked by
/// <see cref="Assertion.SlicesAssertion"/>.
/// </summary>
/// <remarks>
/// <para>
/// The diagram is parsed when the rule is built — the chain word <c>adhere to diagram</c> parses its
/// text, and <c>adhere to diagram in file</c> reads and parses its file — so the check is pure and the
/// same rule reports the same result on every check. A malformed diagram is a <see cref="UserError"/>
/// at build time, naming the offending line, and a diagram file that cannot be read is a
/// <see cref="TechnicalError"/>.
/// </para>
/// <para>
/// This type is immutable and safe for concurrent use.
/// </para>
/// </remarks>
internal sealed class DiagramRule
{
    /// <summary>
    /// The parsed diagram the rule validates the slicing against.
    /// </summary>
    internal PlantUmlDiagram Diagram { get; }

    /// <summary>
    /// The modifiers: which actual dependencies are ignored before the comparison.
    /// </summary>
    internal DiagramAdherenceOptions Options { get; }

    /// <summary>
    /// The rule as the predicate of a report: <c>adhere to diagram</c> or
    /// <c>adhere to diagram in file 'path'</c>.
    /// </summary>
    internal string Description { get; }

    /// <summary>
    /// Creates a diagram rule.
    /// </summary>
    /// <param name="diagram">The parsed diagram. Must not be <see langword="null"/>.</param>
    /// <param name="options">The modifiers. Must not be <see langword="null"/>.</param>
    /// <param name="description">The rule's words in a report. Must not be <see langword="null"/> or empty.</param>
    /// <exception cref="ArgumentNullException"><paramref name="diagram"/>, <paramref name="options"/> or <paramref name="description"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="description"/> is empty.</exception>
    internal DiagramRule(PlantUmlDiagram diagram, DiagramAdherenceOptions options, string description)
    {
        ArgumentNullException.ThrowIfNull(diagram);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(description);
        if (description.Length == 0)
        {
            throw new ArgumentException("A diagram rule description must not be empty.", nameof(description));
        }

        Diagram = diagram;
        Options = options;
        Description = description;
    }
}
