namespace ArchUnitSharp.Metrics;

/// <summary>
/// The kind of subject a <see cref="Metric"/> measures. A class-level metric measures one extracted
/// <see cref="ClassInfo"/> — the <c>method count</c> and <c>field count</c> metrics; a file-level
/// metric measures one extracted <see cref="FileInfo"/> — the <c>lines of code</c>,
/// <c>statements</c>, <c>imports</c>, <c>classes</c> and <c>interfaces</c> metrics.
/// </summary>
public enum MetricSubject
{
    /// <summary>One extracted <see cref="FileInfo"/>.</summary>
    File,

    /// <summary>One extracted <see cref="ClassInfo"/>.</summary>
    Class,
}
