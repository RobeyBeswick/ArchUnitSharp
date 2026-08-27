namespace ArchUnitSharp.Files;

using ArchUnitSharp.Common.Extraction;

/// <summary>
/// A violation produced by an <c>adhere to</c> files rule — <c>should adhere to</c> or
/// <c>should not adhere to</c>: a selected file whose custom predicate's verdict contradicts the mood.
/// Carries the offending file and the rule's message, and nothing else.
/// </summary>
/// <remarks>
/// <para>
/// The <c>adhere to</c> predicate reports one of these per selected file whose verdict does not match
/// the mood — a file the positive mood's predicate rejects, or a file the negated mood's predicate
/// accepts. <see cref="File"/> is the offending file (one of the rule's selected files) and
/// <see cref="Message"/> is the rule's caller-supplied message, the data a report prints to say why the
/// predicate was written. It carries <see cref="ViolationKind.Rule"/>, the same kind the files
/// module's other predicate violations carry.
/// </para>
/// <para>
/// This type is immutable and value-semantic; two violations with the same file and message are equal.
/// </para>
/// </remarks>
public sealed record AdhereToViolation : Violation
{
    private readonly string _file;
    private readonly string _message;

    /// <summary>
    /// The file that violated the rule. Must not be <see langword="null"/> or empty; both the
    /// constructor and a <see langword="with"/> expression route through the same validation, so
    /// neither can introduce a bad value.
    /// </summary>
    public string File
    {
        get => _file;
        init => _file = Require(value, nameof(File));
    }

    /// <summary>
    /// The rule's caller-supplied message, the description of the custom predicate. Must not be
    /// <see langword="null"/> or empty; both the constructor and a <see langword="with"/> expression
    /// route through the same validation, so neither can introduce a bad value.
    /// </summary>
    public string Message
    {
        get => _message;
        init => _message = Require(value, nameof(Message));
    }

    /// <summary>
    /// Creates a violation for a file whose custom predicate contradicted the rule's mood.
    /// </summary>
    /// <param name="file">The offending file; must not be <see langword="null"/> or empty.</param>
    /// <param name="message">The rule's message; must not be <see langword="null"/> or empty.</param>
    /// <exception cref="ArgumentNullException"><paramref name="file"/> or <paramref name="message"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="file"/> or <paramref name="message"/> is empty.</exception>
    public AdhereToViolation(string file, string message)
        : base(ViolationKind.Rule)
    {
        _file = Require(file, nameof(File));
        _message = Require(message, nameof(Message));
    }

    private static string Require(string value, string propertyName) =>
        value is null
            ? throw new ArgumentNullException(propertyName)
            : value.Length == 0
                ? throw new ArgumentException($"{propertyName} must not be empty.", propertyName)
                : value;
}
