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
/// selection — with its mood flag where the predicate exists in both moods — to the shared assertion
/// in <see cref="FilesAssertion"/>, which is the single place a files rule's outcome is computed. The
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

    /// <summary>
    /// <c>should have name</c>: every selected file must have a name matching <paramref name="glob"/>,
    /// where the name is the file's identifier with no directory part, so a file identified by
    /// <c>src/Models/Car.cs</c> has the name <c>Car.cs</c>. A selected file whose name does not match
    /// is reported as one <see cref="FileViolation"/>, and the empty-test guard reports a selection
    /// that matched nothing.
    /// </summary>
    /// <param name="glob">The glob to match each selected file's name against. Must not be <see langword="null"/> or empty.</param>
    /// <returns>The rule's terminal, checked with <see cref="ICheckable.Check(CheckOptions?)"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="glob"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="glob"/> is empty.</exception>
    public ICheckable HaveName(string glob) =>
        new FilesNameRule(_files, new Filter(new Pattern(glob), MatchTarget.Filename), negate: false);

    /// <summary>
    /// <c>should be in folder</c>: every selected file must sit in a folder matching
    /// <paramref name="glob"/>. The folder is the file's identifier with its name removed, so a file
    /// identified by <c>src/Models/Car.cs</c> sits in the folder <c>src/Models</c>. A selected file
    /// whose folder does not match is reported as one <see cref="FileViolation"/>, and the empty-test
    /// guard reports a selection that matched nothing.
    /// </summary>
    /// <param name="glob">The glob to match each selected file's folder against. Must not be <see langword="null"/> or empty.</param>
    /// <returns>The rule's terminal, checked with <see cref="ICheckable.Check(CheckOptions?)"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="glob"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="glob"/> is empty.</exception>
    public ICheckable BeInFolder(string glob) =>
        new FilesFolderRule(_files, new Filter(new Pattern(glob), MatchTarget.PathWithoutFilename), negate: false);

    /// <summary>
    /// <c>should be in path</c>: every selected file must be at a path matching <paramref name="glob"/>.
    /// The path is the file's project-relative identifier, folders and name together, so a file
    /// identified by <c>src/Models/Car.cs</c> is at the path <c>src/Models/Car.cs</c>. A selected file
    /// whose path does not match is reported as one <see cref="FileViolation"/>, and the empty-test
    /// guard reports a selection that matched nothing.
    /// </summary>
    /// <param name="glob">The glob to match each selected file's whole path against. Must not be <see langword="null"/> or empty.</param>
    /// <returns>The rule's terminal, checked with <see cref="ICheckable.Check(CheckOptions?)"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="glob"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="glob"/> is empty.</exception>
    public ICheckable BeInPath(string glob) =>
        new FilesPathRule(_files, new Filter(new Pattern(glob), MatchTarget.Path), negate: false);

    /// <summary>
    /// <c>should depend on files</c>: every selected file must depend on at least one file that
    /// matches every object selector applied to the returned object. A selected file that depends on
    /// none is reported as one <see cref="FileViolation"/>, and the empty-test guard reports a rule
    /// whose selection or object matched nothing.
    /// </summary>
    /// <returns>The rule's object and terminal, checked with <see cref="ICheckable.Check(CheckOptions?)"/>.</returns>
    public DependOn DependOn() => new(_files, negate: false);

    /// <summary>
    /// <c>should depend on external modules</c>: every selected file must depend on at least one
    /// external module whose name matches any selector applied to the returned object. An external
    /// module is the target of an external edge: a name no file in the project declares, kept as
    /// written — <c>System.Linq</c> for <c>using System.Linq;</c>. A selected file that depends on
    /// none is reported as one <see cref="FileViolation"/>, and the empty-test guard reports a rule
    /// whose selection or object matched nothing.
    /// </summary>
    /// <returns>The rule's object and terminal, checked with <see cref="ICheckable.Check(CheckOptions?)"/>.</returns>
    public DependOnExternalModules DependOnExternalModules() => new(_files, negate: false);

    /// <summary>
    /// <c>should adhere to</c>: every selected file must satisfy <paramref name="predicate"/>. The
    /// predicate receives one <see cref="FileDetail"/> per selected file — its project-relative path,
    /// name without extension, extension, directory, full source text and non-blank line count — and
    /// must return <see langword="true"/> for the file to pass. A selected file the predicate rejects
    /// is reported as one <see cref="AdhereToViolation"/> carrying <paramref name="message"/>, and
    /// the empty-test guard reports a selection that matched nothing.
    /// </summary>
    /// <param name="predicate">The custom predicate; must not be <see langword="null"/>.</param>
    /// <param name="message">The rule's description, reported with each violation; must not be <see langword="null"/> or empty.</param>
    /// <returns>The rule's terminal, checked with <see cref="ICheckable.Check(CheckOptions?)"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="predicate"/> or <paramref name="message"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="message"/> is empty.</exception>
    public ICheckable AdhereTo(Func<FileDetail, bool> predicate, string message) =>
        new FilesAdhereToRule(_files, predicate, message, negate: false);

    /// <summary>
    /// <c>should have no cycles</c>: the projected dependency graph of the selected files must be
    /// acyclic. Each cycle the selection forms is reported as one <see cref="CycleViolation"/>, and
    /// the empty-test guard reports a selection that matched nothing. This predicate exists only in
    /// the positive mood.
    /// </summary>
    /// <returns>The rule's terminal, checked with <see cref="ICheckable.Check(CheckOptions?)"/>.</returns>
    public ICheckable HaveNoCycles() => new FilesCyclesRule(_files);
}
