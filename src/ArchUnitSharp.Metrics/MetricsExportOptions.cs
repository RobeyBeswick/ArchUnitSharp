namespace ArchUnitSharp.Metrics;

/// <summary>
/// The options bag for one metrics HTML report: the report's title, whether it carries a timestamp,
/// and the custom stylesheet it is styled with. A single bag with defaults; <see langword="null"/> at
/// the call site means these defaults.
/// </summary>
/// <remarks>
/// <para>
/// Every property defaults to the least surprising value for a report: the title is
/// <c>ArchUnitSharp Metrics Report</c>, a UTC timestamp is included (<see cref="IncludeTimestamp"/>
/// is <see langword="true"/>), and no custom stylesheet is applied (<see cref="CustomCss"/> is
/// <see langword="null"/>), so the report uses the built-in stylesheet. The title must be a non-empty
/// string and the custom stylesheet, when given, a non-empty string — both the constructor and a
/// <see langword="with"/> expression route through the same validation, so neither can introduce a bad
/// value.
/// </para>
/// <para>
/// This type is immutable and value-semantic: a report never mutates the bag it was given, and two
/// bags with the same values are equal. Sharing one instance across concurrent exports is safe.
/// </para>
/// </remarks>
public sealed record MetricsExportOptions
{
    private readonly string _title;
    private readonly string? _customCss;

    /// <summary>
    /// The report's title, shown as the page's <c>&lt;h1&gt;</c> and <c>&lt;title&gt;</c>. Must not be
    /// <see langword="null"/> or empty; both the constructor and a <see langword="with"/> expression
    /// route through the same validation, so neither can introduce a bad value.
    /// </summary>
    public string Title
    {
        get => _title;
        init => _title = Require(value, nameof(Title));
    }

    /// <summary>
    /// When <see langword="true"/> (the default), the report shows a <c>Generated:</c> line with the
    /// UTC instant the report was rendered. When <see langword="false"/>, the line is omitted, which
    /// makes a report's text stable across runs.
    /// </summary>
    public bool IncludeTimestamp { get; init; } = true;

    /// <summary>
    /// The stylesheet the report is styled with, replacing the built-in one. When
    /// <see langword="null"/> (the default), the built-in stylesheet is used. Must not be empty when
    /// given; both the constructor and a <see langword="with"/> expression route through the same
    /// validation, so neither can introduce a bad value.
    /// </summary>
    public string? CustomCss
    {
        get => _customCss;
        init => _customCss = Optional(value, nameof(CustomCss));
    }

    /// <summary>
    /// Creates the default options bag: the default title, a timestamp included and no custom
    /// stylesheet.
    /// </summary>
    public MetricsExportOptions()
    {
        _title = "ArchUnitSharp Metrics Report";
        _customCss = null;
    }

    private static string Require(string value, string propertyName) =>
        value is null
            ? throw new ArgumentNullException(propertyName)
            : value.Length == 0
                ? throw new ArgumentException($"{propertyName} must not be empty.", propertyName)
                : value;

    private static string? Optional(string? value, string propertyName) =>
        value is null || value.Length > 0
            ? value
            : throw new ArgumentException($"{propertyName} must not be empty.", propertyName);
}
