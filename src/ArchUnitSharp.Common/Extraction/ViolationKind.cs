namespace ArchUnitSharp.Common.Extraction;

/// <summary>
/// Discriminates the concrete subtypes of <see cref="Violation"/>. A consumer can switch on a kind
/// without inspecting value data. Adding a new violation kind means adding a member here and, once a
/// concrete violation for it exists, a corresponding <see cref="Violation"/> subtype.
/// </summary>
public enum ViolationKind
{
    /// <summary>
    /// The rule matched no input at all, which the empty-test guard treats as a failure. See
    /// <see cref="EmptyTestViolation"/>.
    /// </summary>
    EmptyTest,

    /// <summary>
    /// A rule predicate failed: the checked value did not satisfy the stated threshold or condition.
    /// No concrete subtype carries this kind yet; it exists so the family has a distinct, non-zero
    /// member, which is what makes the base kind propagation observable in tests.
    /// </summary>
    Rule,
}
