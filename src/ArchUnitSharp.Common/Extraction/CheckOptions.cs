namespace ArchUnitSharp.Common.Extraction;

/// <summary>
/// The options bag passed to <see cref="ICheckable.Check"/>: what the empty-test guard allows, how
/// much a check logs, whether the extraction cache is bypassed, and the C#-specific analysis
/// toggles. A single bag with defaults; <see langword="null"/> at the call site means these
/// defaults.
/// </summary>
/// <remarks>
/// <para>
/// Every property defaults to the least surprising value for a rule run: the empty-test guard is on
/// (<see cref="AllowEmptyTests"/> is <see langword="false"/>), logging is off
/// (<see cref="Logging"/> is <see cref="LoggingLevel.None"/>), the cache is used
/// (<see cref="ClearCache"/> is <see langword="false"/>), and every analysis toggle is off.
/// </para>
/// <para>
/// This type is immutable and value-semantic: a check never mutates the bag it was given, and two
/// bags with the same values are equal. Sharing one instance across concurrent checks is safe.
/// </para>
/// </remarks>
public sealed record CheckOptions
{
    /// <summary>
    /// When <see langword="true"/>, a rule that matches nothing is allowed to pass. When
    /// <see langword="false"/> (the default), a rule that matches nothing is a violation
    /// (<see cref="EmptyTestViolation"/>).
    /// </summary>
    public bool AllowEmptyTests { get; init; }

    /// <summary>
    /// How much a check logs while it runs. <see cref="LoggingLevel.None"/> (the default) means no
    /// logging at all.
    /// </summary>
    public LoggingLevel Logging { get; init; }

    /// <summary>
    /// When <see langword="true"/>, the extraction cache is bypassed so the graph is rebuilt from
    /// source. When <see langword="false"/> (the default), a previously extracted graph is reused
    /// while it is still valid.
    /// </summary>
    public bool ClearCache { get; init; }

    /// <summary>
    /// When <see langword="true"/>, files in test folders are excluded from the analysis. When
    /// <see langword="false"/> (the default), they are included.
    /// </summary>
    public bool IgnoreTestCode { get; init; }

    /// <summary>
    /// When <see langword="true"/>, generated source files (such as <c>*.g.cs</c> and
    /// <c>*.designer.cs</c>) are excluded from the analysis. When <see langword="false"/> (the
    /// default), they are included.
    /// </summary>
    public bool IgnoreGeneratedCode { get; init; }
}
