namespace ArchUnitSharp.Slices;

/// <summary>
/// The immutable modifiers of an <c>adhere to diagram</c> rule: which dependencies of the actual graph
/// the rule ignores before comparing it against the diagram. The two flags are set by the chain words
/// <c>ignoring orphan slices</c> and <c>ignoring external slices</c> on the <see cref="Should"/> mood.
/// </summary>
/// <remarks>
/// <para>
/// Ignoring external slices drops every dependency whose target lies outside the project — an edge to
/// an external module — from the comparison. Ignoring orphan slices drops every dependency whose
/// source or target is a component the diagram does not declare at all, so slices the architect did
/// not draw into the diagram are not held to it. The two modifiers are independent and combine freely.
/// </para>
/// <para>
/// This type is immutable and value-semantic. It is internal: the public surface exposes the modifiers
/// as chain words, never as a bag.
/// </para>
/// </remarks>
internal sealed record DiagramAdherenceOptions
{
    /// <summary>
    /// The default modifiers: neither kind of dependency is ignored.
    /// </summary>
    internal static DiagramAdherenceOptions Default { get; } = new();

    /// <summary>
    /// When <see langword="true"/>, dependencies whose target lies outside the project are ignored.
    /// </summary>
    internal bool IgnoreExternalSlices { get; init; }

    /// <summary>
    /// When <see langword="true"/>, dependencies whose source or target the diagram does not declare
    /// as a component are ignored.
    /// </summary>
    internal bool IgnoreOrphanSlices { get; init; }
}
