namespace ArchUnitSharp.Files;

using ArchUnitSharp.Common.Extraction;
using ArchUnitSharp.Files.Assertion;

/// <summary>
/// The negated mood of a files rule chain: <c>should not</c>. Built from <see cref="Files.ShouldNot"/>;
/// its predicate methods complete the rule and each returns the terminal that is checked with
/// <see cref="ICheckable.Check(CheckOptions?)"/>.
/// </summary>
/// <remarks>
/// <para>
/// This type is the mood, nothing else: it carries no rule logic. A predicate method forwards the
/// selection and its <see langword="true"/> mood flag to the shared assertion in
/// <see cref="FilesAssertion"/>, which is the single place a files rule's outcome is computed — the
/// negation is the flag, not a separate code path. The positive twin is <see cref="Should"/>; there
/// is no third mood.
/// </para>
/// <para>
/// This type is immutable and safe for concurrent use. Building a rule from it never mutates the
/// selection it was built from, so a <see cref="ShouldNot"/> value can be stored and reused.
/// </para>
/// </remarks>
public sealed class ShouldNot
{
    private readonly Files _files;

    /// <summary>
    /// Creates the negated mood over <paramref name="files"/>. Callers obtain a
    /// <see cref="ShouldNot"/> from <see cref="Files.ShouldNot"/> rather than constructing one.
    /// </summary>
    /// <param name="files">The selection the rule asserts over.</param>
    internal ShouldNot(Files files) => _files = files;

    /// <summary>
    /// <c>should not exist</c>: the selected files must not exist. Every selected file exists by
    /// definition — the selection is drawn from the graph's own nodes — so the rule reports one
    /// <see cref="FileViolation"/> per selected file, and the empty-test guard reports a selection
    /// that matched nothing.
    /// </summary>
    /// <returns>The rule's terminal, checked with <see cref="ICheckable.Check(CheckOptions?)"/>.</returns>
    public ICheckable Exist() => new FilesExistRule(_files, negate: true);
}
