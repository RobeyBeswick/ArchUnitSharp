namespace ArchUnitSharp.Metrics;

using ArchUnitSharp.Common.Extraction;

/// <summary>
/// A violation produced by a zone-guard rule: one file whose abstractness/instability point falls in
/// the zone the rule guards against. Carries the data a report needs and nothing else: which file,
/// which zone, and the file's abstractness and instability values, so a report can show where the
/// point fell.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Zone"/> names the zone the file fell into — <see cref="DistanceZone.Pain"/> or
/// <see cref="DistanceZone.Uselessness"/> — and <see cref="Abstractness"/> and
/// <see cref="Instability"/> are the file's two axis values at check time. It carries
/// <see cref="ViolationKind.Rule"/>, the same kind every rule predicate violation carries.
/// </para>
/// <para>
/// This type is immutable and value-semantic; two violations with the same data are equal.
/// </para>
/// </remarks>
public sealed record DistanceZoneViolation : Violation
{
    private readonly string _file;

    /// <summary>
    /// The offending file's graph identifier. Must not be <see langword="null"/> or empty; both the
    /// constructor and a <see langword="with"/> expression route through the same validation, so
    /// neither can introduce a bad value.
    /// </summary>
    public string File
    {
        get => _file;
        init => _file = Require(value, nameof(File));
    }

    /// <summary>
    /// The zone the file's point fell into.
    /// </summary>
    public DistanceZone Zone { get; }

    /// <summary>
    /// The file's abstractness at check time.
    /// </summary>
    public double Abstractness { get; }

    /// <summary>
    /// The file's instability at check time.
    /// </summary>
    public double Instability { get; }

    /// <summary>
    /// Creates a violation for a file whose abstractness/instability point fell in a zone.
    /// </summary>
    /// <param name="file">The offending file's graph identifier; must not be <see langword="null"/> or empty.</param>
    /// <param name="zone">The zone the file fell into.</param>
    /// <param name="abstractness">The file's abstractness at check time.</param>
    /// <param name="instability">The file's instability at check time.</param>
    /// <exception cref="ArgumentNullException"><paramref name="file"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="file"/> is empty.</exception>
    public DistanceZoneViolation(
        string file,
        DistanceZone zone,
        double abstractness,
        double instability)
        : base(ViolationKind.Rule)
    {
        _file = Require(file, nameof(File));
        Zone = zone;
        Abstractness = abstractness;
        Instability = instability;
    }

    private static string Require(string value, string propertyName) =>
        value is null
            ? throw new ArgumentNullException(propertyName)
            : value.Length == 0
                ? throw new ArgumentException($"{propertyName} must not be empty.", propertyName)
                : value;
}
