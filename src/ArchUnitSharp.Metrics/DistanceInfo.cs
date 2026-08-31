namespace ArchUnitSharp.Metrics;

using ArchUnitSharp.Common.Extraction;

/// <summary>
/// The subject of a distance metric: one file's static facts — its types and its lines of code —
/// enriched with the project-level dependency couplings the abstractness, instability and coupling
/// metrics are computed from. It is produced by the projection layer, which reads the file's own
/// counts from its extracted <see cref="FileInfo"/> and its couplings from the project's
/// <see cref="Graph"/>.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="AfferentCoupling"/> is the number of distinct internal project files that depend on
/// this file, <see cref="EfferentCoupling"/> the number of distinct internal project files it depends
/// on — both count internal dependencies only, so self-edges, external targets and files outside the
/// project are never couplings. <see cref="ProjectFileCount"/> is the number of files of the whole
/// project, which sizes the coupling factor; both couplings are therefore below it, and
/// <see cref="AbstractTypeCount"/> never exceeds <see cref="TypeCount"/>.
/// </para>
/// <para>
/// This type is immutable and value-semantic; two distance infos with the same file and facts are
/// equal.
/// </para>
/// </remarks>
public sealed record DistanceInfo
{
    private readonly string _file;

    /// <summary>
    /// The file's graph identifier, project-relative. Must not be <see langword="null"/> or empty;
    /// both the constructor and a <see langword="with"/> expression route through the same validation,
    /// so neither can introduce a bad value.
    /// </summary>
    public string File
    {
        get => _file;
        init => _file = Require(value, nameof(File));
    }

    /// <summary>
    /// The number of types the file declares: its classes plus interfaces. Zero for a file with no
    /// types, which makes the file's abstractness zero rather than a division error.
    /// </summary>
    public int TypeCount { get; init; }

    /// <summary>
    /// The number of the file's types that are abstract: its interfaces plus abstract classes. Never
    /// exceeds <see cref="TypeCount"/>.
    /// </summary>
    public int AbstractTypeCount { get; init; }

    /// <summary>
    /// The number of non-blank lines of the file, which sizes the normalised distance's discount.
    /// </summary>
    public int LinesOfCode { get; init; }

    /// <summary>
    /// The number of distinct internal project files that depend on this file.
    /// </summary>
    public int AfferentCoupling { get; init; }

    /// <summary>
    /// The number of distinct internal project files this file depends on.
    /// </summary>
    public int EfferentCoupling { get; init; }

    /// <summary>
    /// The number of files of the whole project. Both couplings are below it, because a file can be
    /// coupled to at most every other project file.
    /// </summary>
    public int ProjectFileCount { get; init; }

    /// <summary>
    /// Creates a distance info for one file.
    /// </summary>
    /// <param name="file">The file's graph identifier; must not be <see langword="null"/> or empty.</param>
    /// <param name="typeCount">The file's type count.</param>
    /// <param name="abstractTypeCount">The file's abstract-type count; must not exceed <paramref name="typeCount"/>.</param>
    /// <param name="linesOfCode">The file's non-blank line count.</param>
    /// <param name="afferentCoupling">The distinct internal files that depend on the file.</param>
    /// <param name="efferentCoupling">The distinct internal files the file depends on.</param>
    /// <param name="projectFileCount">The whole project's file count; must be positive.</param>
    /// <exception cref="ArgumentNullException"><paramref name="file"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="file"/> is empty, a coupling is not below <paramref name="projectFileCount"/>, <paramref name="abstractTypeCount"/> exceeds <paramref name="typeCount"/>, or a count is negative.</exception>
    public DistanceInfo(
        string file,
        int typeCount,
        int abstractTypeCount,
        int linesOfCode,
        int afferentCoupling,
        int efferentCoupling,
        int projectFileCount)
    {
        _file = Require(file, nameof(File));
        ProjectFileCount = RequirePositive(projectFileCount, nameof(ProjectFileCount));
        TypeCount = RequireNonNegative(typeCount, nameof(TypeCount));
        AbstractTypeCount = abstractTypeCount >= 0 && abstractTypeCount <= typeCount
            ? abstractTypeCount
            : throw new ArgumentException(
                $"{nameof(AbstractTypeCount)} must not exceed {nameof(TypeCount)}.",
                nameof(AbstractTypeCount));
        LinesOfCode = RequireNonNegative(linesOfCode, nameof(LinesOfCode));
        AfferentCoupling = RequireCoupling(afferentCoupling, nameof(AfferentCoupling), ProjectFileCount);
        EfferentCoupling = RequireCoupling(efferentCoupling, nameof(EfferentCoupling), ProjectFileCount);
    }

    private static int RequirePositive(int value, string propertyName) =>
        value > 0
            ? value
            : throw new ArgumentException($"{propertyName} must be positive.", propertyName);

    private static int RequireNonNegative(int value, string propertyName) =>
        value >= 0
            ? value
            : throw new ArgumentException($"{propertyName} must not be negative.", propertyName);

    private static int RequireCoupling(int value, string propertyName, int projectFileCount) =>
        value >= 0 && value < projectFileCount
            ? value
            : throw new ArgumentException(
                $"{propertyName} must not exceed the other project files.",
                propertyName);

    private static string Require(string value, string propertyName) =>
        value is null
            ? throw new ArgumentNullException(propertyName)
            : value.Length == 0
                ? throw new ArgumentException($"{propertyName} must not be empty.", propertyName)
                : value;
}
