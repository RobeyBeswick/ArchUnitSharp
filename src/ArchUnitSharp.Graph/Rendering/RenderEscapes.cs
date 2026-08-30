namespace ArchUnitSharp.Graph.Rendering;

using System.Text;

/// <summary>
/// The string escaping each graph-report format needs before it embeds an identifier or label: DOT,
/// Mermaid, D2, CSV, JSON and HTML each quote or escape text differently, and each renderer routes
/// every identifier through exactly one of these so a label can never break out of its format's
/// syntax. The escaping is pure and deterministic.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Dot"/> and <see cref="D2"/> escape a double-quoted string, <see cref="Mermaid"/> the
/// entity a Mermaid quoted label needs, <see cref="Csv"/> a CSV field, <see cref="Json"/> a JSON
/// string and <see cref="Html"/> text inside an element. Every method returns a value safe to embed
/// verbatim between its format's quotes or tags.
/// </para>
/// <para>
/// This type is stateless and safe for concurrent use.
/// </para>
/// </remarks>
internal static class RenderEscapes
{
    /// <summary>
    /// Escapes a value for a double-quoted DOT identifier: a backslash becomes two, a double quote
    /// becomes <c>\"</c>.
    /// </summary>
    /// <param name="value">The value to escape. Must not be <see langword="null"/>.</param>
    /// <returns>The value safe to embed in a DOT quoted identifier.</returns>
    public static string Dot(string value) =>
        value.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal);

    /// <summary>
    /// Escapes a value for a Mermaid quoted label: a double quote becomes the <c>#quot;</c> entity.
    /// A backslash is not special inside a Mermaid quoted label, and identifiers are normalised to
    /// forward separators, so nothing else needs to change.
    /// </summary>
    /// <param name="value">The value to escape. Must not be <see langword="null"/>.</param>
    /// <returns>The value safe to embed in a Mermaid quoted label.</returns>
    public static string Mermaid(string value) =>
        value.Replace("\"", "#quot;", StringComparison.Ordinal);

    /// <summary>
    /// Escapes a value for a double-quoted D2 identifier: a backslash becomes two, a double quote
    /// becomes <c>\"</c>.
    /// </summary>
    /// <param name="value">The value to escape. Must not be <see langword="null"/>.</param>
    /// <returns>The value safe to embed in a D2 quoted identifier.</returns>
    public static string D2(string value) =>
        value.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal);

    /// <summary>
    /// Escapes a value as a CSV field: when it contains a comma, a double quote or a line break it is
    /// returned wrapped in double quotes with every internal quote doubled; otherwise it is returned
    /// unchanged.
    /// </summary>
    /// <param name="value">The value to escape. Must not be <see langword="null"/>.</param>
    /// <returns>The value safe to embed as one CSV field.</returns>
    public static string Csv(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
        {
            return "\"" + value.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
        }

        return value;
    }

    /// <summary>
    /// Escapes a value as a JSON string: quotes and backslashes are escaped, and every control
    /// character becomes its <c>\b</c>, <c>\f</c>, <c>\n</c>, <c>\r</c>, <c>\t</c> or <c>\uXXXX</c>
    /// escape.
    /// </summary>
    /// <param name="value">The value to escape. Must not be <see langword="null"/>.</param>
    /// <returns>The value safe to embed between JSON string quotes.</returns>
    public static string Json(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (char character in value)
        {
            switch (character)
            {
                case '"':
                    builder.Append("\\\"");
                    break;
                case '\\':
                    builder.Append("\\\\");
                    break;
                case '\b':
                    builder.Append("\\b");
                    break;
                case '\f':
                    builder.Append("\\f");
                    break;
                case '\n':
                    builder.Append("\\n");
                    break;
                case '\r':
                    builder.Append("\\r");
                    break;
                case '\t':
                    builder.Append("\\t");
                    break;
                default:
                    if (char.IsControl(character))
                    {
                        builder.Append("\\u");
                        builder.Append(((int)character).ToString("x4", System.Globalization.CultureInfo.InvariantCulture));
                    }
                    else
                    {
                        builder.Append(character);
                    }

                    break;
            }
        }

        return builder.ToString();
    }

    /// <summary>
    /// Escapes a value as text inside an HTML element: the five characters a browser parses —
    /// <c>&amp;</c>, <c>&lt;</c>, <c>&gt;</c>, <c>&quot;</c> and <c>&#39;</c> — become their entities.
    /// </summary>
    /// <param name="value">The value to escape. Must not be <see langword="null"/>.</param>
    /// <returns>The value safe to embed as HTML text.</returns>
    public static string Html(string value) =>
        value.Replace("&", "&amp;", StringComparison.Ordinal)
            .Replace("<", "&lt;", StringComparison.Ordinal)
            .Replace(">", "&gt;", StringComparison.Ordinal)
            .Replace("\"", "&quot;", StringComparison.Ordinal)
            .Replace("'", "&#39;", StringComparison.Ordinal);
}
