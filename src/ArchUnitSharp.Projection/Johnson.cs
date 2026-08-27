namespace ArchUnitSharp.Projection;

using ArchUnitSharp.Common.Extraction;

/// <summary>
/// Johnson's elementary-cycles algorithm, the second half of the projection layer's cycle detection:
/// it enumerates every elementary cycle of a projected graph — every closed dependency loop in which
/// no node appears more than once — from the graph's strongly connected components, which
/// <see cref="Tarjan.FindStronglyConnectedComponents"/> computes.
/// </summary>
/// <remarks>
/// <para>
/// A cycle is reported as its distinct nodes in order, starting at the component's smallest node; a
/// self-loop is reported as a one-node cycle. <see cref="Projection.Cycles"/> never feeds self-loops
/// here — projections filter self-edges out — so in practice every reported cycle has at least two
/// nodes. The number of elementary cycles can be exponential in the size of a dense graph, so this
/// enumeration is intended for the projected graphs of the domain modules, which are small.
/// </para>
/// <para>
/// The input is a set of <see cref="ProjectedEdge"/> values whose <see cref="ProjectedEdge.Source"/>
/// and <see cref="ProjectedEdge.Target"/> name the nodes; parallel projected edges are already merged,
/// so <c>(source, target)</c> is unique. The output is sorted by length then by contents, so reports
/// are reproducible.
/// </para>
/// <para>
/// This type is stateless and safe for concurrent use. The lists it returns are fresh copies. The
/// algorithm is recursive over the longest cycle, so it is bounded by the graph's size.
/// </para>
/// </remarks>
public static class Johnson
{
    /// <summary>
    /// Returns every elementary cycle of the graph described by <paramref name="edges"/>. Each cycle
    /// is the distinct nodes of one closed dependency loop, in order, starting at the loop's smallest
    /// node. A graph with no cycles yields an empty list.
    /// </summary>
    /// <param name="edges">The projected edges of the graph. Must not be <see langword="null"/>.</param>
    /// <returns>The elementary cycles, sorted by length then by contents.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="edges"/> is <see langword="null"/>.</exception>
    public static IReadOnlyList<IReadOnlyList<string>> FindElementaryCycles(
        IReadOnlyList<ProjectedEdge> edges)
    {
        ArgumentNullException.ThrowIfNull(edges);

        Dictionary<string, List<string>> adjacency = Tarjan.BuildAdjacency(edges, out _);
        var cycles = new List<IReadOnlyList<string>>();

        foreach (IReadOnlyList<string> component in Tarjan.FindStronglyConnectedComponents(edges))
        {
            if (component.Count == 1)
            {
                string vertex = component[0];
                if (adjacency.TryGetValue(vertex, out List<string>? neighbors) && neighbors.Contains(vertex))
                {
                    cycles.Add(new[] { vertex });
                }

                continue;
            }

            FindCyclesInComponent(adjacency, component, cycles);
        }

        return cycles
            .OrderBy(static cycle => cycle.Count)
            .ThenBy(static cycle => string.Join("\u0001", cycle), StringComparer.Ordinal)
            .ToArray();
    }

    /// <summary>
    /// Enumerates the elementary cycles of one strongly connected component. Each cycle is found
    /// exactly once, at the start of its smallest node: the graph is restricted to the nodes at or
    /// after the start in the component's ordinal order, so a cycle is only reachable from its own
    /// smallest node.
    /// </summary>
    private static void FindCyclesInComponent(
        IReadOnlyDictionary<string, List<string>> adjacency,
        IReadOnlyList<string> component,
        ICollection<IReadOnlyList<string>> cycles)
    {
        var position = new Dictionary<string, int>(StringComparer.Ordinal);
        for (int i = 0; i < component.Count; i++)
        {
            position[component[i]] = i;
        }

        for (int startIndex = 0; startIndex < component.Count; startIndex++)
        {
            string start = component[startIndex];

            var restricted = BuildRestricted(adjacency, component, position, startIndex);
            var blocked = new HashSet<string>(StringComparer.Ordinal);
            var blockedBy = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            var stack = new List<string>();
            Visit(start);

            bool Visit(string current)
            {
                bool found = false;
                stack.Add(current);
                blocked.Add(current);

                if (restricted.TryGetValue(current, out List<string>? neighbors))
                {
                    foreach (string neighbor in neighbors)
                    {
                        if (neighbor == start)
                        {
                            cycles.Add(stack.ToArray());
                            found = true;
                        }
                        else if (!blocked.Contains(neighbor) && Visit(neighbor))
                        {
                            found = true;
                        }
                    }
                }

                if (found)
                {
                    Unblock(current);
                }
                else if (restricted.TryGetValue(current, out List<string>? outNeighbors))
                {
                    foreach (string neighbor in outNeighbors)
                    {
                        if (!blockedBy.TryGetValue(neighbor, out List<string>? blockers))
                        {
                            blockers = new List<string>();
                            blockedBy[neighbor] = blockers;
                        }

                        if (!blockers.Contains(current))
                        {
                            blockers.Add(current);
                        }
                    }
                }

                stack.RemoveAt(stack.Count - 1);
                return found;
            }

            void Unblock(string vertex)
            {
                blocked.Remove(vertex);
                if (!blockedBy.TryGetValue(vertex, out List<string>? unblocked))
                {
                    return;
                }

                foreach (string blocker in unblocked)
                {
                    if (blocked.Contains(blocker))
                    {
                        Unblock(blocker);
                    }
                }

                blockedBy.Remove(vertex);
            }
        }
    }

    /// <summary>
    /// The subgraph of the component containing only the nodes at or after <paramref name="startIndex"/>
    /// in the component's ordinal order, with edges restricted to those nodes.
    /// </summary>
    private static Dictionary<string, List<string>> BuildRestricted(
        IReadOnlyDictionary<string, List<string>> adjacency,
        IReadOnlyList<string> component,
        IReadOnlyDictionary<string, int> position,
        int startIndex)
    {
        var restricted = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        for (int i = startIndex; i < component.Count; i++)
        {
            string vertex = component[i];
            List<string> neighbors = adjacency.TryGetValue(vertex, out List<string>? list)
                ? list.Where(neighbor => position.TryGetValue(neighbor, out int p) && p >= startIndex).ToList()
                : new List<string>();
            restricted[vertex] = neighbors;
        }

        return restricted;
    }
}
