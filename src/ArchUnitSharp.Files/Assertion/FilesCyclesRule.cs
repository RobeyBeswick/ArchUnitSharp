namespace ArchUnitSharp.Files.Assertion;

using ArchUnitSharp.Common.Extraction;

/// <summary>
/// The terminal of a <c>should have no cycles</c> files rule, returned by
/// <see cref="Should.HaveNoCycles"/>. Checking it runs the shared <see cref="FilesAssertion.Cycles"/>
/// assertion, which routes an empty selection through the shared <see cref="EmptyTestGuard"/>.
/// </summary>
/// <remarks>
/// <para>
/// This type is immutable and safe for concurrent use: its field never changes, and a check only
/// reads it. A rule can therefore be stored, checked several times and shared across threads.
/// </para>
/// </remarks>
internal sealed class FilesCyclesRule : ICheckable
{
    private readonly Files _files;

    /// <summary>
    /// Creates the terminal of a <c>should have no cycles</c> rule.
    /// </summary>
    /// <param name="files">The selection the rule asserts over.</param>
    public FilesCyclesRule(Files files) => _files = files;

    /// <inheritdoc/>
    public IReadOnlyList<Violation> Check(CheckOptions? options = null) =>
        CheckLogging.Run(options, logger => FilesAssertion.Cycles(_files, options, logger));

    /// <inheritdoc/>
    void ICheckable.ProhibitExternalImplementation()
    {
    }
}
