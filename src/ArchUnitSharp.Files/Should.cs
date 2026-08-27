namespace ArchUnitSharp.Files;

using ArchUnitSharp.Common.Extraction;
using ArchUnitSharp.Files.Assertion;

/// <summary>
/// The positive mood of a files rule chain: <c>should</c>. Built from <see cref="Files.Should"/>; its
/// predicate methods complete the rule and each returns the terminal that is checked with
/// <see cref="ICheckable.Check(CheckOptions?)"/>.
/// </summary>
/// <remarks>
/// <para>
/// This type is the mood, nothing else: it carries no rule logic. A predicate method forwards the
/// selection and its <see langword="false"/> mood flag to the shared assertion in
/// <see cref="FilesAssertion"/>, which is the single place a files rule's outcome is computed. The
/// negated twin is <see cref="ShouldNot"/>; there is no third mood.
/// </para>
/// <para>
/// This type is immutable and safe for concurrent use. Building a rule from it never mutates the
/// selection it was built from, so a <see cref="Should"/> value can be stored and reused.
/// </para>
/// </remarks>
public sealed class Should
{
    private readonly Files _files;

    /// <summary>
    /// Creates the positive mood over <paramref name="files"/>. Callers obtain a
    /// <see cref="Should"/> from <see cref="Files.Should"/> rather than constructing one.
    /// </summary>
    /// <param name="files">The selection the rule asserts over.</param>
    internal Should(Files files) => _files = files;

    /// <summary>
    /// <c>should exist</c>: the selected files must exist. Because the selection is drawn from the
    /// graph's own nodes, every selected file exists, so the rule passes for a non-empty selection
    /// and the empty-test guard reports a selection that matched nothing.
    /// </summary>
    /// <returns>The rule's terminal, checked with <see cref="ICheckable.Check(CheckOptions?)"/>.</returns>
    public ICheckable Exist() => new FilesExistRule(_files, negate: false);
}
