namespace ArchUnitSharp.Common.Extraction;

/// <summary>
/// How much a check logs while it runs, carried by <see cref="CheckOptions.Logging"/>. The zero
/// value, <see cref="None"/>, is the default and means a check logs nothing.
/// </summary>
public enum LoggingLevel
{
    /// <summary>The check logs nothing.</summary>
    None = 0,

    /// <summary>The check logs its progress at normal verbosity.</summary>
    Normal = 1,

    /// <summary>The check logs the details of what it reads and decides, for debugging a rule.</summary>
    Verbose = 2,
}
