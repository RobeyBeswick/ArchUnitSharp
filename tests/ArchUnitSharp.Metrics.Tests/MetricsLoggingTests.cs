using ArchUnitSharp.Common.Extraction;

namespace ArchUnitSharp.Metrics.Tests;

public class MetricsLoggingTests
{
    [Fact]
    public void A_metric_check_with_logging_writes_its_metrics_and_violations_to_the_file()
    {
        using var temp = new TempDir();
        var rule = (MetricRule)new Metrics(Graph(Self("src/Car.cs")), _ => Source)
            .Count()
            .MethodCount()
            .ShouldBeBelow(1);

        IReadOnlyList<Violation> violations = rule.Check(new CheckOptions
        {
            Logging = LoggingLevel.Info,
            LogFile = new LogFileOptions { Directory = temp.Path, FileNamePrefix = "suite" },
        });

        Assert.Single(violations);
        string content = File.ReadAllText(Assert.Single(Directory.GetFiles(temp.Path, "suite-*.log")));
        Assert.Contains(
            "[INFO] start check: project metrics method count should be below 1",
            content);
        Assert.Contains("[INFO] metric: method count of App.Car = 1", content);
        Assert.Contains("[WARN] violation:", content);
        Assert.Contains("[INFO] end check: 1 violation(s)", content);
    }

    [Fact]
    public void Debug_logging_includes_the_progress_lines_of_a_metric_check()
    {
        using var temp = new TempDir();
        var rule = (MetricRule)new Metrics(Graph(Self("src/Car.cs")), _ => Source)
            .Count()
            .MethodCount()
            .ShouldBeBelow(1);

        IReadOnlyList<Violation> violations = rule.Check(new CheckOptions
        {
            Logging = LoggingLevel.Debug,
            LogFile = new LogFileOptions { Directory = temp.Path, FileNamePrefix = "suite" },
        });

        Assert.Single(violations);
        string content = File.ReadAllText(Assert.Single(Directory.GetFiles(temp.Path, "suite-*.log")));
        Assert.Contains("[DEBUG] progress: selected 1 file(s)", content);
        Assert.Contains("[DEBUG] progress: measured 1 class(es)", content);
    }

    [Fact]
    public void A_custom_metric_check_logs_its_metrics_and_violations()
    {
        using var temp = new TempDir();
        var rule = (CustomMetricRule)new Metrics(Graph(Self("src/Car.cs")), _ => Source)
            .CustomMetric("member count", "members", static _ => 2)
            .ShouldBeAbove(5);

        IReadOnlyList<Violation> violations = rule.Check(new CheckOptions
        {
            Logging = LoggingLevel.Debug,
            LogFile = new LogFileOptions { Directory = temp.Path, FileNamePrefix = "suite" },
        });

        Assert.Single(violations);
        string content = File.ReadAllText(Assert.Single(Directory.GetFiles(temp.Path, "suite-*.log")));
        Assert.Contains(
            "[INFO] start check: project metrics custom metric 'member count' should be above 5",
            content);
        Assert.Contains("[DEBUG] progress: selected 1 file(s)", content);
        Assert.Contains("[DEBUG] progress: measured 1 class(es)", content);
        Assert.Contains("[INFO] metric: member count of App.Car = 2", content);
        Assert.Contains("[WARN] violation:", content);
        Assert.Contains("[INFO] end check: 1 violation(s)", content);
    }

    [Fact]
    public void A_lcom_check_logs_its_metrics_and_violations()
    {
        using var temp = new TempDir();
        var rule = (LcomMetricRule)new Metrics(Graph(Self("src/Car.cs")), _ => Source)
            .Lcom()
            .Lcom4()
            .ShouldBeAbove(1.5);

        IReadOnlyList<Violation> violations = rule.Check(new CheckOptions
        {
            Logging = LoggingLevel.Debug,
            LogFile = new LogFileOptions { Directory = temp.Path, FileNamePrefix = "suite" },
        });

        Assert.Single(violations);
        string content = File.ReadAllText(Assert.Single(Directory.GetFiles(temp.Path, "suite-*.log")));
        Assert.Contains("[INFO] start check: project metrics lcom4 should be above 1.5", content);
        Assert.Contains("[DEBUG] progress: selected 1 file(s)", content);
        Assert.Contains("[DEBUG] progress: measured 1 class(es)", content);
        Assert.Contains("[INFO] metric: lcom4 of App.Car = 1", content);
        Assert.Contains("[WARN] violation:", content);
        Assert.Contains("[INFO] end check: 1 violation(s)", content);
    }

    [Fact]
    public void A_distance_check_logs_its_metrics_and_violations()
    {
        using var temp = new TempDir();
        var rule = (DistanceMetricRule)new Metrics(Graph(Self("src/Car.cs")), _ => Source)
            .Distance()
            .Instability()
            .ShouldBeAbove(0.5);

        IReadOnlyList<Violation> violations = rule.Check(new CheckOptions
        {
            Logging = LoggingLevel.Debug,
            LogFile = new LogFileOptions { Directory = temp.Path, FileNamePrefix = "suite" },
        });

        Assert.Single(violations);
        string content = File.ReadAllText(Assert.Single(Directory.GetFiles(temp.Path, "suite-*.log")));
        Assert.Contains("[INFO] start check: project metrics instability should be above 0.5", content);
        Assert.Contains("[DEBUG] progress: measured 1 file(s)", content);
        Assert.Contains("[INFO] metric: instability of src/Car.cs = 0", content);
        Assert.Contains("[WARN] violation:", content);
        Assert.Contains("[INFO] end check: 1 violation(s)", content);
    }

    [Fact]
    public void A_zone_check_logs_its_start_and_violations()
    {
        using var temp = new TempDir();
        var rule = (DistanceZoneRule)new Metrics(Graph(Self("src/Car.cs")), _ => Source)
            .Distance()
            .NotInZoneOfPain();

        IReadOnlyList<Violation> violations = rule.Check(new CheckOptions
        {
            Logging = LoggingLevel.Debug,
            LogFile = new LogFileOptions { Directory = temp.Path, FileNamePrefix = "suite" },
        });

        Assert.Single(violations);
        string content = File.ReadAllText(Assert.Single(Directory.GetFiles(temp.Path, "suite-*.log")));
        Assert.Contains("[INFO] start check: project metrics not in zone of pain", content);
        Assert.Contains("[DEBUG] progress: measured 1 file(s)", content);
        Assert.Contains("[WARN] violation:", content);
        Assert.Contains("src/Car.cs", content);
        Assert.Contains("[INFO] end check: 1 violation(s)", content);
    }

    [Fact]
    public void A_zero_match_metric_check_logs_its_empty_test_violation()
    {
        using var temp = new TempDir();
        var rule = (MetricRule)new Metrics(Graph(Self("src/Car.cs")), _ => Source)
            .WithName("No.cs")
            .Count()
            .MethodCount()
            .ShouldBeBelow(1);

        IReadOnlyList<Violation> violations = rule.Check(new CheckOptions
        {
            Logging = LoggingLevel.Debug,
            LogFile = new LogFileOptions { Directory = temp.Path, FileNamePrefix = "suite" },
        });

        Assert.Single(violations);
        string content = File.ReadAllText(Assert.Single(Directory.GetFiles(temp.Path, "suite-*.log")));
        Assert.Contains(
            "[INFO] start check: project metrics with name 'No.cs' method count should be below 1",
            content);
        Assert.Contains("[DEBUG] progress: selected 0 file(s)", content);
        Assert.Contains(
            "[WARN] violation: project metrics with name 'No.cs' method count should be below 1",
            content);
        Assert.Contains("[INFO] end check: 1 violation(s)", content);
    }

    private const string Source =
        "namespace App;\n" +
        "public class Car\n" +
        "{\n" +
        "    public void Drive() { }\n" +
        "}\n";

    private static Graph Graph(params Edge[] edges) => new(edges);

    private static Edge Self(string file) => new(file, file, external: false, ImportKind.None);
}
