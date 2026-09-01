using ArchUnitSharp.Common.Extraction;

namespace ArchUnitSharp.Slices.Tests;

public class SlicesLoggingTests
{
    [Fact]
    public void A_failing_slices_rule_logs_its_start_progress_and_violations()
    {
        using var temp = new TempDir();
        var policy = new Slices(Graph(
                Self("src/legacy/Old.cs"),
                Self("src/core/Core.cs"),
                Using("src/legacy/Old.cs", "src/core/Core.cs")))
            .DefinedBy("src/(**)/*.cs")
            .ShouldNot()
            .ContainDependency("src/legacy/**", "src/core/**");

        IReadOnlyList<Violation> violations = policy.Check(new CheckOptions
        {
            Logging = LoggingLevel.Debug,
            LogFile = new LogFileOptions { Directory = temp.Path, FileNamePrefix = "suite" },
        });

        Assert.Single(violations);
        string content = File.ReadAllText(Assert.Single(Directory.GetFiles(temp.Path, "suite-*.log")));
        Assert.Contains(
            "[INFO] start check: project slices defined by 'src/(**)/*.cs' should not contain "
            + "dependency from 'src/legacy/**' to 'src/core/**'",
            content);
        Assert.Contains("[DEBUG] progress: projected 1 dependency edge(s)", content);
        Assert.Contains("[WARN] violation:", content);
        Assert.Contains("src/legacy/Old.cs", content);
        Assert.Contains("[INFO] end check: 1 violation(s)", content);
    }

    [Fact]
    public void A_zero_match_slices_rule_logs_its_empty_test_violation()
    {
        using var temp = new TempDir();
        var policy = new Slices(Graph(Self("src/legacy/Old.cs")))
            .DefinedBy("src/(**)/*.cs")
            .Should()
            .ContainDependency("src/nothing/**", "src/core/**");

        IReadOnlyList<Violation> violations = policy.Check(new CheckOptions
        {
            Logging = LoggingLevel.Debug,
            LogFile = new LogFileOptions { Directory = temp.Path, FileNamePrefix = "suite" },
        });

        Assert.Single(violations);
        string content = File.ReadAllText(Assert.Single(Directory.GetFiles(temp.Path, "suite-*.log")));
        Assert.Contains(
            "[INFO] start check: project slices defined by 'src/(**)/*.cs' should contain dependency "
            + "from 'src/nothing/**' to 'src/core/**'",
            content);
        Assert.Contains(
            "[WARN] violation: project slices defined by 'src/(**)/*.cs' should contain dependency "
            + "from 'src/nothing/**' to 'src/core/**'",
            content);
        Assert.Contains("[INFO] end check: 1 violation(s)", content);
    }

    private static Graph Graph(params Edge[] edges) => new(edges);

    private static Edge Self(string file) => new(file, file, external: false, ImportKind.None);

    private static Edge Using(string source, string target) =>
        new(source, target, external: false, ImportKind.Using);
}
