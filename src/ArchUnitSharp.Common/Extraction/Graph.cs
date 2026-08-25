namespace ArchUnitSharp.Common.Extraction;

/// <summary>
/// An immutable collection of <see cref="Edge"/> instances: the project's dependency graph as a list
/// of its edges.
/// </summary>
/// <remarks>
/// <para>
/// The edge list is copied on construction and copied again on every read, so a caller can never
/// corrupt the graph through a reference it obtained from this type. The order of edges is preserved
/// exactly as supplied. This type is immutable and safe for concurrent use: once constructed, it
/// never changes.
/// </para>
/// </remarks>
public sealed class Graph
{
    private readonly Edge[] _edges;

    /// <summary>
    /// Creates a graph from the given edges. The supplied sequence is copied; later mutation of it
    /// does not affect this graph.
    /// </summary>
    /// <param name="edges">The edges of the graph. Must not be <see langword="null"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="edges"/> is <see langword="null"/>.</exception>
    public Graph(IEnumerable<Edge> edges)
    {
        ArgumentNullException.ThrowIfNull(edges);
        _edges = edges.ToArray();
    }

    /// <summary>
    /// The graph's edges, in the order they were supplied. Each access returns a fresh copy, so the
    /// returned list is always safe to hold or mutate.
    /// </summary>
    public IReadOnlyList<Edge> Edges => _edges.ToArray();
}
