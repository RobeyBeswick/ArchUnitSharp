using ArchUnitSharp.Common.Extraction;

namespace ArchUnitSharp.Layers.Tests;

public class LayersLoggingTests
{
    [Fact]
    public void A_failing_layers_rule_logs_its_start_progress_and_violations()
    {
        using var temp = new TempDir();
        var policy = new Layers(Graph(
                Self("src/App/Program.cs"),
                Self("src/Infra/Db.cs"),
                Using("src/App/Program.cs", "src/Infra/Db.cs")))
            .Layer("App").DefinedByFolder("src/App")
            .Layer("Infra").DefinedByFolder("src/Infra")
            .WhereLayer("App").MayNotDependOnLayers("Infra");

        IReadOnlyList<Violation> violations = policy.Check(new CheckOptions
        {
            Logging = LoggingLevel.Debug,
            LogFile = new LogFileOptions { Directory = temp.Path, FileNamePrefix = "suite" },
        });

        Assert.Single(violations);
        string content = File.ReadAllText(Assert.Single(Directory.GetFiles(temp.Path, "suite-*.log")));
        Assert.Contains(
            "[INFO] start check: project layers where layer 'App' may not depend on layers 'Infra'",
            content);
        Assert.Contains("[DEBUG] progress: checking 1 layer rule(s)", content);
        Assert.Contains("[DEBUG] progress: projected 1 cross-layer dependencies", content);
        Assert.Contains("[WARN] violation:", content);
        Assert.Contains("src/Infra/Db.cs", content);
        Assert.Contains("[INFO] end check: 1 violation(s)", content);
    }

    [Fact]
    public void A_zero_match_layers_rule_logs_its_empty_test_violation()
    {
        using var temp = new TempDir();
        var policy = new Layers(Graph(Self("src/App/Program.cs")))
            .Layer("Empty").DefinedByFolder("src/Empty")
            .WhereLayer("Empty").MayNotDependOnLayers("App");

        IReadOnlyList<Violation> violations = policy.Check(new CheckOptions
        {
            Logging = LoggingLevel.Debug,
            LogFile = new LogFileOptions { Directory = temp.Path, FileNamePrefix = "suite" },
        });

        Assert.Single(violations);
        string content = File.ReadAllText(Assert.Single(Directory.GetFiles(temp.Path, "suite-*.log")));
        Assert.Contains(
            "[INFO] start check: project layers where layer 'Empty' may not depend on layers 'App'",
            content);
        Assert.Contains("[DEBUG] progress: checking 1 layer rule(s)", content);
        Assert.Contains(
            "[WARN] violation: project layers where layer 'Empty' may not depend on layers 'App'",
            content);
        Assert.Contains("[INFO] end check: 1 violation(s)", content);
    }

    private static Graph Graph(params Edge[] edges) => new(edges);

    private static Edge Self(string file) => new(file, file, external: false, ImportKind.None);

    private static Edge Using(string source, string target) =>
        new(source, target, external: false, ImportKind.Using);
}
