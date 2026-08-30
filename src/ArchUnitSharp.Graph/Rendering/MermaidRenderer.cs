namespace ArchUnitSharp.Graph.Rendering;

/// <summary>
/// Renders a <see cref="GraphSnapshot"/> as a Mermaid flowchart: every file of the snapshot becomes a
/// quoted node with a stable <c>n</c>-prefixed id, every external target a quoted node of its own,
/// and every aggregated edge an arrow between the two ids.
/// </summary>
/// <remarks>
/// <para>
/// Mermaid node ids cannot carry the characters a file path does, so each label is given a generated
/// id — <c>n0</c>, <c>n1</c>, … in the snapshot's sorted order, external targets after the files —
/// and the label is shown through a quoted label on the node declaration. Labels are embedded with
/// <see cref="RenderEscapes.Mermaid"/>, so a label that contains a quote cannot break out of the
/// label syntax. Nodes are declared in sorted order and edges in the snapshot's sorted order, so the
/// output is stable and reproducible.
/// </para>
/// <para>
/// This type is stateless and safe for concurrent use.
/// </para>
/// </remarks>
internal static class MermaidRenderer
{
    /// <summary>
    /// Renders the snapshot as a Mermaid flowchart.
    /// </summary>
    /// <param name="snapshot">The graph snapshot to render. Must not be <see langword="null"/>.</param>
    /// <returns>The Mermaid source, one statement per line.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="snapshot"/> is <see langword="null"/>.</exception>
    public static string Render(GraphSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        string[] labels = snapshot.Nodes
            .Select(static node => node.Label)
            .Concat(ExternalTargets(snapshot))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        var ids = new Dictionary<string, string>(StringComparer.Ordinal);
        var lines = new List<string> { "flowchart LR" };
        for (int i = 0; i < labels.Length; i++)
        {
            ids[labels[i]] = $"n{i}";
            lines.Add($"  n{i}[\"{RenderEscapes.Mermaid(labels[i])}\"]");
        }

        foreach (SnapshotEdge edge in snapshot.Edges)
        {
            lines.Add($"  {ids[edge.Source]} --> {ids[edge.Target]}");
        }

        return string.Join('\n', lines) + "\n";
    }

    private static IEnumerable<string> ExternalTargets(GraphSnapshot snapshot) =>
        snapshot.Edges
            .Where(static edge => edge.External)
            .Select(static edge => edge.Target)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static target => target, StringComparer.Ordinal);
}
