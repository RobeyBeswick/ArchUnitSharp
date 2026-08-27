namespace ArchUnitSharp.Files.Assertion;

using ArchUnitSharp.Common.Extraction;

/// <summary>
/// The terminal of a <c>should adhere to</c> / <c>should not adhere to</c> files rule, returned by
/// <see cref="Should.AdhereTo"/> and <see cref="ShouldNot.AdhereTo"/>. Checking it runs the shared
/// <see cref="FilesAssertion.AdhereTo"/> assertion with the mood flag the rule was built with, which
/// routes an empty selection through the shared <see cref="EmptyTestGuard"/>.
/// </summary>
/// <remarks>
/// <para>
/// This type is immutable and safe for concurrent use: its fields never change, and a check only
/// reads them. A rule can therefore be stored, checked several times and shared across threads. The
/// custom predicate it carries is invoked once per selected file on every check.
/// </para>
/// </remarks>
internal sealed class FilesAdhereToRule : ICheckable
{
    private readonly Files _files;
    private readonly Func<FileDetail, bool> _predicate;
    private readonly string _message;
    private readonly bool _negate;

    /// <summary>
    /// Creates the terminal of an <c>adhere to</c> rule.
    /// </summary>
    /// <param name="files">The selection the rule asserts over.</param>
    /// <param name="predicate">The rule's custom predicate; each selected file's detail is handed to it. Must not be <see langword="null"/>.</param>
    /// <param name="message">The rule's message, reported with each violation. Must not be <see langword="null"/> or empty.</param>
    /// <param name="negate">
    /// <see langword="false"/> for the positive mood, <see langword="true"/> for the negated mood.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="predicate"/> or <paramref name="message"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="message"/> is empty.</exception>
    public FilesAdhereToRule(Files files, Func<FileDetail, bool> predicate, string message, bool negate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        ArgumentNullException.ThrowIfNull(message);
        if (message.Length == 0)
        {
            throw new ArgumentException("Message must not be empty.", nameof(message));
        }

        _files = files;
        _predicate = predicate;
        _message = message;
        _negate = negate;
    }

    /// <inheritdoc/>
    public IReadOnlyList<Violation> Check(CheckOptions? options = null) =>
        FilesAssertion.AdhereTo(_files, _predicate, _message, _negate, options);

    /// <inheritdoc/>
    void ICheckable.ProhibitExternalImplementation()
    {
    }
}
