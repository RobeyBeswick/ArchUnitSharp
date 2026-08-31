namespace ArchUnitSharp.Slices;

using ArchUnitSharp.Common.Extraction;
using ArchUnitSharp.Slices.Uml;

/// <summary>
/// The positive mood of a slices rule chain: <c>should</c>. Built from <see cref="Slices.Should"/>; its
/// predicate method completes the rule and returns a new <see cref="Slices"/> with the rule added,
/// which is the terminal checked with <see cref="ICheckable.Check"/>.
/// </summary>
/// <remarks>
/// <para>
/// This type is the mood, nothing else: it carries no rule logic. A predicate method forwards the
/// policy — with its mood flag where the predicate exists in both moods — to the shared assertion in
/// <see cref="Assertion.SlicesAssertion"/>, which is the single place a slices rule's outcome is
/// computed. The negated twin is <see cref="ShouldNot"/>; there is no third mood.
/// </para>
/// <para>
/// <c>adhere to diagram</c> exists only in this mood, like <c>should have no cycles</c> in the files
/// module: "should not adhere to diagram" is not a sentence an architect would write. Its modifiers,
/// <see cref="IgnoringOrphanSlices"/> and <see cref="IgnoringExternalSlices"/>, are the present
/// participles of the chain — <c>should ignoring external slices adhere to diagram(...)</c> — and
/// affect only the adhere-to-diagram predicates, never <see cref="ContainDependency"/>.
/// </para>
/// <para>
/// This type is immutable and safe for concurrent use. Completing a rule never mutates the policy it
/// was built from, so a <see cref="Should"/> value can be stored and reused.
/// </para>
/// </remarks>
public sealed class Should
{
    private readonly Slices _slices;
    private readonly DiagramAdherenceOptions _diagramOptions;

    /// <summary>
    /// Creates the positive mood over <paramref name="slices"/>. Callers obtain a <see cref="Should"/>
    /// from <see cref="Slices.Should"/> rather than constructing one.
    /// </summary>
    /// <param name="slices">The policy the rule asserts over.</param>
    internal Should(Slices slices)
        : this(slices, DiagramAdherenceOptions.Default)
    {
    }

    private Should(Slices slices, DiagramAdherenceOptions diagramOptions)
    {
        _slices = slices;
        _diagramOptions = diagramOptions;
    }

    /// <summary>
    /// <c>should contain dependency(from, to)</c>: every slice must contain at least one dependency
    /// from a sliced file whose whole path matches <paramref name="fromGlob"/> to a file whose whole
    /// path matches <paramref name="toGlob"/>. A slice that contains none is reported as one
    /// <see cref="MissingDependencyViolation"/>, and the empty-test guard reports a policy whose
    /// slicing matched nothing or whose <c>from</c> or <c>to</c> glob matched no file.
    /// </summary>
    /// <param name="fromGlob">The glob the importing file of the required dependency must match. Must not be <see langword="null"/> or empty.</param>
    /// <param name="toGlob">The glob the imported file of the required dependency must match. Must not be <see langword="null"/> or empty.</param>
    /// <returns>A new policy with the rule asserted; checked with <see cref="ICheckable.Check"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="fromGlob"/> or <paramref name="toGlob"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="fromGlob"/> or <paramref name="toGlob"/> is empty.</exception>
    public Slices ContainDependency(string fromGlob, string toGlob) =>
        _slices.AddRule(new SliceRule(fromGlob, toGlob, negate: false));

    /// <summary>
    /// <c>ignoring orphan slices</c>: the modifier of an <c>adhere to diagram</c> rule that ignores
    /// every dependency whose source or target is a component the diagram does not declare, so slices
    /// the architect did not draw into the diagram are not held to it. Returns a new <see cref="Should"/>;
    /// the current mood is unchanged. Affects only the adhere-to-diagram predicates.
    /// </summary>
    /// <returns>A new mood that ignores orphan slices.</returns>
    public Should IgnoringOrphanSlices() =>
        WithDiagramOptions(_diagramOptions with { IgnoreOrphanSlices = true });

    /// <summary>
    /// <c>ignoring external slices</c>: the modifier of an <c>adhere to diagram</c> rule that ignores
    /// every dependency whose target lies outside the project, so dependencies to external modules are
    /// not held to the diagram. Returns a new <see cref="Should"/>; the current mood is unchanged.
    /// Affects only the adhere-to-diagram predicates.
    /// </summary>
    /// <returns>A new mood that ignores external slices.</returns>
    public Should IgnoringExternalSlices() =>
        WithDiagramOptions(_diagramOptions with { IgnoreExternalSlices = true });

    /// <summary>
    /// <c>should adhere to diagram</c>: every dependency the slicing's actual graph carries between
    /// slices must be one the diagram allows — a dependency between two slices the diagram has no arrow
    /// for is reported as one <see cref="DiagramAdherenceViolation" /> per slice pair, external
    /// dependencies included unless <see cref="IgnoringExternalSlices"/> was applied. The diagram is
    /// parsed now, so a malformed declaration or arrow is a <see cref="UserError"/> naming its line,
    /// and the empty-test guard reports a policy whose slicing matched nothing or whose diagram declares
    /// nothing.
    /// </summary>
    /// <param name="diagram">The diagram text. Must not be <see langword="null"/> or blank.</param>
    /// <returns>A new policy with the rule asserted; checked with <see cref="ICheckable.Check"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="diagram"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="diagram"/> is blank.</exception>
    /// <exception cref="UserError">The diagram contains a malformed declaration or arrow.</exception>
    public Slices AdhereToDiagram(string diagram) =>
        _slices.AddDiagramRule(NewDiagramRule(diagram, "adhere to diagram"));

    /// <summary>
    /// <c>should adhere to diagram in file</c>: like <see cref="AdhereToDiagram"/>, but the diagram is
    /// read from the file at <paramref name="path"/> and parsed immediately. A file that cannot be read
    /// is a <see cref="TechnicalError"/>; a malformed diagram is a <see cref="UserError"/> naming its
    /// line.
    /// </summary>
    /// <param name="path">The file holding the diagram text. Must not be <see langword="null"/> or empty.</param>
    /// <returns>A new policy with the rule asserted; checked with <see cref="ICheckable.Check"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="path"/> is empty.</exception>
    /// <exception cref="TechnicalError">The diagram file cannot be read.</exception>
    /// <exception cref="UserError">The diagram contains a malformed declaration or arrow.</exception>
    public Slices AdhereToDiagramInFile(string path) =>
        _slices.AddDiagramRule(NewDiagramRule(ReadDiagramFile(path), $"adhere to diagram in file '{path}'"));

    private Should WithDiagramOptions(DiagramAdherenceOptions diagramOptions) =>
        new(_slices, diagramOptions);

    private DiagramRule NewDiagramRule(string diagram, string description)
    {
        ArgumentNullException.ThrowIfNull(diagram);
        if (diagram.Trim().Length == 0)
        {
            throw new ArgumentException("Diagram text must not be empty.", nameof(diagram));
        }

        PlantUmlDiagram parsed = PlantUmlParser.Parse(diagram);
        return new DiagramRule(parsed, _diagramOptions, description);
    }

    /// <summary>
    /// The diagram file's disk half: reads one diagram file's full text. A file that cannot be read is
    /// an environment failure and surfaces as a <see cref="TechnicalError"/>, the same treatment
    /// extraction gives an unreadable source file.
    /// </summary>
    private static string ReadDiagramFile(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        if (path.Length == 0)
        {
            throw new ArgumentException("Path must not be empty.", nameof(path));
        }

        try
        {
            return File.ReadAllText(path);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            throw new TechnicalError($"Failed to read the PlantUML diagram from '{path}'.", exception);
        }
    }
}
