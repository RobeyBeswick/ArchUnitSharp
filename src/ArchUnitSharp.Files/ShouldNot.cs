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

    /// <summary>
    /// <c>should not have name</c>: no selected file may have a name matching
    /// <paramref name="glob"/>. A selected file whose name matches is reported as one
    /// <see cref="FileViolation"/>, and the empty-test guard reports a selection that matched
    /// nothing.
    /// </summary>
    /// <param name="glob">The glob to match each selected file's name against. Must not be <see langword="null"/> or empty.</param>
    /// <returns>The rule's terminal, checked with <see cref="ICheckable.Check(CheckOptions?)"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="glob"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="glob"/> is empty.</exception>
    public ICheckable HaveName(string glob) =>
        new FilesNameRule(_files, new Filter(new Pattern(glob), MatchTarget.Filename), negate: true);

    /// <summary>
    /// <c>should not be in folder</c>: no selected file may sit in a folder matching
    /// <paramref name="glob"/>. A selected file whose folder matches is reported as one
    /// <see cref="FileViolation"/>, and the empty-test guard reports a selection that matched
    /// nothing.
    /// </summary>
    /// <param name="glob">The glob to match each selected file's folder against. Must not be <see langword="null"/> or empty.</param>
    /// <returns>The rule's terminal, checked with <see cref="ICheckable.Check(CheckOptions?)"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="glob"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="glob"/> is empty.</exception>
    public ICheckable BeInFolder(string glob) =>
        new FilesFolderRule(_files, new Filter(new Pattern(glob), MatchTarget.PathWithoutFilename), negate: true);

    /// <summary>
    /// <c>should not be in path</c>: no selected file may be at a path matching
    /// <paramref name="glob"/>. A selected file whose path matches is reported as one
    /// <see cref="FileViolation"/>, and the empty-test guard reports a selection that matched
    /// nothing.
    /// </summary>
    /// <param name="glob">The glob to match each selected file's whole path against. Must not be <see langword="null"/> or empty.</param>
    /// <returns>The rule's terminal, checked with <see cref="ICheckable.Check(CheckOptions?)"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="glob"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="glob"/> is empty.</exception>
    public ICheckable BeInPath(string glob) =>
        new FilesPathRule(_files, new Filter(new Pattern(glob), MatchTarget.Path), negate: true);

    /// <summary>
    /// <c>should not depend on files</c>: no selected file may depend on any file that matches every
    /// object selector applied to the returned object. Each offending dependency is reported as one
    /// <see cref="DependencyViolation"/>, and the empty-test guard reports a rule whose selection or
    /// object matched nothing.
    /// </summary>
    /// <returns>The rule's object and terminal, checked with <see cref="ICheckable.Check(CheckOptions?)"/>.</returns>
    public DependOn DependOn() => new(_files, negate: true);
}
