namespace ArchUnitSharp.Metrics;

/// <summary>
/// One custom metric: a caller-named <see cref="Name"/>, a <see cref="Description"/>, and the
/// calculation that turns one extracted <see cref="ClassInfo"/> into the metric's value. It is the
/// metrics module's escape hatch: when the built-in count metrics do not express a class-level
/// measurement, a rule can name and measure its own.
/// </summary>
/// <remarks>
/// <para>
/// A custom metric measures classes, never files: the calculation receives one class's full
/// <see cref="ClassInfo"/> — its name, file, methods and fields — and returns the measured value. The
/// value feeds the same threshold vocabulary the count metrics use, so a rule reads <c>custom metric
/// 'member count' should be below 20</c>, and the custom selection's <c>should satisfy</c> hands its
/// predicate both the value and the class it was measured from.
/// </para>
/// <para>
/// A metric is a value, not a rule: it names what is measured and how, and nothing more. The rule's
/// threshold and comparison are supplied by the terminal that consumes it, so the same metric can back
/// many rules over the same scope. The metric carries its calculation as a delegate, so two metrics
/// built from distinct but identical lambdas are not equal; sharing one metric between concurrent
/// checks is still safe, because checking never mutates it.
/// </para>
/// </remarks>
public sealed record CustomMetric
{
    private readonly string _name;
    private readonly string _description;

    /// <summary>
    /// The metric's name, as the caller named it — <c>member count</c> for a rule that counts a
    /// class's methods and fields. It is what a rule's report shows and what a
    /// <see cref="CustomMetricViolation"/> carries. Must not be <see langword="null"/> or empty; both
    /// the constructor and a <see langword="with"/> expression route through the same validation, so
    /// neither can introduce a bad value.
    /// </summary>
    public string Name
    {
        get => _name;
        init => _name = Require(value, nameof(Name));
    }

    /// <summary>
    /// The metric's description, as the caller wrote it — the rule's intent in the caller's own words.
    /// Must not be <see langword="null"/> or empty; both the constructor and a <see langword="with"/>
    /// expression route through the same validation, so neither can introduce a bad value.
    /// </summary>
    public string Description
    {
        get => _description;
        init => _description = Require(value, nameof(Description));
    }

    /// <summary>
    /// The calculation that turns one <see cref="ClassInfo"/> into the metric's value. Internal: the
    /// assertion invokes it through <see cref="Calculate"/>.
    /// </summary>
    internal Func<ClassInfo, int> Calculation { get; init; }

    /// <summary>
    /// Creates a custom metric over classes. Internal: the fluent surface's <c>CustomMetric</c> method
    /// is the only producer.
    /// </summary>
    /// <param name="name">The metric's name; must not be <see langword="null"/> or empty.</param>
    /// <param name="description">The metric's description; must not be <see langword="null"/> or empty.</param>
    /// <param name="calculation">The calculation over one class; must not be <see langword="null"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="name"/>, <paramref name="description"/> or <paramref name="calculation"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> or <paramref name="description"/> is empty.</exception>
    internal CustomMetric(string name, string description, Func<ClassInfo, int> calculation)
    {
        _name = Require(name, nameof(Name));
        _description = Require(description, nameof(Description));
        ArgumentNullException.ThrowIfNull(calculation);
        Calculation = calculation;
    }

    /// <summary>
    /// Computes the metric's value over one class by invoking the calculation. Internal: the assertion
    /// computes each subject's value through it.
    /// </summary>
    /// <param name="classInfo">The class to measure. Must not be <see langword="null"/>.</param>
    /// <returns>The metric's value for the class.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="classInfo"/> is <see langword="null"/>.</exception>
    internal int Calculate(ClassInfo classInfo)
    {
        ArgumentNullException.ThrowIfNull(classInfo);
        return Calculation(classInfo);
    }

    private static string Require(string value, string propertyName) =>
        value is null
            ? throw new ArgumentNullException(propertyName)
            : value.Length == 0
                ? throw new ArgumentException($"{propertyName} must not be empty.", propertyName)
                : value;
}
