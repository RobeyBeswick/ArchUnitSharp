namespace ArchUnitSharp.Common.Extraction;

using System.Globalization;

/// <summary>
/// The fixed-vocabulary logger one check runs with, built from <see cref="CheckOptions"/>: the only
/// things a check may log are that it started (<see cref="StartCheck"/>), that it ended
/// (<see cref="EndCheck"/>), its progress (<see cref="Progress"/>), its violations
/// (<see cref="Violation"/>) and the metrics it measured (<see cref="Metric"/>). A line is recorded
/// only when its level is at or above the threshold <see cref="CheckOptions.Logging"/> names, so the
/// default, <see cref="LoggingLevel.None"/>, records nothing.
/// </summary>
/// <remarks>
/// <para>
/// This type is internal to the kernel and is reached from the terminal of every rule: a terminal
/// creates the logger from the options the check was given through <see cref="CheckLogging.Run"/> and
/// passes it to the shared assertion, which emits the events. The buffering half is pure — the verbs
/// append formatted lines to an in-memory list, so the assertion layer never touches the filesystem —
/// and the file half runs on <see cref="Flush"/>, which the terminal's wrapper invokes after the
/// check. With a <see cref="LogFileOptions"/> configured, the flush writes the buffered lines to a
/// timestamped file, creating its directory; a file that cannot be written surfaces as a
/// <see cref="TechnicalError"/>.
/// </para>
/// <para>
/// A logger is created per check and is never shared, so concurrent checks do not interleave lines.
/// </para>
/// </remarks>
internal sealed class CheckLogger
{
    private readonly LoggingLevel _threshold;
    private readonly List<string> _lines;
    private readonly LogFileOptions? _file;
    private readonly string? _filePath;

    private CheckLogger(LoggingLevel threshold, LogFileOptions? file, string? filePath)
    {
        _threshold = threshold;
        _lines = new List<string>();
        _file = file;
        _filePath = filePath;
    }

    /// <summary>
    /// Creates the logger a check runs with, from the options the check was given. The file path, when
    /// file output is configured, is fixed now: the timestamped name captures the instant the check
    /// started. When no file is configured, no timestamp is taken, so creating a logger that logs
    /// nowhere touches nothing ambient.
    /// </summary>
    /// <param name="options">The options to check with; <see langword="null"/> means the defaults in <see cref="CheckOptions"/>.</param>
    /// <returns>The check's logger.</returns>
    internal static CheckLogger Create(CheckOptions? options)
    {
        CheckOptions resolved = options ?? new CheckOptions();
        LogFileOptions? file = resolved.LogFile;
        string? filePath = file is null
            ? null
            : Path.Combine(file.Directory, $"{file.FileNamePrefix}-{Timestamp(DateTime.UtcNow)}.log");
        return new CheckLogger(resolved.Logging, file, filePath);
    }

    /// <summary>
    /// Creates the logger with an explicit timestamp for the file name, so a test can pin the exact
    /// file a check writes.
    /// </summary>
    /// <param name="options">The options to check with; <see langword="null"/> means the defaults in <see cref="CheckOptions"/>.</param>
    /// <param name="timestamp">The instant the check started, in UTC, used for the timestamped file name.</param>
    /// <returns>The check's logger.</returns>
    internal static CheckLogger Create(CheckOptions? options, DateTime timestamp)
    {
        CheckOptions resolved = options ?? new CheckOptions();
        LogFileOptions? file = resolved.LogFile;
        string? filePath = file is null
            ? null
            : Path.Combine(file.Directory, $"{file.FileNamePrefix}-{Timestamp(timestamp)}.log");
        return new CheckLogger(resolved.Logging, file, filePath);
    }

    /// <summary>
    /// <c>start check</c>: the rule being checked began. An <see cref="LoggingLevel.Info"/> line.
    /// </summary>
    /// <param name="rule">The rule that began, in the words a report would show.</param>
    internal void StartCheck(string rule) => Log(LoggingLevel.Info, $"start check: {rule}");

    /// <summary>
    /// <c>end check</c>: the rule being checked finished, with <paramref name="violationCount"/>
    /// violations. An <see cref="LoggingLevel.Info"/> line.
    /// </summary>
    /// <param name="violationCount">The number of violations the check reported.</param>
    internal void EndCheck(int violationCount) =>
        Log(LoggingLevel.Info, $"end check: {violationCount} violation(s)");

    /// <summary>
    /// <c>log progress</c>: a step of the check, such as how many files it selected. A
    /// <see cref="LoggingLevel.Debug"/> line.
    /// </summary>
    /// <param name="message">The step in prose.</param>
    internal void Progress(string message) => Log(LoggingLevel.Debug, $"progress: {message}");

    /// <summary>
    /// <c>log violation</c>: one violation the rule reported, rendered with its data. A
    /// <see cref="LoggingLevel.Warn"/> line.
    /// </summary>
    /// <param name="violation">The violation to log.</param>
    internal void Violation(Violation violation) => Log(LoggingLevel.Warn, $"violation: {Render(violation)}");

    /// <summary>
    /// <c>log violation</c> for every violation in <paramref name="violations"/>, in order.
    /// </summary>
    /// <param name="violations">The violations to log.</param>
    internal void Violations(IReadOnlyList<Violation> violations)
    {
        foreach (Violation violation in violations)
        {
            Violation(violation);
        }
    }

    /// <summary>
    /// <c>log metric</c>: one metric the check measured, named and valued in the invariant culture. An
    /// <see cref="LoggingLevel.Info"/> line.
    /// </summary>
    /// <param name="name">The metric's name.</param>
    /// <param name="value">The metric's value.</param>
    internal void Metric(string name, double value) =>
        Log(LoggingLevel.Info, $"metric: {name} = {value.ToString(CultureInfo.InvariantCulture)}");

    /// <summary>
    /// The log lines recorded so far, in the order they were recorded. Each access returns a fresh
    /// copy.
    /// </summary>
    internal IReadOnlyList<string> Lines => _lines.ToArray();

    /// <summary>
    /// Writes the buffered lines to the configured file, creating its directory when it does not
    /// exist. With <see cref="LogFileOptions.Append"/> the lines are appended to an existing file;
    /// otherwise the file is replaced. No-op when logging is off or no file is configured. A file that
    /// cannot be written is an environment failure and surfaces as a <see cref="TechnicalError"/>.
    /// </summary>
    internal void Flush()
    {
        if (_file is null || _threshold == LoggingLevel.None || _lines.Count == 0)
        {
            return;
        }

        string content = string.Join(Environment.NewLine, _lines) + Environment.NewLine;
        try
        {
            Directory.CreateDirectory(_file.Directory);
            if (_file.Append)
            {
                File.AppendAllText(_filePath!, content);
            }
            else
            {
                File.WriteAllText(_filePath!, content);
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            throw new TechnicalError($"Failed to write the check log to '{_filePath}'.", exception);
        }
    }

    private void Log(LoggingLevel level, string message)
    {
        if (_threshold == LoggingLevel.None || level < _threshold)
        {
            return;
        }

        _lines.Add($"[{LevelWord(level)}] {message}");
    }

    private static string LevelWord(LoggingLevel level) => level switch
    {
        LoggingLevel.Debug => "DEBUG",
        LoggingLevel.Info => "INFO",
        LoggingLevel.Warn => "WARN",
        LoggingLevel.Error => "ERROR",
        _ => throw new ArgumentOutOfRangeException(
            nameof(level),
            level,
            "Level is not a defined LoggingLevel value."),
    };

    private static string Render(Violation violation) =>
        violation is EmptyTestViolation empty
            ? empty.RuleDescription
            : violation.ToString();

    private static string Timestamp(DateTime utcNow) =>
        utcNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
}
