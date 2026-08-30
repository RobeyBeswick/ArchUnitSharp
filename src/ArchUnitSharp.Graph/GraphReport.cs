namespace ArchUnitSharp.Graph;

using ArchUnitSharp.Common.Extraction;
using ArchUnitSharp.Graph.Projection;
using ArchUnitSharp.Graph.Rendering;

/// <summary>
/// The graph domain module's fluent surface: a report over one project's <see cref="Graph"/>, in any
/// of the six output formats. It is the ENTRY and TERMINAL of a report chain — built from the entry
/// points <c>Project.ProjectGraph()</c> / <c>Project.Graph()</c> and finished with a
/// <c>to ...()</c> or <c>export as ...(path)</c> call, one for each format.
/// </summary>
/// <remarks>
/// <para>
/// A <see cref="GraphReport"/> captures its <see cref="GraphSnapshot"/> once — the files of the graph
/// as nodes and its dependencies between distinct files as edges, external dependencies included —
/// and renders every format from that same snapshot. The string forms, <see cref="ToDot"/>,
/// <see cref="ToMermaid"/>, <see cref="ToD2"/>, <see cref="ToCsv"/>, <see cref="ToJson"/> and
/// <see cref="ToHtml"/>, return the format's text; the file forms, <see cref="ExportAsDot"/>,
/// <see cref="ExportAsMermaid"/>, <see cref="ExportAsD2"/>, <see cref="ExportAsCsv"/>,
/// <see cref="ExportAsJson"/> and <see cref="ExportAsHtml"/>, write that text to a file and return
/// the path they wrote. The file form is the module's only disk boundary; the rendering itself is
/// pure.
/// </para>
/// <para>
/// This type is immutable and safe for concurrent use: the snapshot is captured on construction and
/// never changes, so every render of one report is identical.
/// </para>
/// </remarks>
public sealed class GraphReport
{
    private readonly GraphSnapshot _snapshot;

    /// <summary>
    /// Creates a report over <paramref name="graph"/>, capturing its snapshot immediately. The graph
    /// itself is not retained — the report holds only the immutable snapshot.
    /// </summary>
    /// <param name="graph">The project's dependency graph. Must not be <see langword="null"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="graph"/> is <see langword="null"/>.</exception>
    public GraphReport(Graph graph)
    {
        ArgumentNullException.ThrowIfNull(graph);
        _snapshot = GraphProjection.Snapshot(graph);
    }

    /// <summary>
    /// <c>to dot()</c>: renders the report as a DOT digraph for Graphviz.
    /// </summary>
    /// <returns>The DOT source.</returns>
    public string ToDot() => DotRenderer.Render(_snapshot);

    /// <summary>
    /// <c>to mermaid()</c>: renders the report as a Mermaid flowchart.
    /// </summary>
    /// <returns>The Mermaid source.</returns>
    public string ToMermaid() => MermaidRenderer.Render(_snapshot);

    /// <summary>
    /// <c>to d2()</c>: renders the report as a D2 diagram.
    /// </summary>
    /// <returns>The D2 source.</returns>
    public string ToD2() => D2Renderer.Render(_snapshot);

    /// <summary>
    /// <c>to csv()</c>: renders the report as a CSV table, one row per dependency.
    /// </summary>
    /// <returns>The CSV text.</returns>
    public string ToCsv() => CsvRenderer.Render(_snapshot);

    /// <summary>
    /// <c>to json()</c>: renders the report as a JSON document with the nodes and edges arrays.
    /// </summary>
    /// <returns>The JSON document.</returns>
    public string ToJson() => JsonRenderer.Render(_snapshot);

    /// <summary>
    /// <c>to html()</c>: renders the report as a self-contained HTML page with the graph drawn as
    /// inline SVG.
    /// </summary>
    /// <returns>The HTML document.</returns>
    public string ToHtml() => HtmlRenderer.Render(_snapshot);

    /// <summary>
    /// <c>export as dot(path)</c>: renders the report as DOT and writes it to <paramref name="path"/>.
    /// </summary>
    /// <param name="path">The file to write. Must not be <see langword="null"/> or empty; its directory must exist.</param>
    /// <returns><paramref name="path"/>, which now holds the DOT source.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="path"/> is empty.</exception>
    /// <exception cref="TechnicalError">The file cannot be written.</exception>
    public string ExportAsDot(string path) => Export(path, ToDot());

    /// <summary>
    /// <c>export as mermaid(path)</c>: renders the report as Mermaid and writes it to
    /// <paramref name="path"/>.
    /// </summary>
    /// <param name="path">The file to write. Must not be <see langword="null"/> or empty; its directory must exist.</param>
    /// <returns><paramref name="path"/>, which now holds the Mermaid source.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="path"/> is empty.</exception>
    /// <exception cref="TechnicalError">The file cannot be written.</exception>
    public string ExportAsMermaid(string path) => Export(path, ToMermaid());

    /// <summary>
    /// <c>export as d2(path)</c>: renders the report as D2 and writes it to <paramref name="path"/>.
    /// </summary>
    /// <param name="path">The file to write. Must not be <see langword="null"/> or empty; its directory must exist.</param>
    /// <returns><paramref name="path"/>, which now holds the D2 source.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="path"/> is empty.</exception>
    /// <exception cref="TechnicalError">The file cannot be written.</exception>
    public string ExportAsD2(string path) => Export(path, ToD2());

    /// <summary>
    /// <c>export as csv(path)</c>: renders the report as CSV and writes it to <paramref name="path"/>.
    /// </summary>
    /// <param name="path">The file to write. Must not be <see langword="null"/> or empty; its directory must exist.</param>
    /// <returns><paramref name="path"/>, which now holds the CSV text.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="path"/> is empty.</exception>
    /// <exception cref="TechnicalError">The file cannot be written.</exception>
    public string ExportAsCsv(string path) => Export(path, ToCsv());

    /// <summary>
    /// <c>export as json(path)</c>: renders the report as JSON and writes it to <paramref name="path"/>.
    /// </summary>
    /// <param name="path">The file to write. Must not be <see langword="null"/> or empty; its directory must exist.</param>
    /// <returns><paramref name="path"/>, which now holds the JSON document.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="path"/> is empty.</exception>
    /// <exception cref="TechnicalError">The file cannot be written.</exception>
    public string ExportAsJson(string path) => Export(path, ToJson());

    /// <summary>
    /// <c>export as html(path)</c>: renders the report as self-contained HTML and writes it to
    /// <paramref name="path"/>.
    /// </summary>
    /// <param name="path">The file to write. Must not be <see langword="null"/> or empty; its directory must exist.</param>
    /// <returns><paramref name="path"/>, which now holds the HTML document.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="path"/> is empty.</exception>
    /// <exception cref="TechnicalError">The file cannot be written.</exception>
    public string ExportAsHtml(string path) => Export(path, ToHtml());

    private static string Export(string path, string content)
    {
        ArgumentNullException.ThrowIfNull(path);
        if (path.Length == 0)
        {
            throw new ArgumentException("Path must not be empty.", nameof(path));
        }

        try
        {
            File.WriteAllText(path, content);
            return path;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            throw new TechnicalError($"Failed to export the graph report to '{path}'.", exception);
        }
    }
}
