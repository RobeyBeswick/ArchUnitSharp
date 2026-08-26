namespace ArchUnitSharp.Common.Extraction;

/// <summary>
/// The base type of every rule failure. A rule that passes yields an empty violation list; a rule
/// that fails yields one or more <see cref="Violation"/> instances describing what went wrong.
/// </summary>
/// <remarks>
/// <para>
/// A violation carries <em>data, not prose</em>: the offending edge, node, cycle, value or threshold.
/// Formatted, human-readable messages are constructed elsewhere (the testing layer), never stored
/// here. That keeps a violation a stable, structured value a consumer can switch on without parsing
/// text.
/// </para>
/// <para>
/// <see cref="Kind"/> discriminates the concrete subtype. Every concrete kind is a sealed record, so
/// the family stays closed by convention.
/// </para>
/// <para>
/// This type is immutable and value-semantic: equality is by subtype and value, which is what lets a
/// test assert on an exact expected violation.
/// </para>
/// </remarks>
public abstract record Violation
{
    /// <summary>
    /// The kind of this violation, discriminating the concrete subtype. A consumer can switch on it,
    /// or pattern-match on the subtype directly, without inspecting value data.
    /// </summary>
    public ViolationKind Kind { get; }

    /// <summary>
    /// Initializes a violation of the given kind. Only derived types can call this.
    /// </summary>
    /// <param name="kind">The violation kind of the concrete subtype.</param>
    protected Violation(ViolationKind kind) => Kind = kind;
}
