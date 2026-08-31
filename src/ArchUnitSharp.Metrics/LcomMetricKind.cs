namespace ArchUnitSharp.Metrics;

/// <summary>
/// Discriminates the cohesion metrics the metrics module measures: the LCOM (lack of cohesion of
/// methods) family. Every kind is a class-level metric — it measures one extracted
/// <see cref="ClassInfo"/> over the methods each field is accessed by and the fields each method
/// accesses — so a <see cref="LcomMetric"/> always carries the <see cref="MetricSubject.Class"/>
/// subject.
/// </summary>
/// <remarks>
/// <para>
/// The family is the sibling implementations' set. The formulas live in the calculation layer's
/// <c>LcomMetrics</c>, one place; in brief, <see cref="Lcom96a"/>, <see cref="Lcom3"/>,
/// <see cref="Lcom5"/> and <see cref="LcomStar"/> share the normalised method-field distance formula,
/// <see cref="Lcom96b"/> and <see cref="Lcom2"/> share the method-field density complement,
/// <see cref="Lcom1"/> is the difference of non-sharing and sharing method pairs, and
/// <see cref="Lcom4"/> counts the connected components of the method graph. Every value is a
/// <see cref="double"/>; <see cref="Lcom1"/> and <see cref="Lcom4"/> are whole numbers by construction.
/// </para>
/// </remarks>
public enum LcomMetricKind
{
    /// <summary>
    /// LCOM96a: the normalised method-field distance, <c>(m − a/f) / (m − 1)</c> for <c>m</c> methods,
    /// <c>f</c> fields and <c>a</c> total field accesses. Zero when the class has at most one method or
    /// no fields.
    /// </summary>
    Lcom96a,

    /// <summary>
    /// LCOM96b: the method-field density complement, <c>1 − a/(m·f)</c>. Zero when the class has at
    /// most one method or no fields.
    /// </summary>
    Lcom96b,

    /// <summary>
    /// LCOM1: <c>max(P − Q, 0)</c>, where <c>P</c> is the number of method pairs that access no common
    /// field and <c>Q</c> the number that share at least one.
    /// </summary>
    Lcom1,

    /// <summary>
    /// LCOM2: the method-field density complement, the same formula as <see cref="Lcom96b"/>.
    /// </summary>
    Lcom2,

    /// <summary>
    /// LCOM3: the normalised method-field distance, the same formula as <see cref="Lcom96a"/>.
    /// </summary>
    Lcom3,

    /// <summary>
    /// LCOM4: the number of connected components of the graph whose nodes are the class's methods and
    /// whose edges join two methods that access a common field. Zero when the class has no methods;
    /// otherwise at least one.
    /// </summary>
    Lcom4,

    /// <summary>
    /// LCOM5: the normalised method-field distance, the same formula as <see cref="Lcom96a"/>.
    /// </summary>
    Lcom5,

    /// <summary>
    /// LCOM*: the normalised method-field distance, the same formula as <see cref="Lcom96a"/>.
    /// </summary>
    LcomStar,
}
