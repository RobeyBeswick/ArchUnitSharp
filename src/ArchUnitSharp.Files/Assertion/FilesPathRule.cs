namespace ArchUnitSharp.Files.Assertion;

using ArchUnitSharp.Common.Extraction;

/// <summary>
/// The terminal of a <c>should be in path</c> / <c>should not be in path</c> files rule, returned by
/// <see cref="Should.BeInPath"/> and <see cref="ShouldNot.BeInPath"/>. Checking it runs the shared
/// <see cref="FilesAssertion.BeInPath"/> assertion with the mood flag the rule was built with, which
/// routes an empty selection through the shared <see cref="EmptyTestGuard"/>.
/// </summary>
/// <remarks>
/// <para>
/// This type is immutable and safe for concurrent use: its fields never change, and a check only
/// reads them. A rule can therefore be stored, checked several times and shared across threads.
/// </para>
/// </remarks>
internal sealed class FilesPathRule : ICheckable
{
    private readonly Files _files;
    private readonly Filter _filter;
    private readonly bool _negate;

    /// <summary>
    /// Creates the terminal of a <c>should be in path</c> / <c>should not be in path</c> rule.
    /// </summary>
    /// <param name="files">The selection the rule asserts over.</param>
    /// <param name="filter">The rule's glob compiled to a path filter; each selected file's whole path is matched against it.</param>
    /// <param name="negate">
    /// <see langword="false"/> for the positive mood, <see langword="true"/> for the negated mood.
    /// </param>
    public FilesPathRule(Files files, Filter filter, bool negate)
    {
        _files = files;
        _filter = filter;
        _negate = negate;
    }

    /// <inheritdoc/>
    public IReadOnlyList<Violation> Check(CheckOptions? options = null) =>
        FilesAssertion.BeInPath(_files, _filter, _negate, options);

    /// <inheritdoc/>
    void ICheckable.ProhibitExternalImplementation()
    {
    }
}
