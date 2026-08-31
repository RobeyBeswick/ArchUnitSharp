namespace ArchUnitSharp.Metrics;

/// <summary>
/// The cohesion-metric section of a metrics rule chain: <c>lcom</c>. Built from <see cref="Metrics.Lcom"/>;
/// its metric methods name what a rule measures and each returns the <see cref="LcomMetricSelection"/>
/// whose threshold methods complete the rule.
/// </summary>
/// <remarks>
/// <para>
/// This type is the lcom section, nothing else: it carries no rule logic and no metric value. Each
/// metric method forwards the scope and the named metric to a <see cref="LcomMetricSelection"/>, which
/// is where the rule's threshold is chosen and checked. The metric vocabulary is the sibling LCOM
/// family: the class-level <see cref="Lcom96a"/>, <see cref="Lcom96b"/>, <see cref="Lcom1"/>,
/// <see cref="Lcom2"/>, <see cref="Lcom3"/>, <see cref="Lcom4"/>, <see cref="Lcom5"/> and
/// <see cref="LcomStar"/>.
/// </para>
/// <para>
/// This type is immutable and safe for concurrent use. Building a rule from it never mutates the scope
/// it was built from, so a <see cref="LcomMetrics"/> value can be stored and reused.
/// </para>
/// </remarks>
public sealed class LcomMetrics
{
    private readonly Metrics _metrics;

    /// <summary>
    /// Creates the lcom section over <paramref name="metrics"/>. Callers obtain a
    /// <see cref="LcomMetrics"/> from <see cref="Metrics.Lcom"/> rather than constructing one.
    /// </summary>
    /// <param name="metrics">The scope the lcom section belongs to.</param>
    internal LcomMetrics(Metrics metrics) => _metrics = metrics;

    /// <summary>
    /// The scope this lcom section belongs to. Internal: a metric selection reads it to check the rule
    /// the selection builds.
    /// </summary>
    internal Metrics Metrics => _metrics;

    /// <summary>
    /// <c>lcom96a</c>: the normalised method-field distance of each class. A class-level metric, so a
    /// rule reads <c>lcom96a should be below 0.8</c>.
    /// </summary>
    /// <returns>The metric selection whose threshold methods complete the rule.</returns>
    public LcomMetricSelection Lcom96a() => new(_metrics, Calculation.LcomMetrics.Lcom96a());

    /// <summary>
    /// <c>lcom96b</c>: the method-field density complement of each class. A class-level metric, so a
    /// rule reads <c>lcom96b should be below 0.3</c>.
    /// </summary>
    /// <returns>The metric selection whose threshold methods complete the rule.</returns>
    public LcomMetricSelection Lcom96b() => new(_metrics, Calculation.LcomMetrics.Lcom96b());

    /// <summary>
    /// <c>lcom1</c>: the non-sharing minus sharing method pairs of each class. A class-level metric, so
    /// a rule reads <c>lcom1 should be below 3</c>.
    /// </summary>
    /// <returns>The metric selection whose threshold methods complete the rule.</returns>
    public LcomMetricSelection Lcom1() => new(_metrics, Calculation.LcomMetrics.Lcom1());

    /// <summary>
    /// <c>lcom2</c>: the method-field density complement of each class, the same formula as
    /// <see cref="Lcom96b()"/>. A class-level metric, so a rule reads <c>lcom2 should be below 0.5</c>.
    /// </summary>
    /// <returns>The metric selection whose threshold methods complete the rule.</returns>
    public LcomMetricSelection Lcom2() => new(_metrics, Calculation.LcomMetrics.Lcom2());

    /// <summary>
    /// <c>lcom3</c>: the normalised method-field distance of each class, the same formula as
    /// <see cref="Lcom96a()"/>. A class-level metric, so a rule reads <c>lcom3 should be below 0.8</c>.
    /// </summary>
    /// <returns>The metric selection whose threshold methods complete the rule.</returns>
    public LcomMetricSelection Lcom3() => new(_metrics, Calculation.LcomMetrics.Lcom3());

    /// <summary>
    /// <c>lcom4</c>: the connected components of the method graph of each class. A class-level metric,
    /// so a rule reads <c>lcom4 should be 1</c>.
    /// </summary>
    /// <returns>The metric selection whose threshold methods complete the rule.</returns>
    public LcomMetricSelection Lcom4() => new(_metrics, Calculation.LcomMetrics.Lcom4());

    /// <summary>
    /// <c>lcom5</c>: the normalised method-field distance of each class, the same formula as
    /// <see cref="Lcom96a()"/>. A class-level metric, so a rule reads <c>lcom5 should be below 0.8</c>.
    /// </summary>
    /// <returns>The metric selection whose threshold methods complete the rule.</returns>
    public LcomMetricSelection Lcom5() => new(_metrics, Calculation.LcomMetrics.Lcom5());

    /// <summary>
    /// <c>lcom*</c>: the normalised method-field distance of each class, the same formula as
    /// <see cref="Lcom96a()"/>. A class-level metric, so a rule reads <c>lcom* should be below 0.8</c>.
    /// </summary>
    /// <returns>The metric selection whose threshold methods complete the rule.</returns>
    public LcomMetricSelection LcomStar() => new(_metrics, Calculation.LcomMetrics.LcomStar());
}
