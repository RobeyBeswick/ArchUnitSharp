namespace ArchUnitSharp.Graph.Rendering;

using System.Text;

/// <summary>
/// Renders a <see cref="GraphSnapshot"/> as a JSON document: a <c>nodes</c> array with one object per
/// file of the snapshot and an <c>edges</c> array with one object per aggregated edge carrying its
/// source, target, external flag and union of import kinds.
/// </summary>
/// <remarks>
/// <para>
/// Every string value is routed through <see cref="RenderEscapes.Json"/>, so a label that contains a
/// quote, a backslash or a control character cannot break the document. Nodes appear in the
/// snapshot's sorted order and edges in the snapshot's sorted order, the key order of every object is
/// fixed, and line endings are always <c>\n</c>, so the output is stable, reproducible and identical
/// on every platform.
/// </para>
/// <para>
/// This type is stateless and safe for concurrent use.
/// </para>
/// </remarks>
internal static class JsonRenderer
{
    /// <summary>
    /// Renders the snapshot as a JSON document.
    /// </summary>
    /// <param name="snapshot">The graph snapshot to render. Must not be <see langword="null"/>.</param>
    /// <returns>The JSON document.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="snapshot"/> is <see langword="null"/>.</exception>
    public static string Render(GraphSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        string[] nodeLines = snapshot.Nodes
            .Select(static node => $"    {{ \"id\": \"{RenderEscapes.Json(node.Label)}\" }}")
            .ToArray();

        string[] edgeLines = snapshot.Edges
            .Select(static edge =>
                "    { \"source\": \"" + RenderEscapes.Json(edge.Source)
                + "\", \"target\": \"" + RenderEscapes.Json(edge.Target)
                + "\", \"external\": " + (edge.External ? "true" : "false")
                + ", \"importKinds\": \"" + RenderEscapes.Json(edge.ImportKinds.ToString()) + "\" }")
            .ToArray();

        var builder = new StringBuilder();
        builder.Append("{\n");
        AppendArray(builder, "nodes", nodeLines, trailingComma: true);
        AppendArray(builder, "edges", edgeLines, trailingComma: false);
        builder.Append("}\n");
        return builder.ToString();
    }

    private static void AppendArray(StringBuilder builder, string name, string[] lines, bool trailingComma)
    {
        builder.Append("  \"");
        builder.Append(name);
        builder.Append("\": ");
        if (lines.Length == 0)
        {
            builder.Append("[]");
        }
        else
        {
            builder.Append("[\n");
            builder.Append(string.Join(",\n", lines));
            builder.Append("\n  ]");
        }

        builder.Append(trailingComma ? ",\n" : "\n");
    }
}
