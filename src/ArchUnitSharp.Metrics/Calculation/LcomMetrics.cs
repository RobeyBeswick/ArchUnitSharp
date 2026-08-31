namespace ArchUnitSharp.Metrics.Calculation;

using ArchUnitSharp.Metrics;

/// <summary>
/// The metrics module's pure LCOM (lack of cohesion of methods) calculations: the eight cohesion
/// metrics and the value of one metric over one class. This is the one place a cohesion metric's value
/// is computed — a <see cref="LcomMetric"/> is a name and a subject kind, and the calculation turns the
/// pair into the measured number, so nothing downstream re-implements a cohesion formula.
/// </summary>
/// <remarks>
/// <para>
/// Each factory returns the <see cref="LcomMetric"/> the fluent surface exposes. Every metric measures
/// one extracted <see cref="ClassInfo"/> and is computed from the class's methods, its fields and the
/// accesses between them — which method reads or writes which field — so the formulas are pure over
/// the extracted info.
/// </para>
/// <para>
/// <see cref="ValueOf(LcomMetric, ClassInfo)"/> computes one metric's value from an extracted
/// <see cref="ClassInfo"/>, with <c>m</c> the number of methods, <c>f</c> the number of fields and
/// <c>a</c> the number of field accesses — the sum over the fields of the methods that access them.
/// The formulas, in the sibling implementations' reading:
/// </para>
/// <list type="bullet">
/// <item>
/// <description>
/// <see cref="Lcom96a"/>, <see cref="Lcom3"/>, <see cref="Lcom5"/> and <see cref="LcomStar"/> are the
/// normalised method-field distance <c>(m − a/f) / (m − 1)</c>, zero when <c>m ≤ 1</c> or <c>f = 0</c>.
/// </description>
/// </item>
/// <item>
/// <description>
/// <see cref="Lcom96b"/> and <see cref="Lcom2"/> are the method-field density complement
/// <c>1 − a/(m·f)</c>, zero when <c>m ≤ 1</c> or <c>f = 0</c>.
/// </description>
/// </item>
/// <item>
/// <description>
/// <see cref="Lcom1"/> is <c>max(P − Q, 0)</c>, where <c>P</c> is the number of method pairs that
/// access no common field and <c>Q</c> the number that share at least one.
/// </description>
/// </item>
/// <item>
/// <description>
/// <see cref="Lcom4"/> is the number of connected components of the graph whose nodes are the methods
/// and whose edges join two methods that access a common field; zero when there are no methods.
/// </description>
/// </item>
/// </list>
/// <para>
/// All values are <see cref="double"/>; <see cref="Lcom1"/> and <see cref="Lcom4"/> are whole numbers.
/// This type is stateless and safe for concurrent use.
/// </para>
/// </remarks>
internal static class LcomMetrics
{
    /// <summary>
    /// The <c>lcom96a</c> metric: the normalised method-field distance of one class. A class-level
    /// metric.
    /// </summary>
    public static LcomMetric Lcom96a() => new(LcomMetricKind.Lcom96a, MetricSubject.Class);

    /// <summary>
    /// The <c>lcom96b</c> metric: the method-field density complement of one class. A class-level
    /// metric.
    /// </summary>
    public static LcomMetric Lcom96b() => new(LcomMetricKind.Lcom96b, MetricSubject.Class);

    /// <summary>
    /// The <c>lcom1</c> metric: the non-sharing minus sharing method pairs of one class. A class-level
    /// metric.
    /// </summary>
    public static LcomMetric Lcom1() => new(LcomMetricKind.Lcom1, MetricSubject.Class);

    /// <summary>
    /// The <c>lcom2</c> metric: the method-field density complement of one class, the same formula as
    /// <see cref="Lcom96b()"/>. A class-level metric.
    /// </summary>
    public static LcomMetric Lcom2() => new(LcomMetricKind.Lcom2, MetricSubject.Class);

    /// <summary>
    /// The <c>lcom3</c> metric: the normalised method-field distance of one class, the same formula as
    /// <see cref="Lcom96a()"/>. A class-level metric.
    /// </summary>
    public static LcomMetric Lcom3() => new(LcomMetricKind.Lcom3, MetricSubject.Class);

    /// <summary>
    /// The <c>lcom4</c> metric: the connected components of the method graph of one class. A
    /// class-level metric.
    /// </summary>
    public static LcomMetric Lcom4() => new(LcomMetricKind.Lcom4, MetricSubject.Class);

    /// <summary>
    /// The <c>lcom5</c> metric: the normalised method-field distance of one class, the same formula as
    /// <see cref="Lcom96a()"/>. A class-level metric.
    /// </summary>
    public static LcomMetric Lcom5() => new(LcomMetricKind.Lcom5, MetricSubject.Class);

    /// <summary>
    /// The <c>lcom*</c> metric: the normalised method-field distance of one class, the same formula as
    /// <see cref="Lcom96a()"/>. A class-level metric.
    /// </summary>
    public static LcomMetric LcomStar() => new(LcomMetricKind.LcomStar, MetricSubject.Class);

    /// <summary>
    /// Computes a cohesion metric's value over one class. Every LCOM metric is a class-level metric,
    /// so the subject is always an extracted <see cref="ClassInfo"/>.
    /// </summary>
    /// <param name="metric">The metric to compute. Must not be <see langword="null"/>.</param>
    /// <param name="classInfo">The class to measure. Must not be <see langword="null"/>.</param>
    /// <returns>The metric's value for the class.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="metric"/> or <paramref name="classInfo"/> is <see langword="null"/>.</exception>
    public static double ValueOf(LcomMetric metric, ClassInfo classInfo)
    {
        ArgumentNullException.ThrowIfNull(metric);
        ArgumentNullException.ThrowIfNull(classInfo);

        return metric.Kind switch
        {
            LcomMetricKind.Lcom96a or LcomMetricKind.Lcom3 or LcomMetricKind.Lcom5 or LcomMetricKind.LcomStar =>
                NormalizedMethodFieldDistance(classInfo),
            LcomMetricKind.Lcom96b or LcomMetricKind.Lcom2 =>
                MethodFieldDensityComplement(classInfo),
            LcomMetricKind.Lcom1 => PairDifference(classInfo),
            LcomMetricKind.Lcom4 => ConnectedComponents(classInfo),
            _ => throw new ArgumentOutOfRangeException(
                nameof(metric),
                metric.Kind,
                "Metric is not a defined LCOM metric."),
        };
    }

    /// <summary>
    /// The normalised method-field distance <c>(m − a/f) / (m − 1)</c>, zero when the class has at most
    /// one method or no fields.
    /// </summary>
    private static double NormalizedMethodFieldDistance(ClassInfo classInfo)
    {
        int methodCount = classInfo.Methods.Count;
        int fieldCount = classInfo.Fields.Count;
        if (methodCount <= 1 || fieldCount == 0)
        {
            return 0.0;
        }

        double averageAccesses = TotalFieldAccesses(classInfo) / (double)fieldCount;
        return (methodCount - averageAccesses) / (methodCount - 1);
    }

    /// <summary>
    /// The method-field density complement <c>1 − a/(m·f)</c>, zero when the class has at most one
    /// method or no fields.
    /// </summary>
    private static double MethodFieldDensityComplement(ClassInfo classInfo)
    {
        int methodCount = classInfo.Methods.Count;
        int fieldCount = classInfo.Fields.Count;
        if (methodCount <= 1 || fieldCount == 0)
        {
            return 0.0;
        }

        return 1.0 - TotalFieldAccesses(classInfo) / (double)(methodCount * fieldCount);
    }

    /// <summary>
    /// The total number of field accesses of a class: the sum over its methods of the fields they
    /// access, which equals the sum over its fields of the methods that access them when method names
    /// are unique. The method side is the source of truth: it preserves every method declaration, so
    /// two overloads of one name that both access a field count as two accesses, where the field side
    /// deduplicates by name and would count one.
    /// </summary>
    private static int TotalFieldAccesses(ClassInfo classInfo) =>
        classInfo.Methods.Sum(static method => method.AccessedFields.Count);

    /// <summary>
    /// The pair difference <c>max(P − Q, 0)</c> of a class.
    /// </summary>
    private static double PairDifference(ClassInfo classInfo)
    {
        IReadOnlyList<MethodInfo> methods = classInfo.Methods;
        int sharing = 0;
        int nonSharing = 0;
        for (int i = 0; i < methods.Count; i++)
        {
            for (int j = i + 1; j < methods.Count; j++)
            {
                if (FieldsOverlap(methods[i], methods[j]))
                {
                    sharing++;
                }
                else
                {
                    nonSharing++;
                }
            }
        }

        return Math.Max(nonSharing - sharing, 0);
    }

    /// <summary>
    /// The number of connected components of the method graph of a class: the graph whose nodes are the
    /// methods and whose edges join two methods that access a common field. Zero when the class has no
    /// methods.
    /// </summary>
    private static double ConnectedComponents(ClassInfo classInfo)
    {
        IReadOnlyList<MethodInfo> methods = classInfo.Methods;
        if (methods.Count == 0)
        {
            return 0.0;
        }

        var remaining = new List<MethodInfo>(methods);
        int components = 0;
        while (remaining.Count > 0)
        {
            components++;
            RemoveComponent(remaining);
        }

        return components;
    }

    /// <summary>
    /// Removes from <paramref name="remaining"/> the whole connected component that contains its first
    /// method: the method and, transitively, every method that reaches it through shared fields.
    /// </summary>
    private static void RemoveComponent(List<MethodInfo> remaining)
    {
        var pending = new Stack<MethodInfo>();
        pending.Push(remaining[0]);
        while (pending.Count > 0)
        {
            MethodInfo method = pending.Pop();
            if (!remaining.Remove(method))
            {
                continue;
            }

            MethodInfo[] neighbors = remaining
                .Where(candidate => FieldsOverlap(method, candidate))
                .ToArray();
            foreach (MethodInfo neighbor in neighbors)
            {
                pending.Push(neighbor);
            }
        }
    }

    /// <summary>
    /// Whether two methods access a common field.
    /// </summary>
    private static bool FieldsOverlap(MethodInfo left, MethodInfo right) =>
        left.AccessedFields.Intersect(right.AccessedFields, StringComparer.Ordinal).Any();
}
