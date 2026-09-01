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
/// <para>
/// Each assertion is handed the check's <see cref="CheckLogger"/> by the terminal that calls it and
/// emits the fixed logging vocabulary: the rule being checked starts on entry, the number of files
/// and subjects the scope selected is progress, every measured metric is logged with its value, and
/// every violation the rule reports is logged as it is produced. The logger only buffers lines — the
/// assertion never touches the filesystem — and the terminal's wrapper records the check's end and
/// flushes the log after the assertion returns.
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
    /// <param name="logger">The check's logger, created by the terminal; <see langword="null"/> means the check logs nothing.</param>
    /// <returns>The violations found; empty when the rule passed.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="rule"/> is <see langword="null"/>.</exception>
    /// <exception cref="UserError"><paramref name="rule"/>'s scope was built without a source provider, so a selected file's text is unavailable.</exception>
    public static IReadOnlyList<Violation> Check(
        MetricRule rule,
        CheckOptions? options,
        CheckLogger? logger = null)
    {
        ArgumentNullException.ThrowIfNull(rule);
        logger ??= CheckLogger.Create(null);

        string description = DescribeRule(rule);
        logger.StartCheck(description);

        Metrics scope = rule.Scope;
        IReadOnlyList<string> selected = MetricsProjection.SelectFiles(scope.Graph, scope.FileFilters);
        logger.Progress($"selected {selected.Count} file(s)");

        if (selected.Count == 0)
        {
            IReadOnlyList<Violation> empty = EmptyTestGuard.Guard(description, options);
            logger.Violations(empty);
            return empty;
        }

        IReadOnlyList<FileInfo> files = selected
            .Select(path => MetricsExtractor.Extract(path, scope.SourceText(path)))
            .ToArray();

        if (rule.Metric.Subject == MetricSubject.Class)
        {
            IReadOnlyList<ClassInfo> subjects = MetricsProjection.SelectClasses(files, scope.ClassFilters);
            if (subjects.Count == 0)
            {
                IReadOnlyList<Violation> empty = EmptyTestGuard.Guard(description, options);
                logger.Violations(empty);
                return empty;
            }

            logger.Progress($"measured {subjects.Count} class(es)");
            return rule.Predicate is not null
                ? CheckPredicate(rule, subjects, logger)
                : CheckThreshold(rule, subjects, logger);
        }

        IReadOnlyList<FileInfo> fileSubjects = MetricsProjection.SelectFileSubjects(files, scope.ClassFilters);
        if (fileSubjects.Count == 0)
        {
            IReadOnlyList<Violation> empty = EmptyTestGuard.Guard(description, options);
            logger.Violations(empty);
            return empty;
        }

        logger.Progress($"measured {fileSubjects.Count} file(s)");
        return rule.Predicate is not null
            ? CheckPredicate(rule, fileSubjects, logger)
            : CheckThreshold(rule, fileSubjects, logger);
    }

    /// <summary>
    /// Checks a custom-metric rule and returns the violations it found. An empty list means the rule
    /// passed. Every custom metric is a class-level metric, so the rule's subjects are the classes the
    /// scope's file and class selectors leave in scope, each measured by the metric's own calculation.
    /// </summary>
    /// <param name="rule">The rule to check. Must not be <see langword="null"/>.</param>
    /// <param name="options">The options to check with; <see langword="null"/> means the defaults in <see cref="CheckOptions"/>.</param>
    /// <param name="logger">The check's logger, created by the terminal; <see langword="null"/> means the check logs nothing.</param>
    /// <returns>The violations found; empty when the rule passed.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="rule"/> is <see langword="null"/>.</exception>
    /// <exception cref="UserError"><paramref name="rule"/>'s scope was built without a source provider, so a selected file's text is unavailable.</exception>
    public static IReadOnlyList<Violation> Check(
        CustomMetricRule rule,
        CheckOptions? options,
        CheckLogger? logger = null)
    {
        ArgumentNullException.ThrowIfNull(rule);
        logger ??= CheckLogger.Create(null);

        string description = DescribeCustomRule(rule);
        logger.StartCheck(description);

        Metrics scope = rule.Scope;
        IReadOnlyList<string> selected = MetricsProjection.SelectFiles(scope.Graph, scope.FileFilters);
        logger.Progress($"selected {selected.Count} file(s)");

        if (selected.Count == 0)
        {
            IReadOnlyList<Violation> empty = EmptyTestGuard.Guard(description, options);
            logger.Violations(empty);
            return empty;
        }

        IReadOnlyList<FileInfo> files = selected
            .Select(path => MetricsExtractor.Extract(path, scope.SourceText(path)))
            .ToArray();

        IReadOnlyList<ClassInfo> subjects = MetricsProjection.SelectClasses(files, scope.ClassFilters);
        if (subjects.Count == 0)
        {
            IReadOnlyList<Violation> empty = EmptyTestGuard.Guard(description, options);
            logger.Violations(empty);
            return empty;
        }

        logger.Progress($"measured {subjects.Count} class(es)");
        return rule.Predicate is not null
            ? CheckCustomPredicate(rule, subjects, logger)
            : CheckCustomThreshold(rule, subjects, logger);
    }

    /// <summary>
    /// Checks a cohesion-metric rule and returns the violations it found. An empty list means the rule
    /// passed. Every LCOM metric is a class-level metric, so the rule's subjects are the classes the
    /// scope's class selectors leave in scope.
    /// </summary>
    /// <param name="rule">The rule to check. Must not be <see langword="null"/>.</param>
    /// <param name="options">The options to check with; <see langword="null"/> means the defaults in <see cref="CheckOptions"/>.</param>
    /// <param name="logger">The check's logger, created by the terminal; <see langword="null"/> means the check logs nothing.</param>
    /// <returns>The violations found; empty when the rule passed.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="rule"/> is <see langword="null"/>.</exception>
    /// <exception cref="UserError"><paramref name="rule"/>'s scope was built without a source provider, so a selected file's text is unavailable.</exception>
    public static IReadOnlyList<Violation> CheckLcom(
        LcomMetricRule rule,
        CheckOptions? options,
        CheckLogger? logger = null)
    {
        ArgumentNullException.ThrowIfNull(rule);
        logger ??= CheckLogger.Create(null);

        string description = DescribeLcomRule(rule);
        logger.StartCheck(description);

        Metrics scope = rule.Scope;
        IReadOnlyList<string> selected = MetricsProjection.SelectFiles(scope.Graph, scope.FileFilters);
        logger.Progress($"selected {selected.Count} file(s)");

        if (selected.Count == 0)
        {
            IReadOnlyList<Violation> empty = EmptyTestGuard.Guard(description, options);
            logger.Violations(empty);
            return empty;
        }

        IReadOnlyList<FileInfo> files = selected
            .Select(path => MetricsExtractor.Extract(path, scope.SourceText(path)))
            .ToArray();

        IReadOnlyList<ClassInfo> subjects = MetricsProjection.SelectClasses(files, scope.ClassFilters);
        if (subjects.Count == 0)
        {
            IReadOnlyList<Violation> empty = EmptyTestGuard.Guard(description, options);
            logger.Violations(empty);
            return empty;
        }

        logger.Progress($"measured {subjects.Count} class(es)");
        return rule.Predicate is not null
            ? CheckLcomPredicate(rule, subjects, logger)
            : CheckLcomThreshold(rule, subjects, logger);
    }

    /// <summary>
    /// Checks a distance-metric rule and returns the violations it found. An empty list means the rule
    /// passed. Every distance metric is a file-level metric, so the rule's subjects are the files the
    /// scope's file and class selectors leave in scope, each measured with the couplings it has in the
    /// whole project's graph.
    /// </summary>
    /// <param name="rule">The rule to check. Must not be <see langword="null"/>.</param>
    /// <param name="options">The options to check with; <see langword="null"/> means the defaults in <see cref="CheckOptions"/>.</param>
    /// <param name="logger">The check's logger, created by the terminal; <see langword="null"/> means the check logs nothing.</param>
    /// <returns>The violations found; empty when the rule passed.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="rule"/> is <see langword="null"/>.</exception>
    /// <exception cref="UserError"><paramref name="rule"/>'s scope was built without a source provider, so a selected file's text is unavailable.</exception>
    public static IReadOnlyList<Violation> CheckDistance(
        DistanceMetricRule rule,
        CheckOptions? options,
        CheckLogger? logger = null)
    {
        ArgumentNullException.ThrowIfNull(rule);
        logger ??= CheckLogger.Create(null);

        string description = DescribeDistanceRule(rule);
        logger.StartCheck(description);

        IReadOnlyList<DistanceInfo> subjects = DistanceSubjects(rule.Scope);
        if (subjects.Count == 0)
        {
            IReadOnlyList<Violation> empty = EmptyTestGuard.Guard(description, options);
            logger.Violations(empty);
            return empty;
        }

        logger.Progress($"measured {subjects.Count} file(s)");
        return rule.Predicate is not null
            ? CheckDistancePredicate(rule, subjects, logger)
            : CheckDistanceThreshold(rule, subjects, logger);
    }

    /// <summary>
    /// Checks a zone-guard rule and returns the violations it found. An empty list means the rule
    /// passed. The rule's subjects are the files the scope's file and class selectors leave in scope,
    /// and every subject whose abstractness/instability point falls in the rule's zone is reported as
    /// one <see cref="DistanceZoneViolation"/>.
    /// </summary>
    /// <param name="rule">The rule to check. Must not be <see langword="null"/>.</param>
    /// <param name="options">The options to check with; <see langword="null"/> means the defaults in <see cref="CheckOptions"/>.</param>
    /// <param name="logger">The check's logger, created by the terminal; <see langword="null"/> means the check logs nothing.</param>
    /// <returns>The violations found; empty when the rule passed.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="rule"/> is <see langword="null"/>.</exception>
    /// <exception cref="UserError"><paramref name="rule"/>'s scope was built without a source provider, so a selected file's text is unavailable.</exception>
    public static IReadOnlyList<Violation> CheckZone(
        DistanceZoneRule rule,
        CheckOptions? options,
        CheckLogger? logger = null)
    {
        ArgumentNullException.ThrowIfNull(rule);
        logger ??= CheckLogger.Create(null);

        string description = DescribeZoneRule(rule);
        logger.StartCheck(description);

        IReadOnlyList<DistanceInfo> subjects = DistanceSubjects(rule.Scope);
        if (subjects.Count == 0)
        {
            IReadOnlyList<Violation> empty = EmptyTestGuard.Guard(description, options);
            logger.Violations(empty);
            return empty;
        }

        logger.Progress($"measured {subjects.Count} file(s)");

        Violation[] violations = subjects
            .Where(info => DistanceCalculation.InZone(info, rule.Zone))
            .Select(info => (Violation)new DistanceZoneViolation(
                info.File,
                rule.Zone,
                DistanceCalculation.ValueOf(DistanceCalculation.Abstractness(), info),
                DistanceCalculation.ValueOf(DistanceCalculation.Instability(), info)))
            .ToArray();
        logger.Violations(violations);
        return violations;
    }

    private static IReadOnlyList<Violation> CheckThreshold(
        MetricRule rule,
        IReadOnlyList<ClassInfo> subjects,
        CheckLogger logger)
    {
        MetricComparison comparison = rule.Comparison!.Value;
        int threshold = rule.Threshold!.Value;
        string metric = MetricWords.Count(rule.Metric.Kind);

        var violations = new List<Violation>();
        foreach (ClassInfo classInfo in subjects)
        {
            int value = CountMetricCalculation.ValueOf(rule.Metric, classInfo);
            logger.Metric($"{metric} of {classInfo.Name}", value);
            if (!SatisfiesThreshold(comparison, value, threshold))
            {
                violations.Add(new MetricViolation(
                    classInfo.FilePath,
                    classInfo.Name,
                    rule.Metric.Kind,
                    value,
                    comparison,
                    threshold));
            }
        }

        logger.Violations(violations);
        return violations;
    }

    private static IReadOnlyList<Violation> CheckThreshold(
        MetricRule rule,
        IReadOnlyList<FileInfo> subjects,
        CheckLogger logger)
    {
        MetricComparison comparison = rule.Comparison!.Value;
        int threshold = rule.Threshold!.Value;
        string metric = MetricWords.Count(rule.Metric.Kind);

        var violations = new List<Violation>();
        foreach (FileInfo file in subjects)
        {
            int value = CountMetricCalculation.ValueOf(rule.Metric, file);
            logger.Metric($"{metric} of {file.Path}", value);
            if (!SatisfiesThreshold(comparison, value, threshold))
            {
                violations.Add(new MetricViolation(
                    file.Path,
                    null,
                    rule.Metric.Kind,
                    value,
                    comparison,
                    threshold));
            }
        }

        logger.Violations(violations);
        return violations;
    }

    private static IReadOnlyList<Violation> CheckPredicate(
        MetricRule rule,
        IReadOnlyList<ClassInfo> subjects,
        CheckLogger logger)
    {
        string metric = MetricWords.Count(rule.Metric.Kind);

        var violations = new List<Violation>();
        foreach (ClassInfo classInfo in subjects)
        {
            int value = CountMetricCalculation.ValueOf(rule.Metric, classInfo);
            logger.Metric($"{metric} of {classInfo.Name}", value);
            if (!rule.Predicate!(value))
            {
                violations.Add(new MetricViolation(
                    classInfo.FilePath,
                    classInfo.Name,
                    rule.Metric.Kind,
                    value,
                    rule.Message!));
            }
        }

        logger.Violations(violations);
        return violations;
    }

    private static IReadOnlyList<Violation> CheckPredicate(
        MetricRule rule,
        IReadOnlyList<FileInfo> subjects,
        CheckLogger logger)
    {
        string metric = MetricWords.Count(rule.Metric.Kind);

        var violations = new List<Violation>();
        foreach (FileInfo file in subjects)
        {
            int value = CountMetricCalculation.ValueOf(rule.Metric, file);
            logger.Metric($"{metric} of {file.Path}", value);
            if (!rule.Predicate!(value))
            {
                violations.Add(new MetricViolation(
                    file.Path,
                    null,
                    rule.Metric.Kind,
                    value,
                    rule.Message!));
            }
        }

        logger.Violations(violations);
        return violations;
    }

    private static IReadOnlyList<Violation> CheckCustomThreshold(
        CustomMetricRule rule,
        IReadOnlyList<ClassInfo> subjects,
        CheckLogger logger)
    {
        MetricComparison comparison = rule.Comparison!.Value;
        int threshold = rule.Threshold!.Value;

        var violations = new List<Violation>();
        foreach (ClassInfo classInfo in subjects)
        {
            int value = rule.Metric.Calculate(classInfo);
            logger.Metric($"{rule.Metric.Name} of {classInfo.Name}", value);
            if (!SatisfiesThreshold(comparison, value, threshold))
            {
                violations.Add(new CustomMetricViolation(
                    classInfo.FilePath,
                    classInfo.Name,
                    rule.Metric.Name,
                    rule.Metric.Description,
                    value,
                    comparison,
                    threshold));
            }
        }

        logger.Violations(violations);
        return violations;
    }

    private static IReadOnlyList<Violation> CheckCustomPredicate(
        CustomMetricRule rule,
        IReadOnlyList<ClassInfo> subjects,
        CheckLogger logger)
    {
        var violations = new List<Violation>();
        foreach (ClassInfo classInfo in subjects)
        {
            int value = rule.Metric.Calculate(classInfo);
            logger.Metric($"{rule.Metric.Name} of {classInfo.Name}", value);
            if (!rule.Predicate!(value, classInfo))
            {
                violations.Add(new CustomMetricViolation(
                    classInfo.FilePath,
                    classInfo.Name,
                    rule.Metric.Name,
                    rule.Metric.Description,
                    value,
                    rule.Message!));
            }
        }

        logger.Violations(violations);
        return violations;
    }

    private static IReadOnlyList<Violation> CheckLcomThreshold(
        LcomMetricRule rule,
        IReadOnlyList<ClassInfo> subjects,
        CheckLogger logger)
    {
        MetricComparison comparison = rule.Comparison!.Value;
        double threshold = rule.Threshold!.Value;
        string metric = MetricWords.Lcom(rule.Metric.Kind);

        var violations = new List<Violation>();
        foreach (ClassInfo classInfo in subjects)
        {
            double value = LcomCalculation.ValueOf(rule.Metric, classInfo);
            logger.Metric($"{metric} of {classInfo.Name}", value);
            if (!SatisfiesThreshold(comparison, value, threshold))
            {
                violations.Add(new LcomMetricViolation(
                    classInfo.FilePath,
                    classInfo.Name,
                    rule.Metric.Kind,
                    value,
                    comparison,
                    threshold));
            }
        }

        logger.Violations(violations);
        return violations;
    }

    private static IReadOnlyList<Violation> CheckLcomPredicate(
        LcomMetricRule rule,
        IReadOnlyList<ClassInfo> subjects,
        CheckLogger logger)
    {
        string metric = MetricWords.Lcom(rule.Metric.Kind);

        var violations = new List<Violation>();
        foreach (ClassInfo classInfo in subjects)
        {
            double value = LcomCalculation.ValueOf(rule.Metric, classInfo);
            logger.Metric($"{metric} of {classInfo.Name}", value);
            if (!rule.Predicate!(value))
            {
                violations.Add(new LcomMetricViolation(
                    classInfo.FilePath,
                    classInfo.Name,
                    rule.Metric.Kind,
                    value,
                    rule.Message!));
            }
        }

        logger.Violations(violations);
        return violations;
    }

    private static IReadOnlyList<Violation> CheckDistanceThreshold(
        DistanceMetricRule rule,
        IReadOnlyList<DistanceInfo> subjects,
        CheckLogger logger)
    {
        MetricComparison comparison = rule.Comparison!.Value;
        double threshold = rule.Threshold!.Value;
        string metric = MetricWords.Distance(rule.Metric.Kind);

        var violations = new List<Violation>();
        foreach (DistanceInfo info in subjects)
        {
            double value = DistanceCalculation.ValueOf(rule.Metric, info);
            logger.Metric($"{metric} of {info.File}", value);
            if (!SatisfiesThreshold(comparison, value, threshold))
            {
                violations.Add(new DistanceMetricViolation(
                    info.File,
                    rule.Metric.Kind,
                    value,
                    comparison,
                    threshold));
            }
        }

        logger.Violations(violations);
        return violations;
    }

    private static IReadOnlyList<Violation> CheckDistancePredicate(
        DistanceMetricRule rule,
        IReadOnlyList<DistanceInfo> subjects,
        CheckLogger logger)
    {
        string metric = MetricWords.Distance(rule.Metric.Kind);

        var violations = new List<Violation>();
        foreach (DistanceInfo info in subjects)
        {
            double value = DistanceCalculation.ValueOf(rule.Metric, info);
            logger.Metric($"{metric} of {info.File}", value);
            if (!rule.Predicate!(value))
            {
                violations.Add(new DistanceMetricViolation(
                    info.File,
                    rule.Metric.Kind,
                    value,
                    rule.Message!));
            }
        }

        logger.Violations(violations);
        return violations;
    }

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
        builder.Append(MetricWords.Count(rule.Metric.Kind));

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
        builder.Append(MetricWords.Lcom(rule.Metric.Kind));

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
        builder.Append(MetricWords.Distance(rule.Metric.Kind));

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
