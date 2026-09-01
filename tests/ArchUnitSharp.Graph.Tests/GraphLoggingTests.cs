using ArchUnitSharp.Common.Extraction;

namespace ArchUnitSharp.Graph.Tests;

public class GraphLoggingTests
{
    [Fact]
    public void A_check_with_a_matching_scope_logs_its_start_and_progress()
    {
        using var temp = new TempDir();
        var report = new GraphReport(Graph(Self("a.cs"), Self("b.cs")));

        IReadOnlyList<Violation> violations = report.Check(new CheckOptions
        {
            Logging = LoggingLevel.Debug,
            LogFile = new LogFileOptions { Directory = temp.Path, FileNamePrefix = "suite" },
        });

        Assert.Empty(violations);
        string content = File.ReadAllText(Assert.Single(Directory.GetFiles(temp.Path, "suite-*.log")));
        Assert.Contains("[INFO] start check: project graph", content);
        Assert.Contains("[DEBUG] progress: scope matched 2 file(s)", content);
        Assert.Contains("[INFO] end check: 0 violation(s)", content);
    }

    [Fact]
    public void A_zero_match_graph_check_logs_its_empty_test_violation()
    {
        using var temp = new TempDir();
        var report = new GraphReport(Graph(Self("a.cs"))).ReachableFrom("No/Such/File.cs");

        IReadOnlyList<Violation> violations = report.Check(new CheckOptions
        {
            Logging = LoggingLevel.Debug,
            LogFile = new LogFileOptions { Directory = temp.Path, FileNamePrefix = "suite" },
        });

        Assert.Single(violations);
        string content = File.ReadAllText(Assert.Single(Directory.GetFiles(temp.Path, "suite-*.log")));
        Assert.Contains(
            "[INFO] start check: project graph reachable from 'No/Such/File.cs'",
            content);
        Assert.Contains("[DEBUG] progress: scope matched 0 file(s)", content);
        Assert.Contains(
            "[WARN] violation: project graph reachable from 'No/Such/File.cs'",
            content);
        Assert.Contains("[INFO] end check: 1 violation(s)", content);
    }

    private static ArchUnitSharp.Common.Extraction.Graph Graph(params Edge[] edges) => new(edges);

    private static Edge Self(string file) => new(file, file, external: false, ImportKind.None);
}
