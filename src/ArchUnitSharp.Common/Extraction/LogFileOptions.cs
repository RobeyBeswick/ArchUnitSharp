namespace ArchUnitSharp.Common.Extraction;

/// <summary>
/// The optional file output of a check's log, carried by <see cref="CheckOptions.LogFile"/> so a CI
/// run can archive the log as a build artifact. When set, a check writes its log lines to one file in
/// <see cref="Directory"/>, creating the directory when it does not exist. The file's name is
/// <see cref="FileNamePrefix"/> plus a UTC timestamp — <c>"archunit"</c> and a check at
/// <c>2026-08-31T21:46:30Z</c> produce <c>archunit-20260831-214630.log</c> — so each check's log is
/// distinct. <see cref="Append"/> decides what happens when the file already exists: a second check in
/// the same timestamped second appends its lines or replaces the file.
/// </summary>
/// <remarks>
/// <para>
/// The file output is purely opt-in: when <see cref="CheckOptions.LogFile"/> is <see langword="null"/>,
/// a check writes its log nowhere. This type is immutable and value-semantic — two options with the
/// same values are equal — and sharing one instance across concurrent checks is safe.
/// </para>
/// </remarks>
public sealed record LogFileOptions
{
    private readonly string _directory = ".";
    private readonly string _fileNamePrefix = "archunit";

    /// <summary>
    /// The directory the log file is written to. The directory is created when it does not exist.
    /// Defaults to the current directory.
    /// </summary>
    public string Directory
    {
        get => _directory;
        init => _directory = RequireDirectory(value);
    }

    /// <summary>
    /// The log file name's prefix, with the UTC timestamp and the <c>.log</c> extension appended.
    /// Defaults to <c>"archunit"</c>.
    /// </summary>
    public string FileNamePrefix
    {
        get => _fileNamePrefix;
        init => _fileNamePrefix = RequirePrefix(value);
    }

    /// <summary>
    /// When <see langword="true"/>, log lines are appended to the file when it already exists; when
    /// <see langword="false"/> (the default), the file is replaced. Two checks in the same timestamped
    /// second with <see cref="Append"/> set to <see langword="true"/> therefore merge their logs, while
    /// with the default the later check's log replaces the earlier one's.
    /// </summary>
    public bool Append { get; init; }

    private static string RequireDirectory(string directory) =>
        directory is null
            ? throw new ArgumentNullException(nameof(Directory))
            : directory.Length == 0
                ? throw new ArgumentException("Log directory must not be empty.", nameof(Directory))
                : directory;

    private static string RequirePrefix(string prefix) =>
        prefix is null
            ? throw new ArgumentNullException(nameof(FileNamePrefix))
            : prefix.Length == 0
                ? throw new ArgumentException("Log file name prefix must not be empty.", nameof(FileNamePrefix))
                : prefix;
}
