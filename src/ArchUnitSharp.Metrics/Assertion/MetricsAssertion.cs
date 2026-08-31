namespace ArchUnitSharp.Metrics.Assertion;

using System.Globalization;
using System.Text;
using ArchUnitSharp.Common.Extraction;
using ArchUnitSharp.Metrics;
using ArchUnitSharp.Metrics.Extraction;
using ArchUnitSharp.Metrics.Projection;
using CountMetricCalculation = ArchUnitSharp.Metrics.Calculation.CountMetrics;
using DistanceCalculation = ArchUnitSharp.Metrics.Calculation.DistanceMetrics;
using LcomCalculation = ArchUnitSharp.Metrics.Calculation.LcomMetrics;

/// <summary>
/// The metrics module's shared assertion: the one place a metric rule's outcome is computed. A rule is
/// a <see cref="MetricRule"/> — the scope, the <see cref="Metric"/>, and either a
/// <see cref="MetricComparison"/> plus threshold or a custom predicate plus message — or a
/// <see cref="LcomMetricRule"/> over a <see cref="LcomMetric"/>, a <see cref="DistanceMetricRule"/>
/// over a <see cref="DistanceMetric"/>, a <see cref="DistanceZoneRule"/> over a
/// <see cref="DistanceZone"/>, or a <see cref="CustomMetricRule"/> over a <see cref="CustomMetric"/>,
/// and every rule routes an empty subject set through the shared <see cref="EmptyTestGuard"/>.
/// </summary>
/// <remarks>
/// <para>
/// A rule's subjects are the files the scope's file selectors name, extracted by
/// <see cref="MetricsExtractor"/>; a file-level metric measures each selected file and a class-level
/// metric — count and cohesion alike — measures each class of them, narrowed by the scope's
/// <c>for classes matching</c> selector. A scope whose selectors name no file, or whose class selector
/// leaves no subject, is a violation (<see cref="EmptyTestViolation"/>) rather than a pass unless
/// <see cref="CheckOptions.AllowEmptyTests"/> is set.
/// </para>
/// <para>
/// Each subject's value is computed by the calculation layer's <c>CountMetrics</c>,
/// <c>LcomMetrics</c> or <c>DistanceMetrics</c> — a distance metric's couplings read from the graph
/// by <see cref="DistanceProjection"/> — and compared against the rule's threshold with the rule's
/// comparison, or handed to the rule's predicate, and every subject that misses is reported as one
/// <see cref="MetricViolation"/>, <see cref="LcomMetricViolation"/> or
/// <see cref="DistanceMetricViolation"/>, in subject order. A zone rule reports every file whose
/// abstractness/instability point falls in its zone as one <see cref="DistanceZoneViolation"/>. A
/// scope built without a source provider raises a <see cref="UserError"/> when it tries to read a
/// selected file's text, the same boundary the files module's <c>adhere to</c> rules use.
/// </para>
/// <para>
/// This type is stateless and safe for concurrent use. The lists it returns are fresh copies.
/// </para>
/// </remarks>
internal static class MetricsAssertion
{
    /// <summary>
    /// Checks a count-metric rule and returns the violations it found. An empty list means the rule
    /// passed.
    /// </summary>
    /// <param name="rule">The rule to check. Must not be <see langword="null"/>.</param>
    /// <param name="options">The options to check with; <see langword="null"/> means the defaults in <see cref="CheckOptions"/>.</param>
    /// <returns>The violations found; empty when the rule passed.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="rule"/> is <see langword="null"/>.</exception>
    /// <exception cref="UserError"><paramref name="rule"/>'s scope was built without a source provider, so a selected file's text is unavailable.</exception>
    public static IReadOnlyList<Violation> Check(MetricRule rule, CheckOptions? options)
    {
        ArgumentNullException.ThrowIfNull(rule);

        Metrics scope = rule.Scope;
        IReadOnlyList<string> selected = MetricsProjection.SelectFiles(scope.Graph, scope.FileFilters);

        if (selected.Count == 0)
        {
            return EmptyTestGuard.Guard(DescribeRule(rule), options);
        }

        IReadOnlyList<FileInfo> files = selected
            .Select(path => MetricsExtractor.Extract(path, scope.SourceText(path)))
            .ToArray();

        if (rule.Metric.Subject == MetricSubject.Class)
        {
            IReadOnlyList<ClassInfo> subjects = MetricsProjection.SelectClasses(files, scope.ClassFilters);
            if (subjects.Count == 0)
            {
                return EmptyTestGuard.Guard(DescribeRule(rule), options);
            }

            return rule.Predicate is not null
                ? CheckPredicate(rule, subjects)
                : CheckThreshold(rule, subjects);
        }

        IReadOnlyList<FileInfo> fileSubjects = MetricsProjection.SelectFileSubjects(files, scope.ClassFilters);
        if (fileSubjects.Count == 0)
        {
            return EmptyTestGuard.Guard(DescribeRule(rule), options);
        }

        return rule.Predicate is not null
            ? CheckPredicate(rule, fileSubjects)
            : CheckThreshold(rule, fileSubjects);
    }

    /// <summary>
    /// Checks a custom-metric rule and returns the violations it found. An empty list means the rule
    /// passed. Every custom metric is a class-level metric, so the rule's subjects are the classes the
    /// scope's file and class selectors leave in scope, each measured by the metric's own calculation.
    /// </summary>
    /// <param name="rule">The rule to check. Must not be <see langword="null"/>.</param>
    /// <param name="options">The options to check with; <see langword="null"/> means the defaults in <see cref="CheckOptions"/>.</param>
    /// <returns>The violations found; empty when the rule passed.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="rule"/> is <see langword="null"/>.</exception>
    /// <exception cref="UserError"><paramref name="rule"/>'s scope was built without a source provider, so a selected file's text is unavailable.</exception>
    public static IReadOnlyList<Violation> Check(CustomMetricRule rule, CheckOptions? options)
    {
        ArgumentNullException.ThrowIfNull(rule);

        Metrics scope = rule.Scope;
        IReadOnlyList<string> selected = MetricsProjection.SelectFiles(scope.Graph, scope.FileFilters);

        if (selected.Count == 0)
        {
            return EmptyTestGuard.Guard(DescribeCustomRule(rule), options);
        }

        IReadOnlyList<FileInfo> files = selected
            .Select(path => MetricsExtractor.Extract(path, scope.SourceText(path)))
            .ToArray();

        IReadOnlyList<ClassInfo> subjects = MetricsProjection.SelectClasses(files, scope.ClassFilters);
        if (subjects.Count == 0)
        {
            return EmptyTestGuard.Guard(DescribeCustomRule(rule), options);
        }

        return rule.Predicate is not null
            ? CheckCustomPredicate(rule, subjects)
            : CheckCustomThreshold(rule, subjects);
    }

    /// <summary>
    /// Checks a cohesion-metric rule and returns the violations it found. An empty list means the rule
    /// passed. Every LCOM metric is a class-level metric, so the rule's subjects are the classes the
    /// scope's class selectors leave in scope.
    /// </summary>
    /// <param name="rule">The rule to check. Must not be <see langword="null"/>.</param>
    /// <param name="options">The options to check with; <see langword="null"/> means the defaults in <see cref="CheckOptions"/>.</param>
    /// <returns>The violations found; empty when the rule passed.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="rule"/> is <see langword="null"/>.</exception>
    /// <exception cref="UserError"><paramref name="rule"/>'s scope was built without a source provider, so a selected file's text is unavailable.</exception>
    public static IReadOnlyList<Violation> CheckLcom(LcomMetricRule rule, CheckOptions? options)
    {
        ArgumentNullException.ThrowIfNull(rule);

        Metrics scope = rule.Scope;
        IReadOnlyList<string> selected = MetricsProjection.SelectFiles(scope.Graph, scope.FileFilters);

        if (selected.Count == 0)
        {
            return EmptyTestGuard.Guard(DescribeLcomRule(rule), options);
        }

        IReadOnlyList<FileInfo> files = selected
            .Select(path => MetricsExtractor.Extract(path, scope.SourceText(path)))
            .ToArray();

        IReadOnlyList<ClassInfo> subjects = MetricsProjection.SelectClasses(files, scope.ClassFilters);
        if (subjects.Count == 0)
        {
            return EmptyTestGuard.Guard(DescribeLcomRule(rule), options);
        }

        return rule.Predicate is not null
            ? CheckLcomPredicate(rule, subjects)
            : CheckLcomThreshold(rule, subjects);
    }

    /// <summary>
    /// Checks a distance-metric rule and returns the violations it found. An empty list means the rule
    /// passed. Every distance metric is a file-level metric, so the rule's subjects are the files the
    /// scope's file and class selectors leave in scope, each measured with the couplings it has in the
    /// whole project's graph.
    /// </summary>
    /// <param name="rule">The rule to check. Must not be <see langword="null"/>.</param>
    /// <param name="options">The options to check with; <see langword="null"/> means the defaults in <see cref="CheckOptions"/>.</param>
    /// <returns>The violations found; empty when the rule passed.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="rule"/> is <see langword="null"/>.</exception>
    /// <exception cref="UserError"><paramref name="rule"/>'s scope was built without a source provider, so a selected file's text is unavailable.</exception>
    public static IReadOnlyList<Violation> CheckDistance(DistanceMetricRule rule, CheckOptions? options)
    {
        ArgumentNullException.ThrowIfNull(rule);

        IReadOnlyList<DistanceInfo> subjects = DistanceSubjects(rule.Scope);
        if (subjects.Count == 0)
        {
            return EmptyTestGuard.Guard(DescribeDistanceRule(rule), options);
        }

        return rule.Predicate is not null
            ? CheckDistancePredicate(rule, subjects)
            : CheckDistanceThreshold(rule, subjects);
    }

    /// <summary>
    /// Checks a zone-guard rule and returns the violations it found. An empty list means the rule
    /// passed. The rule's subjects are the files the scope's file and class selectors leave in scope,
    /// and every subject whose abstractness/instability point falls in the rule's zone is reported as
    /// one <see cref="DistanceZoneViolation"/>.
    /// </summary>
    /// <param name="rule">The rule to check. Must not be <see langword="null"/>.</param>
    /// <param name="options">The options to check with; <see langword="null"/> means the defaults in <see cref="CheckOptions"/>.</param>
    /// <returns>The violations found; empty when the rule passed.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="rule"/> is <see langword="null"/>.</exception>
    /// <exception cref="UserError"><paramref name="rule"/>'s scope was built without a source provider, so a selected file's text is unavailable.</exception>
    public static IReadOnlyList<Violation> CheckZone(DistanceZoneRule rule, CheckOptions? options)
    {
        ArgumentNullException.ThrowIfNull(rule);

        IReadOnlyList<DistanceInfo> subjects = DistanceSubjects(rule.Scope);
        if (subjects.Count == 0)
        {
            return EmptyTestGuard.Guard(DescribeZoneRule(rule), options);
        }

        return subjects
            .Where(info => DistanceCalculation.InZone(info, rule.Zone))
            .Select(info => (Violation)new DistanceZoneViolation(
                info.File,
                rule.Zone,
                DistanceCalculation.ValueOf(DistanceCalculation.Abstractness(), info),
                DistanceCalculation.ValueOf(DistanceCalculation.Instability(), info)))
            .ToArray();
    }

    private static IReadOnlyList<Violation> CheckThreshold(MetricRule rule, IReadOnlyList<ClassInfo> subjects)
    {
        MetricComparison comparison = rule.Comparison!.Value;
        int threshold = rule.Threshold!.Value;

        return subjects
            .Select(classInfo => (ClassInfo: classInfo, Value: CountMetricCalculation.ValueOf(rule.Metric, classInfo)))
            .Where(pair => !SatisfiesThreshold(comparison, pair.Value, threshold))
            .Select(pair => (Violation)new MetricViolation(
                pair.ClassInfo.FilePath,
                pair.ClassInfo.Name,
                rule.Metric.Kind,
                pair.Value,
                comparison,
                threshold))
            .ToArray();
    }

    private static IReadOnlyList<Violation> CheckThreshold(MetricRule rule, IReadOnlyList<FileInfo> subjects)
    {
        MetricComparison comparison = rule.Comparison!.Value;
        int threshold = rule.Threshold!.Value;

        return subjects
            .Select(file => (File: file, Value: CountMetricCalculation.ValueOf(rule.Metric, file)))
            .Where(pair => !SatisfiesThreshold(comparison, pair.Value, threshold))
            .Select(pair => (Violation)new MetricViolation(
                pair.File.Path,
                null,
                rule.Metric.Kind,
                pair.Value,
                comparison,
                threshold))
            .ToArray();
    }

    private static IReadOnlyList<Violation> CheckPredicate(MetricRule rule, IReadOnlyList<ClassInfo> subjects) =>
        subjects
            .Select(classInfo => (ClassInfo: classInfo, Value: CountMetricCalculation.ValueOf(rule.Metric, classInfo)))
            .Where(pair => !rule.Predicate!(pair.Value))
            .Select(pair => (Violation)new MetricViolation(
                pair.ClassInfo.FilePath,
                pair.ClassInfo.Name,
                rule.Metric.Kind,
                pair.Value,
                rule.Message!))
            .ToArray();

    private static IReadOnlyList<Violation> CheckPredicate(MetricRule rule, IReadOnlyList<FileInfo> subjects) =>
        subjects
            .Select(file => (File: file, Value: CountMetricCalculation.ValueOf(rule.Metric, file)))
            .Where(pair => !rule.Predicate!(pair.Value))
            .Select(pair => (Violation)new MetricViolation(
                pair.File.Path,
                null,
                rule.Metric.Kind,
                pair.Value,
                rule.Message!))
            .ToArray();

    private static IReadOnlyList<Violation> CheckCustomThreshold(
        CustomMetricRule rule,
        IReadOnlyList<ClassInfo> subjects)
    {
        MetricComparison comparison = rule.Comparison!.Value;
        int threshold = rule.Threshold!.Value;

        return subjects
            .Select(classInfo => (ClassInfo: classInfo, Value: rule.Metric.Calculate(classInfo)))
            .Where(pair => !SatisfiesThreshold(comparison, pair.Value, threshold))
            .Select(pair => (Violation)new CustomMetricViolation(
                pair.ClassInfo.FilePath,
                pair.ClassInfo.Name,
                rule.Metric.Name,
                rule.Metric.Description,
                pair.Value,
                comparison,
                threshold))
            .ToArray();
    }

    private static IReadOnlyList<Violation> CheckCustomPredicate(
        CustomMetricRule rule,
        IReadOnlyList<ClassInfo> subjects) =>
        subjects
            .Select(classInfo => (ClassInfo: classInfo, Value: rule.Metric.Calculate(classInfo)))
            .Where(pair => !rule.Predicate!(pair.Value, pair.ClassInfo))
            .Select(pair => (Violation)new CustomMetricViolation(
                pair.ClassInfo.FilePath,
                pair.ClassInfo.Name,
                rule.Metric.Name,
                rule.Metric.Description,
                pair.Value,
                rule.Message!))
            .ToArray();

    private static IReadOnlyList<Violation> CheckLcomThreshold(
        LcomMetricRule rule,
        IReadOnlyList<ClassInfo> subjects)
    {
        MetricComparison comparison = rule.Comparison!.Value;
        double threshold = rule.Threshold!.Value;

        return subjects
            .Select(classInfo => (ClassInfo: classInfo, Value: LcomCalculation.ValueOf(rule.Metric, classInfo)))
            .Where(pair => !SatisfiesThreshold(comparison, pair.Value, threshold))
            .Select(pair => (Violation)new LcomMetricViolation(
                pair.ClassInfo.FilePath,
                pair.ClassInfo.Name,
                rule.Metric.Kind,
                pair.Value,
                comparison,
                threshold))
            .ToArray();
    }

    private static IReadOnlyList<Violation> CheckLcomPredicate(
        LcomMetricRule rule,
        IReadOnlyList<ClassInfo> subjects) =>
        subjects
            .Select(classInfo => (ClassInfo: classInfo, Value: LcomCalculation.ValueOf(rule.Metric, classInfo)))
            .Where(pair => !rule.Predicate!(pair.Value))
            .Select(pair => (Violation)new LcomMetricViolation(
                pair.ClassInfo.FilePath,
                pair.ClassInfo.Name,
                rule.Metric.Kind,
                pair.Value,
                rule.Message!))
            .ToArray();

    private static IReadOnlyList<Violation> CheckDistanceThreshold(
        DistanceMetricRule rule,
        IReadOnlyList<DistanceInfo> subjects)
    {
        MetricComparison comparison = rule.Comparison!.Value;
        double threshold = rule.Threshold!.Value;

        return subjects
            .Select(info => (Info: info, Value: DistanceCalculation.ValueOf(rule.Metric, info)))
            .Where(pair => !SatisfiesThreshold(comparison, pair.Value, threshold))
            .Select(pair => (Violation)new DistanceMetricViolation(
                pair.Info.File,
                rule.Metric.Kind,
                pair.Value,
                comparison,
                threshold))
            .ToArray();
    }

    private static IReadOnlyList<Violation> CheckDistancePredicate(
        DistanceMetricRule rule,
        IReadOnlyList<DistanceInfo> subjects) =>
        subjects
            .Select(info => (Info: info, Value: DistanceCalculation.ValueOf(rule.Metric, info)))
            .Where(pair => !rule.Predicate!(pair.Value))
            .Select(pair => (Violation)new DistanceMetricViolation(
                pair.Info.File,
                rule.Metric.Kind,
                pair.Value,
                rule.Message!))
            .ToArray();

    /// <summary>
    /// A rule's file-level subjects as distance infos: the files the scope's file selectors name,
    /// narrowed to the files that contain a matching class, each enriched with its couplings in the
    /// whole project's graph.
    /// </summary>
    private static IReadOnlyList<DistanceInfo> DistanceSubjects(Metrics scope)
    {
        IReadOnlyList<string> selected = MetricsProjection.SelectFiles(scope.Graph, scope.FileFilters);
        if (selected.Count == 0)
        {
            return Array.Empty<DistanceInfo>();
        }

        IReadOnlyList<FileInfo> files = selected
            .Select(path => MetricsExtractor.Extract(path, scope.SourceText(path)))
            .ToArray();

        IReadOnlyList<FileInfo> subjects = MetricsProjection.SelectFileSubjects(files, scope.ClassFilters);
        if (subjects.Count == 0)
        {
            return Array.Empty<DistanceInfo>();
        }

        return DistanceProjection.Build(subjects, scope.Graph);
    }

    /// <summary>
    /// Whether a metric value satisfies a comparison against a threshold.
    /// </summary>
    private static bool SatisfiesThreshold(MetricComparison comparison, int value, int threshold) =>
        comparison switch
        {
            MetricComparison.Below => value < threshold,
            MetricComparison.Above => value > threshold,
            MetricComparison.Equal => value == threshold,
            MetricComparison.BelowOrEqual => value <= threshold,
            MetricComparison.AboveOrEqual => value >= threshold,
            _ => throw new ArgumentOutOfRangeException(
                nameof(comparison),
                comparison,
                "Comparison is not a defined MetricComparison value."),
        };

    /// <summary>
    /// Whether a cohesion metric value satisfies a comparison against a threshold.
    /// </summary>
    private static bool SatisfiesThreshold(MetricComparison comparison, double value, double threshold) =>
        comparison switch
        {
            MetricComparison.Below => value < threshold,
            MetricComparison.Above => value > threshold,
            MetricComparison.Equal => value == threshold,
            MetricComparison.BelowOrEqual => value <= threshold,
            MetricComparison.AboveOrEqual => value >= threshold,
            _ => throw new ArgumentOutOfRangeException(
                nameof(comparison),
                comparison,
                "Comparison is not a defined MetricComparison value."),
        };

    /// <summary>
    /// The whole rule, in the words a report would show: the scope, the metric, and the required
    /// comparison and threshold or the required predicate's message.
    /// </summary>
    private static string DescribeRule(MetricRule rule)
    {
        var builder = new StringBuilder(rule.Scope.DescribeScope());
        builder.Append(' ');
        builder.Append(MetricWords(rule.Metric.Kind));

        if (rule.Predicate is not null)
        {
            builder.Append(" should satisfy '");
            builder.Append(rule.Message);
            builder.Append('\'');
            return builder.ToString();
        }

        builder.Append(' ');
        builder.Append(ComparisonWords(rule.Comparison!.Value));
        builder.Append(' ');
        builder.Append(rule.Threshold!.Value);
        return builder.ToString();
    }

    /// <summary>
    /// The whole custom-metric rule, in the words a report would show: the scope, the metric's name,
    /// and the required comparison and threshold or the required predicate's message.
    /// </summary>
    private static string DescribeCustomRule(CustomMetricRule rule)
    {
        var builder = new StringBuilder(rule.Scope.DescribeScope());
        builder.Append(" custom metric '");
        builder.Append(rule.Metric.Name);
        builder.Append('\'');

        if (rule.Predicate is not null)
        {
            builder.Append(" should satisfy '");
            builder.Append(rule.Message);
            builder.Append('\'');
            return builder.ToString();
        }

        builder.Append(' ');
        builder.Append(ComparisonWords(rule.Comparison!.Value));
        builder.Append(' ');
        builder.Append(rule.Threshold!.Value);
        return builder.ToString();
    }

    /// <summary>
    /// The whole cohesion rule, in the words a report would show: the scope, the metric, and the
    /// required comparison and threshold or the required predicate's message.
    /// </summary>
    private static string DescribeLcomRule(LcomMetricRule rule)
    {
        var builder = new StringBuilder(rule.Scope.DescribeScope());
        builder.Append(' ');
        builder.Append(LcomWords(rule.Metric.Kind));

        if (rule.Predicate is not null)
        {
            builder.Append(" should satisfy '");
            builder.Append(rule.Message);
            builder.Append('\'');
            return builder.ToString();
        }

        builder.Append(' ');
        builder.Append(ComparisonWords(rule.Comparison!.Value));
        builder.Append(' ');
        builder.Append(rule.Threshold!.Value.ToString(CultureInfo.InvariantCulture));
        return builder.ToString();
    }

    /// <summary>
    /// The whole distance rule, in the words a report would show: the scope, the metric, and the
    /// required comparison and threshold or the required predicate's message.
    /// </summary>
    private static string DescribeDistanceRule(DistanceMetricRule rule)
    {
        var builder = new StringBuilder(rule.Scope.DescribeScope());
        builder.Append(' ');
        builder.Append(DistanceWords(rule.Metric.Kind));

        if (rule.Predicate is not null)
        {
            builder.Append(" should satisfy '");
            builder.Append(rule.Message);
            builder.Append('\'');
            return builder.ToString();
        }

        builder.Append(' ');
        builder.Append(ComparisonWords(rule.Comparison!.Value));
        builder.Append(' ');
        builder.Append(rule.Threshold!.Value.ToString(CultureInfo.InvariantCulture));
        return builder.ToString();
    }

    /// <summary>
    /// The whole zone rule, in the words a report would show: the scope and the zone it guards
    /// against.
    /// </summary>
    private static string DescribeZoneRule(DistanceZoneRule rule)
    {
        var builder = new StringBuilder(rule.Scope.DescribeScope());
        builder.Append(' ');
        builder.Append(ZoneWords(rule.Zone));
        return builder.ToString();
    }

    /// <summary>
    /// The metric's own words for a report.
    /// </summary>
    private static string MetricWords(CountMetricKind kind) => kind switch
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
    /// The cohesion metric's own words for a report.
    /// </summary>
    private static string LcomWords(LcomMetricKind kind) => kind switch
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
    /// The distance metric's own words for a report.
    /// </summary>
    private static string DistanceWords(DistanceMetricKind kind) => kind switch
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

    /// <summary>
    /// The zone's own words for a report: <c>not in zone of pain</c> for
    /// <see cref="DistanceZone.Pain"/>, and so on.
    /// </summary>
    private static string ZoneWords(DistanceZone zone) => zone switch
    {
        DistanceZone.Pain => "not in zone of pain",
        DistanceZone.Uselessness => "not in zone of uselessness",
        _ => throw new ArgumentOutOfRangeException(
            nameof(zone),
            zone,
            "Zone is not a defined DistanceZone value."),
    };

    /// <summary>
    /// The comparison's predicate words for a report: <c>should be below</c> for
    /// <see cref="MetricComparison.Below"/>, and so on.
    /// </summary>
    private static string ComparisonWords(MetricComparison comparison) => comparison switch
    {
        MetricComparison.Below => "should be below",
        MetricComparison.Above => "should be above",
        MetricComparison.Equal => "should be",
        MetricComparison.BelowOrEqual => "should be below or equal to",
        MetricComparison.AboveOrEqual => "should be above or equal to",
        _ => throw new ArgumentOutOfRangeException(
            nameof(comparison),
            comparison,
            "Comparison is not a defined MetricComparison value."),
    };
}
