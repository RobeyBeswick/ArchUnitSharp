namespace ArchUnitSharp.Files.Assertion;

using ArchUnitSharp.Common.Extraction;

/// <summary>
/// The terminal of a <c>should have name</c> / <c>should not have name</c> files rule, returned by
/// <see cref="Should.HaveName"/> and <see cref="ShouldNot.HaveName"/>. Checking it runs the shared
/// <see cref="FilesAssertion.HaveName"/> assertion with the mood flag the rule was built with, which
/// is where the empty-test guard lives.
/// </summary>
/// <remarks>
/// <para>
/// This type is immutable and safe for concurrent use: its fields never change, and a check only
/// reads them. A rule can therefore be stored, checked several times and shared across threads.
/// </para>
/// </remarks>
internal sealed class FilesNameRule : ICheckable
{
    private readonly Files _files;
    private readonly Filter _filter;
    private readonly bool _negate;

    /// <summary>
    /// Creates the terminal of a <c>should have name</c> / <c>should not have name</c> rule.
    /// </summary>
    /// <param name="files">The selection the rule asserts over.</param>
    /// <param name="filter">The rule's glob compiled to a name filter; each selected file's name is matched against it.</param>
    /// <param name="negate">
    /// <see langword="false"/> for the positive mood, <see langword="true"/> for the negated mood.
    /// </param>
    public FilesNameRule(Files files, Filter filter, bool negate)
    {
        _files = files;
        _filter = filter;
        _negate = negate;
    }

    /// <inheritdoc/>
    public IReadOnlyList<Violation> Check(CheckOptions? options = null) =>
        FilesAssertion.HaveName(_files, _filter, _negate, options);

    /// <inheritdoc/>
    void ICheckable.ProhibitExternalImplementation()
    {
    }
}
