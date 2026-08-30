namespace ArchUnitSharp.Graph.Rendering;

/// <summary>
/// Renders a <see cref="GraphSnapshot"/> as a D2 diagram: every file of the snapshot becomes a quoted
/// node declaration, every external target a quoted declaration of its own, and every aggregated edge
/// an arrow between the two quoted identifiers.
/// </summary>
/// <remarks>
/// <para>
/// Identifiers are embedded between double quotes with <see cref="RenderEscapes.D2"/>, so a label
/// that contains a quote or a backslash cannot break out of the identifier syntax. Nodes are emitted
/// in the snapshot's sorted order, external targets after the files, and edges in the snapshot's
/// sorted order, so the output is stable and reproducible. A graph with no nodes — and therefore no
/// edges — renders to an empty document.
/// </para>
/// <para>
/// This type is stateless and safe for concurrent use.
/// </para>
/// </remarks>
internal static class D2Renderer
{
    /// <summary>
    /// Renders the snapshot as a D2 diagram.
    /// </summary>
    /// <param name="snapshot">The graph snapshot to render. Must not be <see langword="null"/>.</param>
    /// <returns>The D2 source, one statement per line, or an empty string for an empty graph.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="snapshot"/> is <see langword="null"/>.</exception>
    public static string Render(GraphSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        string[] labels = snapshot.Nodes
            .Select(static node => node.Label)
            .Concat(ExternalTargets(snapshot))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        var lines = new List<string>(labels.Length + snapshot.Edges.Count);
        foreach (string label in labels)
        {
            lines.Add($"\"{RenderEscapes.D2(label)}\"");
        }

        foreach (SnapshotEdge edge in snapshot.Edges)
        {
            lines.Add($"\"{RenderEscapes.D2(edge.Source)}\" -> \"{RenderEscapes.D2(edge.Target)}\"");
        }

        return lines.Count == 0 ? string.Empty : string.Join('\n', lines) + "\n";
    }

    private static IEnumerable<string> ExternalTargets(GraphSnapshot snapshot) =>
        snapshot.Edges
            .Where(static edge => edge.External)
            .Select(static edge => edge.Target)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static target => target, StringComparer.Ordinal);
}
