namespace ArchUnitSharp.Graph.Rendering;

using ArchUnitSharp.Graph.Projection;
using ArchUnitSharp.Projection;

/// <summary>
/// Renders a <see cref="GraphSnapshot"/> as a CSV table: one header row naming the four columns —
/// <c>source</c>, <c>target</c>, <c>external</c> and <c>importKinds</c> — and one row per projected
/// edge.
/// </summary>
/// <remarks>
/// <para>
/// Every field is routed through <see cref="RenderEscapes.Csv"/>, so a label that contains a comma, a
/// quote or a line break is returned quoted with its internal quotes doubled and cannot break the
/// table. The <c>external</c> column is <c>true</c> when the dependency leaves the project and
/// <c>false</c> otherwise; the <c>importKinds</c> column is the union of import kinds the projected
/// edge carries, in the <see cref="System.Enum.ToString()"/> form. Rows appear in the snapshot's
/// sorted edge order, so the output is stable and reproducible.
/// </para>
/// <para>
/// This type is stateless and safe for concurrent use.
/// </para>
/// </remarks>
internal static class CsvRenderer
{
    /// <summary>
    /// Renders the snapshot as a CSV table.
    /// </summary>
    /// <param name="snapshot">The graph snapshot to render. Must not be <see langword="null"/>.</param>
    /// <returns>The CSV text, one edge per line after the header.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="snapshot"/> is <see langword="null"/>.</exception>
    public static string Render(GraphSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var lines = new List<string>(snapshot.Edges.Count + 1) { "source,target,external,importKinds" };
        foreach (ProjectedEdge edge in snapshot.Edges)
        {
            lines.Add(string.Join(
                ',',
                RenderEscapes.Csv(edge.Source),
                RenderEscapes.Csv(edge.Target),
                edge.External ? "true" : "false",
                RenderEscapes.Csv(edge.ImportKinds.ToString())));
        }

        return string.Join('\n', lines) + "\n";
    }
}
