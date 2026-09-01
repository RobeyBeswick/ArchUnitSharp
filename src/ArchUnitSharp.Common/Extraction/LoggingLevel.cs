namespace ArchUnitSharp.Common.Extraction;

/// <summary>
/// How much a check logs while it runs, carried by <see cref="CheckOptions.Logging"/>. The zero
/// value, <see cref="None"/>, is the default and means a check logs nothing. A check records a line
/// only when the line's level is at or above the threshold the option names, so
/// <see cref="Debug"/> records everything and <see cref="Error"/> records only error-level lines.
/// </summary>
/// <remarks>
/// <para>
/// The levels are a ladder from lowest to highest: <see cref="Debug"/>, <see cref="Info"/>,
/// <see cref="Warn"/>, <see cref="Error"/>. The fixed check vocabulary maps onto it: a check's start
/// and end and each metric it measures are <see cref="Info"/> lines, its progress is a
/// <see cref="Debug"/> line, and each violation it reports is a <see cref="Warn"/> line. Nothing in
/// the fixed vocabulary logs at <see cref="Error"/>: a technical failure of a check is raised as a
/// <see cref="TechnicalError"/> exception rather than logged, so an error-only threshold records
/// nothing from a check.
/// </para>
/// </remarks>
public enum LoggingLevel
{
    /// <summary>The check logs nothing.</summary>
    None = 0,

    /// <summary>Low-level progress lines: what a check reads, selects and computes, for debugging a rule.</summary>
    Debug = 1,

    /// <summary>Ordinary check events: a check starting and ending, and each metric it measures.</summary>
    Info = 2,

    /// <summary>Rule outcomes that are failures: each violation a check reports.</summary>
    Warn = 3,

    /// <summary>Technical failures of the check itself; the fixed check vocabulary records nothing at this level.</summary>
    Error = 4,
}
