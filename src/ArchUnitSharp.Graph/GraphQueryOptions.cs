namespace ArchUnitSharp.Graph;

using ArchUnitSharp.Common.Extraction;

/// <summary>
/// The internal data model of one snapshot query: the accumulated state of a <see cref="GraphReport"/>
/// builder. Each query option the surface exposes maps to one field here, and a fluent method returns
/// a new builder carrying a new <see cref="GraphQueryOptions"/> derived from the current one, so the
/// state is immutable and a half-built query can be branched from.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Focus"/> and <see cref="FocusDepth"/> together form the <c>focus on(pattern, depth)</c>
/// option: <see cref="Focus"/> is the whole-path filter that selects the seed files and
/// <see cref="FocusDepth"/> the hop radius around them. A <see langword="null"/> restriction field
/// means that restriction was not applied. <see cref="Collapse"/> is the ordered list of collapse
/// rules applied to each file's identifier, in the order they were added.
/// </para>
/// <para>
/// <see cref="CheckOptions"/> is the options bag the builder's rule terminal (<see cref="GraphReport.Check"/>)
/// honours when its scope matches nothing. It defaults to <c>new CheckOptions()</c>.
/// </para>
/// <para>
/// This type is immutable and safe for concurrent use: every property is set once at construction
/// and the collapse list is never mutated after it is stored.
/// </para>
/// </remarks>
internal sealed record GraphQueryOptions
{
    private readonly CollapseRule[] _collapse = Array.Empty<CollapseRule>();

    /// <summary>
    /// The default query options: every restriction unset, external and self dependencies excluded,
    /// no collapse rules, an empty title and the default <see cref="Common.Extraction.CheckOptions"/>.
    /// </summary>
    public static GraphQueryOptions Default { get; } = new();

    /// <summary>
    /// When <see langword="true"/>, edges whose target lies outside the project are included in the
    /// snapshot. When <see langword="false"/> (the default), they are excluded.
    /// </summary>
    public bool IncludeExternalDependencies { get; init; }

    /// <summary>
    /// When <see langword="true"/>, the per-file self-edge every file carries is included in the
    /// snapshot as a self-loop. When <see langword="false"/> (the default), only real dependencies
    /// are aggregated.
    /// </summary>
    public bool IncludeSelfDependencies { get; init; }

    /// <summary>
    /// The whole-path filter that selects the seed files of the <c>focus on</c> restriction;
    /// <see langword="null"/> when the restriction is not applied.
    /// </summary>
    public Filter? Focus { get; init; }

    /// <summary>
    /// The hop radius of the <c>focus on</c> restriction. Meaningful only when <see cref="Focus"/> is
    /// not <see langword="null"/>.
    /// </summary>
    public int FocusDepth { get; init; }

    /// <summary>
    /// The whole-path filter that selects the seed files of the <c>reachable from</c> restriction;
    /// <see langword="null"/> when the restriction is not applied.
    /// </summary>
    public Filter? ReachableFrom { get; init; }

    /// <summary>
    /// The whole-path filter that selects the seed files of the <c>dependents of</c> restriction;
    /// <see langword="null"/> when the restriction is not applied.
    /// </summary>
    public Filter? DependentsOf { get; init; }

    /// <summary>
    /// The collapse rules applied to each file's identifier, in the order they were added. The first
    /// rule that relabels a file wins; a file no rule relabels keeps its own identifier as its label.
    /// Each access returns a fresh copy, so the returned list is always safe to hold or mutate.
    /// </summary>
    public CollapseRule[] Collapse
    {
        get => _collapse.ToArray();
        init => _collapse = value.ToArray();
    }

    /// <summary>
    /// The snapshot's title; empty when <c>titled</c> was not applied.
    /// </summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>
    /// The options bag the rule terminal honours when the snapshot's scope matches nothing.
    /// </summary>
    public CheckOptions CheckOptions { get; init; } = new();
}
