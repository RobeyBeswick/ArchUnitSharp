namespace ArchUnitSharp.Testing;

using ArchUnitSharp.Common.Extraction;

/// <summary>
/// The single place a rule's <see cref="Violation"/> list becomes a verdict and a report message: given
/// the list <see cref="Common.Extraction.ICheckable.Check"/> returns, it produces a <see cref="CheckResult"/>
/// carrying the pass flag — empty list is a pass, any violation is a fail — and the message that goes
/// with it, every violation rendered by <see cref="ViolationFactory"/>.
/// </summary>
/// <remarks>
/// <para>
/// This is the shaping seam every adapter binds to: an adapter consumes a rule through
/// <see cref="Common.Extraction.ICheckable.Check"/>, hands the violations to
/// <see cref="Create(IReadOnlyList{Common.Extraction.Violation})"/> and gets a value it can branch on
/// and print. Adapters never format; they call in here. The message joins the rendered violations with a
/// newline in the order they were given — which is the order the rule's check produced, stable and
/// sorted — and the pass line is the fixed string the factory owns, so reports read the same way
/// everywhere.
/// </para>
/// <para>
/// This type is stateless and safe for concurrent use. The <see cref="CheckResult"/> it returns is a
/// fresh value on every call.
/// </para>
/// </remarks>
public static class ResultFactory
{
    /// <summary>
    /// The report line of a passing rule.
    /// </summary>
    public const string PassLine = "The rule passed.";

    /// <summary>
    /// Shapes <paramref name="violations"/> into a <see cref="CheckResult"/>: <see cref="CheckResult.Passed"/>
    /// is <see langword="true"/> when the list is empty, <see langword="false"/> otherwise, and
    /// <see cref="CheckResult.Message"/> is the fixed pass line or the violations' rendered messages
    /// joined with a newline, in the given order.
    /// </summary>
    /// <param name="violations">The violations a check returned. Must not be <see langword="null"/>.</param>
    /// <returns>The shaped verdict and message.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="violations"/> is <see langword="null"/>.</exception>
    public static CheckResult Create(IReadOnlyList<Violation> violations)
    {
        ArgumentNullException.ThrowIfNull(violations);

        if (violations.Count == 0)
        {
            return new CheckResult(Passed: true, Message: PassLine);
        }

        return new CheckResult(
            Passed: false,
            Message: string.Join("\n", violations.Select(ViolationFactory.Format)));
    }
}
