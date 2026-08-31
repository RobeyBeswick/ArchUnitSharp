namespace ArchUnitSharp.Metrics;

/// <summary>
/// The metric vocabulary in the module's own words, one phrase per kind: <c>method count</c> for
/// <see cref="CountMetricKind.MethodCount"/>, <c>lcom96a</c> for <see cref="LcomMetricKind.Lcom96a"/>,
/// <c>distance from main sequence</c> for <see cref="DistanceMetricKind.DistanceFromMainSequence"/>,
/// and so on. This is the one place a kind becomes the words a report or a rule description shows, so
/// the rule descriptions the assertion builds and the report rows the export writes can never name one
/// metric two ways.
/// </summary>
/// <remarks>
/// <para>
/// The phrase for each kind is fixed: the same word the fluent surface's metric method reads as, in
/// the sibling implementations' vocabulary. A count metric is <c>method count</c>, <c>field count</c>,
/// <c>lines of code</c>, <c>statements</c>, <c>imports</c>, <c>classes</c> or <c>interfaces</c>; a
/// cohesion metric is <c>lcom96a</c>, <c>lcom96b</c>, <c>lcom1</c>, <c>lcom2</c>, <c>lcom3</c>,
/// <c>lcom4</c>, <c>lcom5</c> or <c>lcom*</c>; and a distance metric is <c>abstractness</c>,
/// <c>instability</c>, <c>distance from main sequence</c>, <c>coupling factor</c> or
/// <c>normalised distance</c>. A kind that is not defined raises <see cref="ArgumentOutOfRangeException"/>.
/// </para>
/// <para>
/// This type is stateless and safe for concurrent use.
/// </para>
/// </remarks>
internal static class MetricWords
{
    /// <summary>
    /// A count metric's own words for a report or a rule description.
    /// </summary>
    /// <param name="kind">The count metric's kind.</param>
    /// <returns>The kind's phrase.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="kind"/> is not a defined <see cref="CountMetricKind"/> value.</exception>
    public static string Count(CountMetricKind kind) => kind switch
    {
        CountMetricKind.MethodCount => "method count",
        CountMetricKind.FieldCount => "field count",
        CountMetricKind.LinesOfCode => "lines of code",
        CountMetricKind.Statements => "statements",
        CountMetricKind.Imports => "imports",
        CountMetricKind.Classes => "classes",
        CountMetricKind.Interfaces => "interfaces",
        _ => throw new ArgumentOutOfRangeException(
            nameof(kind),
            kind,
            "Kind is not a defined CountMetricKind value."),
    };

    /// <summary>
    /// A cohesion metric's own words for a report or a rule description.
    /// </summary>
    /// <param name="kind">The cohesion metric's kind.</param>
    /// <returns>The kind's phrase.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="kind"/> is not a defined <see cref="LcomMetricKind"/> value.</exception>
    public static string Lcom(LcomMetricKind kind) => kind switch
    {
        LcomMetricKind.Lcom96a => "lcom96a",
        LcomMetricKind.Lcom96b => "lcom96b",
        LcomMetricKind.Lcom1 => "lcom1",
        LcomMetricKind.Lcom2 => "lcom2",
        LcomMetricKind.Lcom3 => "lcom3",
        LcomMetricKind.Lcom4 => "lcom4",
        LcomMetricKind.Lcom5 => "lcom5",
        LcomMetricKind.LcomStar => "lcom*",
        _ => throw new ArgumentOutOfRangeException(
            nameof(kind),
            kind,
            "Kind is not a defined LcomMetricKind value."),
    };

    /// <summary>
    /// A distance metric's own words for a report or a rule description.
    /// </summary>
    /// <param name="kind">The distance metric's kind.</param>
    /// <returns>The kind's phrase.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="kind"/> is not a defined <see cref="DistanceMetricKind"/> value.</exception>
    public static string Distance(DistanceMetricKind kind) => kind switch
    {
        DistanceMetricKind.Abstractness => "abstractness",
        DistanceMetricKind.Instability => "instability",
        DistanceMetricKind.DistanceFromMainSequence => "distance from main sequence",
        DistanceMetricKind.CouplingFactor => "coupling factor",
        DistanceMetricKind.NormalisedDistance => "normalised distance",
        _ => throw new ArgumentOutOfRangeException(
            nameof(kind),
            kind,
            "Kind is not a defined DistanceMetricKind value."),
    };
}
