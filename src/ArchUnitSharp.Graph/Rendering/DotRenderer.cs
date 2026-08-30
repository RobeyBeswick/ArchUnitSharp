namespace ArchUnitSharp.Graph.Rendering;

/// <summary>
/// Renders a <see cref="GraphSnapshot"/> as a DOT digraph for Graphviz: every file of the snapshot
/// becomes a quoted node declaration and every aggregated edge an arrow between quoted identifiers.
/// An external target is not declared — a DOT edge to it brings the node into existence — and its
/// arrow renders with the same style as an internal one, so the DOT is the structure of the graph
/// and nothing else.
/// </summary>
/// <remarks>
/// <para>
/// Identifiers are embedded between double quotes with <see cref="RenderEscapes.Dot"/>, so a label
/// that contains a quote or a backslash cannot break out of the identifier syntax. Nodes are emitted
/// in the snapshot's sorted order and edges in the snapshot's sorted order, so the output is stable
/// and reproducible.
/// </para>
/// <para>
/// This type is stateless and safe for concurrent use.
/// </para>
/// </remarks>
internal static class DotRenderer
{
    /// <summary>
    /// Renders the snapshot as a DOT digraph.
    /// </summary>
    /// <param name="snapshot">The graph snapshot to render. Must not be <see langword="null"/>.</param>
    /// <returns>The DOT source, one statement per line.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="snapshot"/> is <see langword="null"/>.</exception>
    public static string Render(GraphSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var lines = new List<string> { "digraph {" };
        foreach (SnapshotNode node in snapshot.Nodes)
        {
            lines.Add($"  \"{RenderEscapes.Dot(node.Label)}\";");
        }

        foreach (SnapshotEdge edge in snapshot.Edges)
        {
            lines.Add($"  \"{RenderEscapes.Dot(edge.Source)}\" -> \"{RenderEscapes.Dot(edge.Target)}\";");
        }

        lines.Add("}");
        return string.Join('\n', lines) + "\n";
    }
}
