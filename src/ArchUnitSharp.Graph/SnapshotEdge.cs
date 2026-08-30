namespace ArchUnitSharp.Graph;

using ArchUnitSharp.Common.Extraction;

/// <summary>
/// An edge of a <see cref="GraphSnapshot"/>: an aggregate of the raw dependencies between two labels
/// of the snapshot. It carries the two labels, the number of raw dependencies it replaces, whether the
/// dependency leaves the project, and the union of their import kinds.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Source"/> and <see cref="Target"/> are node labels — file identifiers, folders, collapse
/// buckets or external module names — not file identifiers. <see cref="Count"/> is the aggregation
/// count: how many raw dependency edges between the two labels the query's scope included.
/// <see cref="External"/> is <see langword="true"/> when the dependency leaves the project, which is
/// exactly when the target label is an external module name; a label any internal edge reaches is never
/// external. <see cref="ImportKinds"/> is the union of the import kinds of the aggregated raw edges.
/// </para>
/// <para>
/// This type is immutable and value-semantic, and safe for concurrent use.
/// </para>
/// </remarks>
public sealed record SnapshotEdge
{
    private readonly string _source;
    private readonly string _target;

    /// <summary>
    /// The label of the importing endpoint of the aggregated dependency. Must not be
    /// <see langword="null"/> or empty; both the constructor and a <see langword="with"/> expression
    /// route through the same validation, so neither can introduce a bad value.
    /// </summary>
    public string Source
    {
        get => _source;
        init => _source = RequireSource(value);
    }

    /// <summary>
    /// The label of the imported endpoint of the aggregated dependency. Must not be
    /// <see langword="null"/> or empty; both the constructor and a <see langword="with"/> expression
    /// route through the same validation, so neither can introduce a bad value.
    /// </summary>
    public string Target
    {
        get => _target;
        init => _target = RequireTarget(value);
    }

    /// <summary>
    /// The aggregation count: the number of raw dependency edges between the two labels that this edge
    /// replaces. Always at least one.
    /// </summary>
    public int Count { get; init; }

    /// <summary>
    /// <see langword="true"/> when the aggregated dependency leaves the project being analysed.
    /// </summary>
    public bool External { get; init; }

    /// <summary>
    /// The union of the import kinds of the raw dependency edges this edge aggregates. A single raw
    /// edge contributes exactly its own kind.
    /// </summary>
    public ImportKind ImportKinds { get; init; }

    /// <summary>
    /// Creates a snapshot edge.
    /// </summary>
    /// <param name="source">The label of the importing endpoint; must not be <see langword="null"/> or empty.</param>
    /// <param name="target">The label of the imported endpoint; must not be <see langword="null"/> or empty.</param>
    /// <param name="count">The aggregation count; must be at least one.</param>
    /// <param name="external"><see langword="true"/> when the dependency leaves the project.</param>
    /// <param name="importKinds">The union of the import kinds of the aggregated raw edges.</param>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> or <paramref name="target"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="source"/> or <paramref name="target"/> is empty.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is less than one.</exception>
    public SnapshotEdge(string source, string target, int count, bool external, ImportKind importKinds)
    {
        _source = RequireSource(source);
        _target = RequireTarget(target);
        if (count < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(count), count, "Count must not be less than one.");
        }

        Count = count;
        External = external;
        ImportKinds = importKinds;
    }

    /// <summary>
    /// Two edges are equal when their sources, targets, counts, external flags and import kinds are
    /// equal.
    /// </summary>
    /// <param name="other">The other edge.</param>
    /// <returns><see langword="true"/> when the edges are equal.</returns>
    public bool Equals(SnapshotEdge? other) =>
        other is not null
        && string.Equals(Source, other.Source, StringComparison.Ordinal)
        && string.Equals(Target, other.Target, StringComparison.Ordinal)
        && Count == other.Count
        && External == other.External
        && ImportKinds == other.ImportKinds;

    /// <summary>
    /// A hash code consistent with <see cref="Equals(SnapshotEdge?)"/>.
    /// </summary>
    /// <returns>A hash code over the source, target, count, external flag and import kinds.</returns>
    public override int GetHashCode() => HashCode.Combine(Source, Target, Count, External, ImportKinds);

    private static string RequireSource(string source) =>
        source is null
            ? throw new ArgumentNullException(nameof(Source))
            : source.Length == 0
                ? throw new ArgumentException("Source must not be empty.", nameof(Source))
                : source;

    private static string RequireTarget(string target) =>
        target is null
            ? throw new ArgumentNullException(nameof(Target))
            : target.Length == 0
                ? throw new ArgumentException("Target must not be empty.", nameof(Target))
                : target;
}
