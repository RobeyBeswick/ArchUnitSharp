namespace ArchUnitSharp.Projection;

using ArchUnitSharp.Common.Extraction;

/// <summary>
/// A node of the projected graph: a relabelled endpoint that the <see cref="MapFunction"/> hook
/// produces from the raw self-edge of each file, together with the raw self-edges (one per file) of
/// the files that project to it. Node projection depends on self-edges — every file's marker edge —
/// so a file with no dependencies still appears as a node, and a file the map drops does not.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Label"/> is the module's name for the node — a layer, a slice, a class name, or a file
/// identifier when the map is the identity. <see cref="Edges"/> carries the raw self-edge of every
/// file that projects to this node, so a violation message can point at the concrete files behind it;
/// a node backed by several files carries all of them.
/// </para>
/// <para>
/// This type is immutable and value-semantic: two nodes with the same label and the same raw edges are
/// equal. The raw-edge list is copied on construction and copied again on every read, so a caller can
/// never corrupt an instance through a reference it obtained from it.
/// </para>
/// </remarks>
public sealed record ProjectedNode
{
    private readonly string _label;
    private readonly Edge[] _edges;

    /// <summary>
    /// The module's label for this node. Must not be <see langword="null"/> or empty; both the
    /// constructor and a <see langword="with"/> expression route through the same validation, so
    /// neither can introduce a bad value.
    /// </summary>
    public string Label
    {
        get => _label;
        init => _label = RequireLabel(value);
    }

    /// <summary>
    /// The raw self-edges — one per file — of the files that project to this node. Never empty. Each
    /// access returns a fresh copy, so the returned list is always safe to hold or mutate.
    /// </summary>
    public IReadOnlyList<Edge> Edges
    {
        get => _edges.ToArray();
        init => _edges = RequireEdges(value);
    }

    /// <summary>
    /// Creates a projected node.
    /// </summary>
    /// <param name="label">The module's label for this node; must not be <see langword="null"/> or empty.</param>
    /// <param name="edges">The raw self-edges of the files that project to this node; must not be <see langword="null"/> or empty.</param>
    /// <exception cref="ArgumentNullException"><paramref name="label"/> or <paramref name="edges"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="label"/> is empty, or <paramref name="edges"/> is empty.</exception>
    public ProjectedNode(string label, IEnumerable<Edge> edges)
    {
        _label = RequireLabel(label);
        _edges = RequireEdges(edges);
    }

    /// <summary>
    /// Two nodes are equal when their labels and raw edge lists are equal.
    /// </summary>
    /// <param name="other">The other projected node.</param>
    /// <returns><see langword="true"/> when the nodes are equal.</returns>
    public bool Equals(ProjectedNode? other) =>
        other is not null
        && string.Equals(Label, other.Label, StringComparison.Ordinal)
        && Edges.SequenceEqual(other.Edges);

    /// <summary>
    /// A hash code consistent with <see cref="Equals(ProjectedNode?)"/>.
    /// </summary>
    /// <returns>A hash code over the label and raw edges.</returns>
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Label);
        foreach (Edge edge in Edges)
        {
            hash.Add(edge);
        }

        return hash.ToHashCode();
    }

    private static string RequireLabel(string label) =>
        label is null
            ? throw new ArgumentNullException(nameof(Label))
            : label.Length == 0
                ? throw new ArgumentException("Label must not be empty.", nameof(Label))
                : label;

    private static Edge[] RequireEdges(IEnumerable<Edge> edges)
    {
        ArgumentNullException.ThrowIfNull(edges);
        Edge[] copy = edges.ToArray();
        if (copy.Length == 0)
        {
            throw new ArgumentException("A projected node must be backed by at least one raw self-edge.", nameof(Edges));
        }

        return copy;
    }
}
