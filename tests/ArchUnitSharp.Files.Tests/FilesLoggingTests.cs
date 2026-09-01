using ArchUnitSharp.Common.Extraction;

namespace ArchUnitSharp.Files.Tests;

public class FilesLoggingTests
{
    [Fact]
    public void A_check_with_logging_writes_its_start_and_end_to_the_configured_file()
    {
        string directory = TempDirectory();
        var rule = new Files(Graph(Self("a.cs"), Self("b.cs"))).Should().Exist();

        IReadOnlyList<Violation> violations = rule.Check(new CheckOptions
        {
            Logging = LoggingLevel.Info,
            LogFile = new LogFileOptions { Directory = directory, FileNamePrefix = "suite" },
        });

        try
        {
            Assert.Empty(violations);
            string file = Assert.Single(Directory.GetFiles(directory, "suite-*.log"));
            string content = File.ReadAllText(file);
            Assert.Contains("[INFO] start check: project files should exist", content);
            Assert.Contains("[INFO] end check: 0 violation(s)", content);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void A_failing_check_at_debug_level_logs_its_progress_and_violations()
    {
        string directory = TempDirectory();
        var rule = new Files(Graph(Self("a.cs"), Self("b.cs")))
            .Should()
            .HaveName("a.cs");

        IReadOnlyList<Violation> violations = rule.Check(new CheckOptions
        {
            Logging = LoggingLevel.Debug,
            LogFile = new LogFileOptions { Directory = directory, FileNamePrefix = "suite" },
        });

        try
        {
            Assert.Single(violations);
            string content = File.ReadAllText(Assert.Single(Directory.GetFiles(directory, "suite-*.log")));
            Assert.Contains("[DEBUG] progress: selected 2 file(s)", content);
            Assert.Contains("[WARN] violation:", content);
            Assert.Contains("b.cs", content);
            Assert.Contains("[INFO] end check: 1 violation(s)", content);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void A_have_no_cycles_rule_logs_its_start_progress_and_violations()
    {
        string directory = TempDirectory();
        var rule = new Files(Graph(
                Using("a.cs", "b.cs"),
                Using("b.cs", "a.cs")))
            .Should()
            .HaveNoCycles();

        IReadOnlyList<Violation> violations = rule.Check(new CheckOptions
        {
            Logging = LoggingLevel.Debug,
            LogFile = new LogFileOptions { Directory = directory, FileNamePrefix = "suite" },
        });

        try
        {
            Assert.Single(violations);
            string content = File.ReadAllText(Assert.Single(Directory.GetFiles(directory, "suite-*.log")));
            Assert.Contains("[INFO] start check: project files should have no cycles", content);
            Assert.Contains("[DEBUG] progress: selected 2 file(s)", content);
            Assert.Contains("[DEBUG] progress: projected 1 cycle(s)", content);
            Assert.Contains("[WARN] violation:", content);
            Assert.Contains("a.cs", content);
            Assert.Contains("[INFO] end check: 1 violation(s)", content);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void A_be_in_folder_rule_logs_its_start_and_end()
    {
        string directory = TempDirectory();
        var rule = new Files(Graph(
                Self("src/Models/Car.cs"),
                Self("src/Models/Truck.cs")))
            .Should()
            .BeInFolder("src/Models");

        IReadOnlyList<Violation> violations = rule.Check(new CheckOptions
        {
            Logging = LoggingLevel.Debug,
            LogFile = new LogFileOptions { Directory = directory, FileNamePrefix = "suite" },
        });

        try
        {
            Assert.Empty(violations);
            string content = File.ReadAllText(Assert.Single(Directory.GetFiles(directory, "suite-*.log")));
            Assert.Contains("[INFO] start check: project files should be in folder 'src/Models'", content);
            Assert.Contains("[DEBUG] progress: selected 2 file(s)", content);
            Assert.Contains("[INFO] end check: 0 violation(s)", content);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void A_be_in_path_rule_logs_its_start_and_end()
    {
        string directory = TempDirectory();
        var rule = new Files(Graph(
                Self("src/Models/Car.cs"),
                Self("src/Models/Truck.cs")))
            .Should()
            .BeInPath("src/Models/*.cs");

        IReadOnlyList<Violation> violations = rule.Check(new CheckOptions
        {
            Logging = LoggingLevel.Debug,
            LogFile = new LogFileOptions { Directory = directory, FileNamePrefix = "suite" },
        });

        try
        {
            Assert.Empty(violations);
            string content = File.ReadAllText(Assert.Single(Directory.GetFiles(directory, "suite-*.log")));
            Assert.Contains("[INFO] start check: project files should be in path 'src/Models/*.cs'", content);
            Assert.Contains("[DEBUG] progress: selected 2 file(s)", content);
            Assert.Contains("[INFO] end check: 0 violation(s)", content);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void A_depend_on_rule_logs_its_object_selection_and_violations()
    {
        string directory = TempDirectory();
        var rule = new Files(Graph(
                Self("src/App/Program.cs"),
                Self("src/Models/Car.cs")))
            .InFolder("src/App")
            .Should()
            .DependOn()
            .InFolder("src/Models");

        IReadOnlyList<Violation> violations = rule.Check(new CheckOptions
        {
            Logging = LoggingLevel.Debug,
            LogFile = new LogFileOptions { Directory = directory, FileNamePrefix = "suite" },
        });

        try
        {
            Assert.Single(violations);
            string content = File.ReadAllText(Assert.Single(Directory.GetFiles(directory, "suite-*.log")));
            Assert.Contains(
                "[INFO] start check: project files in folder 'src/App' should depend on files in folder 'src/Models'",
                content);
            Assert.Contains("[DEBUG] progress: selected 1 file(s)", content);
            Assert.Contains("[DEBUG] progress: object matched 1 file(s)", content);
            Assert.Contains("[DEBUG] progress: projected 0 dependency edge(s)", content);
            Assert.Contains("[WARN] violation:", content);
            Assert.Contains("[INFO] end check: 1 violation(s)", content);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void A_depend_on_external_modules_rule_logs_its_object_selection()
    {
        string directory = TempDirectory();
        var rule = new Files(Graph(
                Self("a.cs"),
                External("a.cs", "System.Linq")))
            .Should()
            .DependOnExternalModules()
            .Matching("System.*");

        IReadOnlyList<Violation> violations = rule.Check(new CheckOptions
        {
            Logging = LoggingLevel.Debug,
            LogFile = new LogFileOptions { Directory = directory, FileNamePrefix = "suite" },
        });

        try
        {
            Assert.Empty(violations);
            string content = File.ReadAllText(Assert.Single(Directory.GetFiles(directory, "suite-*.log")));
            Assert.Contains(
                "[INFO] start check: project files should depend on external modules matching 'System.*'",
                content);
            Assert.Contains("[DEBUG] progress: selected 1 file(s)", content);
            Assert.Contains("[DEBUG] progress: object matched 1 external module(s)", content);
            Assert.Contains("[DEBUG] progress: projected 1 dependency edge(s)", content);
            Assert.Contains("[INFO] end check: 0 violation(s)", content);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void An_adhere_to_rule_logs_its_start_and_violations()
    {
        string directory = TempDirectory();
        var rule = new Files(Graph(Self("Car.cs"), Self("Truck.cs")), Reader("text"))
            .Should()
            .AdhereTo(static detail => detail.NameWithoutExtension == "Car", "is named Car");

        IReadOnlyList<Violation> violations = rule.Check(new CheckOptions
        {
            Logging = LoggingLevel.Debug,
            LogFile = new LogFileOptions { Directory = directory, FileNamePrefix = "suite" },
        });

        try
        {
            Assert.Single(violations);
            string content = File.ReadAllText(Assert.Single(Directory.GetFiles(directory, "suite-*.log")));
            Assert.Contains("[INFO] start check: project files should adhere to 'is named Car'", content);
            Assert.Contains("[DEBUG] progress: selected 2 file(s)", content);
            Assert.Contains("[WARN] violation:", content);
            Assert.Contains("Truck.cs", content);
            Assert.Contains("[INFO] end check: 1 violation(s)", content);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void A_zero_match_files_rule_logs_its_empty_test_violation()
    {
        string directory = TempDirectory();
        var rule = new Files(Graph(Self("a.cs"))).WithName("No.cs").Should().Exist();

        IReadOnlyList<Violation> violations = rule.Check(new CheckOptions
        {
            Logging = LoggingLevel.Debug,
            LogFile = new LogFileOptions { Directory = directory, FileNamePrefix = "suite" },
        });

        try
        {
            Assert.Single(violations);
            string content = File.ReadAllText(Assert.Single(Directory.GetFiles(directory, "suite-*.log")));
            Assert.Contains("[INFO] start check: project files with name 'No.cs' should exist", content);
            Assert.Contains("[DEBUG] progress: selected 0 file(s)", content);
            Assert.Contains("[WARN] violation: project files with name 'No.cs' should exist", content);
            Assert.Contains("[INFO] end check: 1 violation(s)", content);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void Logging_off_by_default_writes_no_file()
    {
        string directory = TempDirectory();
        var rule = new Files(Graph(Self("a.cs"))).Should().Exist();

        IReadOnlyList<Violation> violations = rule.Check(new CheckOptions
        {
            LogFile = new LogFileOptions { Directory = directory, FileNamePrefix = "suite" },
        });

        try
        {
            Assert.Empty(violations);
            Assert.False(Directory.Exists(directory));
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    private static string TempDirectory() =>
        Path.Combine(Path.GetTempPath(), "archunit-files-logging-" + Guid.NewGuid().ToString("N"));

    private static Graph Graph(params Edge[] edges) => new(edges);

    private static Edge Self(string file) => new(file, file, external: false, ImportKind.None);

    private static Edge Using(string source, string target) =>
        new(source, target, external: false, ImportKind.Using);

    private static Edge External(string source, string module) =>
        new(source, module, external: true, ImportKind.Using);

    private static Func<string, string> Reader(string content) => _ => content;
}
