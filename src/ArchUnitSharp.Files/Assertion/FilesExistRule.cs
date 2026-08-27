namespace ArchUnitSharp.Files.Assertion;

using ArchUnitSharp.Common.Extraction;

/// <summary>
/// The terminal of a <c>should exist</c> / <c>should not exist</c> files rule, returned by
/// <see cref="Should.Exist"/> and <see cref="ShouldNot.Exist"/>. Checking it runs the shared
/// <see cref="FilesAssertion.Exist"/> assertion with the mood flag the rule was built with, which
/// routes an empty selection through the shared <see cref="EmptyTestGuard"/>.
/// </summary>
/// <remarks>
/// <para>
/// This type is immutable and safe for concurrent use: its two fields never change, and a check only
/// reads them. A rule can therefore be stored, checked several times and shared across threads.
/// </para>
/// </remarks>
internal sealed class FilesExistRule : ICheckable
{
    private readonly Files _files;
    private readonly bool _negate;

    /// <summary>
    /// Creates the terminal of an existence rule.
    /// </summary>
    /// <param name="files">The selection the rule asserts over.</param>
    /// <param name="negate">
    /// <see langword="false"/> for the positive mood, <see langword="true"/> for the negated mood.
    /// </param>
    public FilesExistRule(Files files, bool negate)
    {
        _files = files;
        _negate = negate;
    }

    /// <inheritdoc/>
    public IReadOnlyList<Violation> Check(CheckOptions? options = null) =>
        FilesAssertion.Exist(_files, _negate, options);

    /// <inheritdoc/>
    void ICheckable.ProhibitExternalImplementation()
    {
    }
}
