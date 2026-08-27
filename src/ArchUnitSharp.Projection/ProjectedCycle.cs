namespace ArchUnitSharp.Projection;

/// <summary>
/// A cycle of the projected graph: the projected edges that form a closed dependency loop, in order.
/// Cycle projection reports elementary cycles — no node appears more than once in a cycle, so each
/// cycle is a single loop rather than a tangle of them.
/// </summary>
/// <remarks>
/// <para>
/// The hops are <see cref="Edges"/>, each a <see cref="ProjectedEdge"/> whose raw edges name the
/// concrete files behind that hop, so a violation message can render both the loop of labels
/// (<c>A → B → C → A</c>) and the files that form it. The last hop's target is the first hop's source.
/// </para>
/// <para>
/// A cycle has at least two hops. A single self-loop is not a cycle here: projections filter
/// self-edges out, so the same label never depends on itself within a reported cycle.
/// </para>
/// <para>
/// This type is immutable and value-semantic: two cycles whose hops are equal are equal. The hop list
/// is copied on construction and copied again on every read, so a caller can never corrupt an instance
/// through a reference it obtained from it.
/// </para>
/// </remarks>
public sealed record ProjectedCycle
{
    private readonly ProjectedEdge[] _edges;

    /// <summary>
    /// The projected edges of this cycle, in order. The last hop's target is the first hop's source.
    /// Never empty. Each access returns a fresh copy, so the returned list is always safe to hold or
    /// mutate.
    /// </summary>
    public IReadOnlyList<ProjectedEdge> Edges
    {
        get => _edges.ToArray();
        init => _edges = RequireEdges(value);
    }

    /// <summary>
    /// Creates a cycle from its hops.
    /// </summary>
    /// <param name="edges">The projected edges of the cycle, in order; must not be <see langword="null"/>, and must carry at least two hops.</param>
    /// <exception cref="ArgumentNullException"><paramref name="edges"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="edges"/> carries fewer than two hops.</exception>
    public ProjectedCycle(IEnumerable<ProjectedEdge> edges)
    {
        _edges = RequireEdges(edges);
    }

    /// <summary>
    /// Two cycles are equal when their hops are equal.
    /// </summary>
    /// <param name="other">The other cycle.</param>
    /// <returns><see langword="true"/> when the cycles are equal.</returns>
    public bool Equals(ProjectedCycle? other) =>
        other is not null && Edges.SequenceEqual(other.Edges);

    /// <summary>
    /// A hash code consistent with <see cref="Equals(ProjectedCycle?)"/>.
    /// </summary>
    /// <returns>A hash code over the hops.</returns>
    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (ProjectedEdge edge in Edges)
        {
            hash.Add(edge);
        }

        return hash.ToHashCode();
    }

    private static ProjectedEdge[] RequireEdges(IEnumerable<ProjectedEdge> edges)
    {
        ArgumentNullException.ThrowIfNull(edges);
        ProjectedEdge[] copy = edges.ToArray();
        if (copy.Length < 2)
        {
            throw new ArgumentException("A cycle must carry at least two hops.", nameof(Edges));
        }

        return copy;
    }
}
