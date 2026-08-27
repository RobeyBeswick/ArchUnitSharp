namespace ArchUnitSharp.Projection;

using ArchUnitSharp.Common.Extraction;

/// <summary>
/// Tarjan's strongly-connected-components algorithm, the first half of the projection layer's cycle
/// detection: it partitions the nodes of a projected graph into strongly connected components, the
/// sets of nodes from which every node can reach every other.
/// </summary>
/// <remarks>
/// <para>
/// A component of more than one node contains at least one cycle; a component of one node contains a
/// cycle only if it has a self-loop. <see cref="Johnson.FindElementaryCycles"/> enumerates the actual
/// cycles within each cyclic component.
/// </para>
/// <para>
/// The input is a set of <see cref="ProjectedEdge"/> values whose <see cref="ProjectedEdge.Source"/>
/// and <see cref="ProjectedEdge.Target"/> name the nodes; parallel projected edges are already merged,
/// so <c>(source, target)</c> is unique. The output is sorted: each component's nodes are sorted
/// ordinally and the components themselves are ordered by those contents, so reports are reproducible.
/// </para>
/// <para>
/// This type is stateless and safe for concurrent use. The lists it returns are fresh copies. The
/// algorithm is recursive over the graph's longest path; it is intended for the projected graphs of
/// the domain modules, which are small, not for the full file graph.
/// </para>
/// </remarks>
public static class Tarjan
{
    /// <summary>
    /// Returns the strongly connected components of the graph described by <paramref name="edges"/>.
    /// Every node of the graph belongs to exactly one component.
    /// </summary>
    /// <param name="edges">The projected edges of the graph. Must not be <see langword="null"/>.</param>
    /// <returns>The strongly connected components, each node set sorted ordinally, the components sorted by their contents.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="edges"/> is <see langword="null"/>.</exception>
    public static IReadOnlyList<IReadOnlyList<string>> FindStronglyConnectedComponents(
        IReadOnlyList<ProjectedEdge> edges)
    {
        ArgumentNullException.ThrowIfNull(edges);

        var adjacency = BuildAdjacency(edges, out SortedSet<string> nodes);

        var indices = new Dictionary<string, int>(StringComparer.Ordinal);
        var lowlink = new Dictionary<string, int>(StringComparer.Ordinal);
        var onStack = new HashSet<string>(StringComparer.Ordinal);
        var stack = new List<string>();
        var components = new List<List<string>>();
        int index = 0;

        foreach (string node in nodes)
        {
            if (!indices.ContainsKey(node))
            {
                StrongConnect(node);
            }
        }

        return components
            .Select(static component => (IReadOnlyList<string>)component.OrderBy(static n => n, StringComparer.Ordinal).ToArray())
            .OrderBy(static component => string.Join("\u0001", component), StringComparer.Ordinal)
            .ToArray();

        void StrongConnect(string vertex)
        {
            indices[vertex] = index;
            lowlink[vertex] = index;
            index++;
            stack.Add(vertex);
            onStack.Add(vertex);

            if (adjacency.TryGetValue(vertex, out List<string>? neighbors))
            {
                foreach (string neighbor in neighbors)
                {
                    if (!indices.ContainsKey(neighbor))
                    {
                        StrongConnect(neighbor);
                        lowlink[vertex] = Math.Min(lowlink[vertex], lowlink[neighbor]);
                    }
                    else if (onStack.Contains(neighbor))
                    {
                        lowlink[vertex] = Math.Min(lowlink[vertex], indices[neighbor]);
                    }
                }
            }

            if (lowlink[vertex] != indices[vertex])
            {
                return;
            }

            var component = new List<string>();
            while (true)
            {
                string member = stack[^1];
                stack.RemoveAt(stack.Count - 1);
                onStack.Remove(member);
                component.Add(member);
                if (member == vertex)
                {
                    break;
                }
            }

            components.Add(component);
        }
    }

    /// <summary>
    /// The node set and per-node adjacency of the graph described by <paramref name="edges"/>.
    /// Neighbours are sorted ordinally and deduplicated, so traversal order is deterministic.
    /// </summary>
    internal static Dictionary<string, List<string>> BuildAdjacency(
        IReadOnlyList<ProjectedEdge> edges,
        out SortedSet<string> nodes)
    {
        nodes = new SortedSet<string>(StringComparer.Ordinal);
        var adjacency = new Dictionary<string, List<string>>(StringComparer.Ordinal);

        foreach (ProjectedEdge edge in edges)
        {
            nodes.Add(edge.Source);
            nodes.Add(edge.Target);

            if (!adjacency.TryGetValue(edge.Source, out List<string>? neighbors))
            {
                neighbors = new List<string>();
                adjacency[edge.Source] = neighbors;
            }

            if (!neighbors.Contains(edge.Target))
            {
                neighbors.Add(edge.Target);
            }
        }

        foreach (List<string> neighbors in adjacency.Values)
        {
            neighbors.Sort(StringComparer.Ordinal);
        }

        return adjacency;
    }
}
