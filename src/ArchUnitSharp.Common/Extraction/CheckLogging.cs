namespace ArchUnitSharp.Common.Extraction;

/// <summary>
/// The shared logging wrapper every rule terminal checks through: it creates the check's
/// <see cref="CheckLogger"/> from the options the check was given, hands it to the assertion that
/// computes the rule's outcome, records the check's end and flushes the buffered lines — writing the
/// log file, when one is configured — after the check. Options are threaded by parameter rather than
/// read from ambient state, so a check's logging is fully determined by its own options and
/// concurrent checks never share a logger.
/// </summary>
/// <remarks>
/// <para>
/// A terminal's <c>Check</c> is one call to <see cref="Run"/>, so every terminal reaches the same
/// start/end/violation logging and the same file-flush boundary; the assertion the terminal delegates
/// to emits the events that name the rule and its progress. A check that throws still flushes what it
/// logged before the failure.
/// </para>
/// <para>
/// This type is stateless and safe for concurrent use.
/// </para>
/// </remarks>
internal static class CheckLogging
{
    /// <summary>
    /// Runs a check with logging: creates the logger, delegates to <paramref name="check"/>, records
    /// the check's end and flushes the log. Returns whatever <paramref name="check"/> returned.
    /// </summary>
    /// <param name="options">The options to check with; <see langword="null"/> means the defaults in <see cref="CheckOptions"/>.</param>
    /// <param name="check">The check to run, handed the logger to emit events through.</param>
    /// <returns>The violations the check found; empty when the rule passed.</returns>
    public static IReadOnlyList<Violation> Run(
        CheckOptions? options,
        Func<CheckLogger, IReadOnlyList<Violation>> check)
    {
        CheckLogger logger = CheckLogger.Create(options);
        try
        {
            IReadOnlyList<Violation> violations = check(logger);
            logger.EndCheck(violations.Count);
            return violations;
        }
        finally
        {
            logger.Flush();
        }
    }
}
