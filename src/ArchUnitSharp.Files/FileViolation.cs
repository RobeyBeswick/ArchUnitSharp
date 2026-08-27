namespace ArchUnitSharp.Files;

using ArchUnitSharp.Common.Extraction;

/// <summary>
/// A violation produced by a files rule predicate: the checked value — a file identifier — did not
/// satisfy the rule. A <c>should not exist</c> rule reports one of these per selected file, carrying
/// the file that exists although the rule says it must not.
/// </summary>
/// <remarks>
/// <para>
/// The violation carries the offending file and nothing else; the meaning is supplied by the rule
/// that produced it. It carries <see cref="ViolationKind.Rule"/>, the same kind the files module's
/// other predicate violation, <see cref="CycleViolation"/>, carries.
/// </para>
/// <para>
/// This type is immutable and value-semantic; two violations with the same file are equal.
/// </para>
/// </remarks>
public sealed record FileViolation : Violation
{
    private readonly string _file;

    /// <summary>
    /// The file that violated the rule. Must not be <see langword="null"/> or empty; both the
    /// constructor and a <see langword="with"/> expression route through the same validation, so
    /// neither can introduce a bad value.
    /// </summary>
    public string File
    {
        get => _file;
        init => _file = Require(value);
    }

    /// <summary>
    /// Creates a violation for the file that violated a files rule.
    /// </summary>
    /// <param name="file">The offending file; must not be <see langword="null"/> or empty.</param>
    /// <exception cref="ArgumentNullException"><paramref name="file"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="file"/> is empty.</exception>
    public FileViolation(string file)
        : base(ViolationKind.Rule)
    {
        _file = Require(file);
    }

    private static string Require(string file) =>
        file is null
            ? throw new ArgumentNullException(nameof(File))
            : file.Length == 0
                ? throw new ArgumentException("File must not be empty.", nameof(File))
                : file;
}
