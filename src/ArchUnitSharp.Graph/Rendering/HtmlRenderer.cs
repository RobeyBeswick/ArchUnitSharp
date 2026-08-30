namespace ArchUnitSharp.Graph.Rendering;

using System.Text;
using ArchUnitSharp.Graph.Projection;
using ArchUnitSharp.Projection;

/// <summary>
/// Renders a <see cref="GraphSnapshot"/> as a self-contained HTML page: one file, every style inline
/// and the graph drawn as inline SVG, so the page renders a layered diagram of the project's
/// dependencies in any browser with no script, no external stylesheet and no network access.
/// </summary>
/// <remarks>
/// <para>
/// The layout is deterministic. Nodes — the snapshot's files plus its external targets — are ordered
/// ordinally and placed into columns by a longest-path layering: a node goes one column to the right
/// of its deepest predecessor, and a node left over by a cycle is given a fresh column after the rest
/// so the diagram is always a straight left-to-right flow. Nodes are stacked down each column in
/// sorted order, and every edge is drawn as a cubic Bézier curve from its source's right edge to its
/// target's left edge with an arrowhead. External targets render as distinct nodes in a second style,
/// so a dependency that leaves the project is visible at a glance.
/// </para>
/// <para>
/// Every label is routed through <see cref="RenderEscapes.Html"/>, so a label that contains a
/// character the browser parses cannot escape its text node. The page ends with a line that counts
/// the nodes and edges it drew. A graph with no nodes renders a page that says the graph is empty.
/// The output is stable and reproducible on every platform.
/// </para>
/// <para>
/// This type is stateless and safe for concurrent use.
/// </para>
/// </remarks>
internal static class HtmlRenderer
{
    private const int Margin = 20;
    private const int NodeHeight = 30;
    private const int NodeGap = 14;
    private const int LayerGap = 60;
    private const int TextWidthPerCharacter = 8;
    private const int NodeTextPadding = 16;
    private const int MinimumNodeWidth = 90;
    private const int ControlOffset = 25;

    private const string Head =
        "<!DOCTYPE html>\n"
        + "<html lang=\"en\">\n"
        + "<head>\n"
        + "<meta charset=\"utf-8\">\n"
        + "<title>Dependency graph</title>\n"
        + "<style>\n"
        + "body { margin: 2rem; font-family: sans-serif; }\n"
        + "h1 { font-size: 1.4rem; }\n"
        + "svg { border: 1px solid #ddd; }\n"
        + "path.edge { fill: none; stroke: #666; stroke-width: 1.5; }\n"
        + "path.edge.external { stroke: #c00; stroke-dasharray: 4 3; }\n"
        + ".node rect { fill: #eef; stroke: #446; stroke-width: 1.5; }\n"
        + ".node text { font: 12px sans-serif; fill: #222; }\n"
        + ".node.external rect { fill: #fdd; stroke: #c00; }\n"
        + "</style>\n"
        + "</head>\n"
        + "<body>\n"
        + "<h1>Dependency graph</h1>\n";

    /// <summary>
    /// Renders the snapshot as a self-contained HTML page.
    /// </summary>
    /// <param name="snapshot">The graph snapshot to render. Must not be <see langword="null"/>.</param>
    /// <returns>The HTML document.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="snapshot"/> is <see langword="null"/>.</exception>
    public static string Render(GraphSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        string[] labels = snapshot.Nodes
            .Select(static node => node.Label)
            .Concat(ExternalTargets(snapshot))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (labels.Length == 0)
        {
            return Head + "<p>The graph is empty.</p>\n</body>\n</html>\n";
        }

        var external = new HashSet<string>(ExternalTargets(snapshot), StringComparer.Ordinal);
        var layers = AssignLayers(labels, snapshot);

        int nodeWidth = Math.Max(
            MinimumNodeWidth,
            labels.Max(static label => TextWidthPerCharacter * label.Length) + NodeTextPadding);

        var rows = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var column in labels.GroupBy(label => layers[label]).OrderBy(static group => group.Key))
        {
            int row = 0;
            foreach (string label in column)
            {
                rows[label] = row++;
            }
        }

        int maxLayer = labels.Max(label => layers[label]);
        int maxRows = labels.Max(label => rows[label]) + 1;
        int width = Margin + maxLayer * (nodeWidth + LayerGap) + nodeWidth + Margin;
        int height = Margin + maxRows * NodeHeight + (maxRows - 1) * NodeGap + Margin;

        var x = new Dictionary<string, int>(StringComparer.Ordinal);
        var y = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (string label in labels)
        {
            x[label] = Margin + layers[label] * (nodeWidth + LayerGap);
            y[label] = Margin + rows[label] * (NodeHeight + NodeGap);
        }

        var builder = new StringBuilder(Head);
        builder.Append($"<svg width=\"{width}\" height=\"{height}\" viewBox=\"0 0 {width} {height}\" xmlns=\"http://www.w3.org/2000/svg\">\n");
        builder.Append("<defs>\n");
        builder.Append("<marker id=\"arrow\" markerWidth=\"9\" markerHeight=\"6\" refX=\"8\" refY=\"3\" orient=\"auto\">\n");
        builder.Append("<path d=\"M0,0 L9,3 L0,6 z\" fill=\"#666\"></path>\n");
        builder.Append("</marker>\n");
        builder.Append("</defs>\n");

        foreach (ProjectedEdge edge in snapshot.Edges)
        {
            int sourceRight = x[edge.Source] + nodeWidth;
            int sourceMid = y[edge.Source] + NodeHeight / 2;
            int targetLeft = x[edge.Target];
            int targetMid = y[edge.Target] + NodeHeight / 2;

            string cssClass = edge.External ? "edge external" : "edge";
            string d = $"M{sourceRight},{sourceMid} "
                + $"C{sourceRight + ControlOffset},{sourceMid} "
                + $"{targetLeft - ControlOffset},{targetMid} "
                + $"{targetLeft},{targetMid}";
            builder.Append($"<path class=\"{cssClass}\" d=\"{d}\" marker-end=\"url(#arrow)\"></path>\n");
        }

        foreach (string label in labels)
        {
            string cssClass = external.Contains(label) ? "node external" : "node";
            int textX = nodeWidth / 2;
            int textY = NodeHeight / 2 + 5;
            builder.Append($"<g class=\"{cssClass}\" transform=\"translate({x[label]},{y[label]})\">\n");
            builder.Append($"<rect width=\"{nodeWidth}\" height=\"{NodeHeight}\"></rect>\n");
            builder.Append($"<text x=\"{textX}\" y=\"{textY}\" text-anchor=\"middle\">{RenderEscapes.Html(label)}</text>\n");
            builder.Append("</g>\n");
        }

        builder.Append("</svg>\n");
        string nodeWord = labels.Length == 1 ? "node" : "nodes";
        string edgeWord = snapshot.Edges.Count == 1 ? "edge" : "edges";
        builder.Append($"<p>{labels.Length} {nodeWord}, {snapshot.Edges.Count} {edgeWord}</p>\n");
        builder.Append("</body>\n</html>\n");
        return builder.ToString();
    }

    private static Dictionary<string, int> AssignLayers(string[] labels, GraphSnapshot snapshot)
    {
        var incoming = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        foreach (string label in labels)
        {
            incoming[label] = new List<string>();
        }

        foreach (ProjectedEdge edge in snapshot.Edges)
        {
            if (incoming.TryGetValue(edge.Target, out List<string>? predecessors))
            {
                predecessors.Add(edge.Source);
            }
        }

        foreach (List<string> predecessors in incoming.Values)
        {
            predecessors.Sort(StringComparer.Ordinal);
        }

        var layers = new Dictionary<string, int>(StringComparer.Ordinal);
        var remaining = new List<string>(labels);
        while (remaining.Count > 0)
        {
            bool progressed = false;
            var still = new List<string>();
            foreach (string label in remaining)
            {
                if (incoming[label].All(layers.ContainsKey))
                {
                    int layer = 0;
                    foreach (string predecessor in incoming[label])
                    {
                        layer = Math.Max(layer, layers[predecessor] + 1);
                    }

                    layers[label] = layer;
                    progressed = true;
                }
                else
                {
                    still.Add(label);
                }
            }

            if (!progressed)
            {
                int layer = labels.Max(label => layers.TryGetValue(label, out int existing) ? existing : -1);
                foreach (string label in still)
                {
                    layers[label] = ++layer;
                }

                break;
            }

            remaining = still;
        }

        return layers;
    }

    private static IEnumerable<string> ExternalTargets(GraphSnapshot snapshot) =>
        snapshot.Edges
            .Where(static edge => edge.External)
            .Select(static edge => edge.Target)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static target => target, StringComparer.Ordinal);
}
