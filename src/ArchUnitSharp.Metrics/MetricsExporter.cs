namespace ArchUnitSharp.Metrics;

using System.Globalization;
using ArchUnitSharp.Common.Extraction;
using ArchUnitSharp.Metrics.Rendering;

/// <summary>
/// The metrics module's HTML report exporter: renders a metrics data map — one metric/subject pair
/// per value, as <see cref="MetricsReportData"/> builds it — as a self-contained HTML page with a
/// custom title, an optional UTC timestamp and a custom stylesheet, and writes it to a path whose
/// directory it creates. <see cref="ToHtml"/> returns the page's text; <see cref="ExportAsHtml"/>
/// writes it and returns the path written.
/// </summary>
/// <remarks>
/// <para>
/// A report is a data form, not a rule: it renders the data map it is handed, so an empty map renders
/// an explicit <c>No metric data.</c> page rather than raising a violation, and there is no empty-test
/// guard. The options bag carries the title, the timestamp choice and the custom stylesheet; the
/// timestamp, when included, is the UTC instant the export runs, formatted as
/// <c>yyyy-MM-ddTHH:mm:ssZ</c>. The rendering itself is pure — the file write is the only disk access,
/// and a path that cannot be written surfaces as a <see cref="TechnicalError"/>.
/// </para>
/// <para>
/// <c>export as html(path)</c> on a count, cohesion or distance builder measures the builder's whole
/// metric family and delegates to <see cref="ExportAsHtml(IReadOnlyDictionary{string,string}, string, MetricsExportOptions?)"/>,
/// so this type is where the per-builder terminal and a caller who already has a data map meet.
/// </para>
/// <para>
/// This type is stateless and safe for concurrent use. Every output it produces is deterministic
/// apart from the timestamp, which the options bag can omit.
/// </para>
/// </remarks>
public static class MetricsExporter
{
    /// <summary>
    /// <c>to html()</c>: renders the metrics data map as a self-contained HTML page. The title, the
    /// timestamp and the stylesheet come from <paramref name="options"/>, which defaults to
    /// <c>new MetricsExportOptions()</c> when <see langword="null"/>. An empty map renders an explicit
    /// <c>No metric data.</c> state.
    /// </summary>
    /// <param name="data">The metrics data map to render. Must not be <see langword="null"/>.</param>
    /// <param name="options">The report's options; <see langword="null"/> means the defaults in <see cref="MetricsExportOptions"/>.</param>
    /// <returns>The HTML document.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="data"/> is <see langword="null"/>.</exception>
    public static string ToHtml(IReadOnlyDictionary<string, string> data, MetricsExportOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(data);

        MetricsExportOptions resolved = options ?? new MetricsExportOptions();
        string? timestamp = resolved.IncludeTimestamp
            ? DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture)
            : null;
        return MetricsHtmlRenderer.Render(data, resolved, timestamp);
    }

    /// <summary>
    /// <c>export as html(path)</c>: renders the metrics data map as a self-contained HTML page and
    /// writes it to <paramref name="outputPath"/>, creating the file's directory when it does not
    /// exist. The title, the timestamp and the stylesheet come from <paramref name="options"/>, which
    /// defaults to <c>new MetricsExportOptions()</c> when <see langword="null"/>.
    /// </summary>
    /// <param name="data">The metrics data map to render. Must not be <see langword="null"/>.</param>
    /// <param name="outputPath">The file to write. Must not be <see langword="null"/> or empty.</param>
    /// <param name="options">The report's options; <see langword="null"/> means the defaults in <see cref="MetricsExportOptions"/>.</param>
    /// <returns><paramref name="outputPath"/>, which now holds the HTML document.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="data"/> or <paramref name="outputPath"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="outputPath"/> is empty.</exception>
    /// <exception cref="TechnicalError">The file cannot be written.</exception>
    public static string ExportAsHtml(
        IReadOnlyDictionary<string, string> data,
        string outputPath,
        MetricsExportOptions? options = null)
    {
        string html = ToHtml(data, options);
        return Write(outputPath, html);
    }

    /// <summary>
    /// Writes <paramref name="content"/> to <paramref name="outputPath"/>, creating its directory when
    /// it does not exist, and returns the path. A path that cannot be written is an environment
    /// failure and surfaces as a <see cref="TechnicalError"/>, the same treatment the graph report's
    /// export gives a write failure.
    /// </summary>
    private static string Write(string outputPath, string content)
    {
        ArgumentNullException.ThrowIfNull(outputPath);
        if (outputPath.Length == 0)
        {
            throw new ArgumentException("Path must not be empty.", nameof(outputPath));
        }

        try
        {
            string? directory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(outputPath, content);
            return outputPath;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            throw new TechnicalError($"Failed to export the metrics report to '{outputPath}'.", exception);
        }
    }
}
