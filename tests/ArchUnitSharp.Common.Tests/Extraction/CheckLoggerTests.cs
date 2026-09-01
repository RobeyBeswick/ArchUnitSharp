using ArchUnitSharp.Common.Extraction;

namespace ArchUnitSharp.Common.Tests.Extraction;

public class CheckLoggerTests
{
    private static readonly DateTime Timestamp = new(2026, 8, 31, 21, 46, 30, DateTimeKind.Utc);

    [Fact]
    public void The_default_options_log_nothing()
    {
        var logger = CheckLogger.Create(null);

        logger.StartCheck("project files should exist");
        logger.Progress("selected 2 file(s)");
        logger.Violation(new EmptyTestViolation("project files should exist"));
        logger.Metric("method count", 3);

        Assert.Empty(logger.Lines);
    }

    [Fact]
    public void The_five_verbs_log_the_fixed_vocabulary_at_their_fixed_levels()
    {
        var logger = CheckLogger.Create(new CheckOptions { Logging = LoggingLevel.Debug });
        var violation = new EmptyTestViolation("project files should exist");

        logger.StartCheck("project files should exist");
        logger.Progress("selected 2 file(s)");
        logger.Violation(violation);
        logger.Metric("method count", 3);
        logger.EndCheck(1);

        Assert.Equal(
            new[]
            {
                "[INFO] start check: project files should exist",
                "[DEBUG] progress: selected 2 file(s)",
                "[WARN] violation: project files should exist",
                "[INFO] metric: method count = 3",
                "[INFO] end check: 1 violation(s)",
            },
            logger.Lines);
    }

    [Fact]
    public void Debug_lines_are_dropped_at_the_info_threshold()
    {
        var logger = CheckLogger.Create(new CheckOptions { Logging = LoggingLevel.Info });

        logger.StartCheck("project files should exist");
        logger.Progress("selected 2 file(s)");

        Assert.Equal(new[] { "[INFO] start check: project files should exist" }, logger.Lines);
    }

    [Fact]
    public void Info_lines_are_dropped_at_the_warn_threshold()
    {
        var logger = CheckLogger.Create(new CheckOptions { Logging = LoggingLevel.Warn });
        var violation = new EmptyTestViolation("project files should exist");

        logger.StartCheck("project files should exist");
        logger.Violation(violation);

        Assert.Equal(new[] { "[WARN] violation: project files should exist" }, logger.Lines);
    }

    [Fact]
    public void Violations_logs_every_violation_in_order()
    {
        var logger = CheckLogger.Create(new CheckOptions { Logging = LoggingLevel.Warn });

        logger.Violations(new Violation[]
        {
            new EmptyTestViolation("first rule"),
            new EmptyTestViolation("second rule"),
        });

        Assert.Equal(
            new[] { "[WARN] violation: first rule", "[WARN] violation: second rule" },
            logger.Lines);
    }

    [Fact]
    public void An_empty_test_violation_logs_its_rule_description_not_its_type_name()
    {
        var logger = CheckLogger.Create(new CheckOptions { Logging = LoggingLevel.Warn });

        logger.Violation(new EmptyTestViolation("project files with name 'Car.cs' should exist"));

        Assert.Equal(
            new[] { "[WARN] violation: project files with name 'Car.cs' should exist" },
            logger.Lines);
    }

    [Fact]
    public void Lines_returns_a_fresh_copy_every_time()
    {
        var logger = CheckLogger.Create(new CheckOptions { Logging = LoggingLevel.Info });
        logger.StartCheck("project files should exist");

        IReadOnlyList<string> first = logger.Lines;
        ((string[])first)[0] = "tampered";

        Assert.Equal("[INFO] start check: project files should exist", logger.Lines[0]);
    }

    [Fact]
    public void Flush_writes_a_timestamped_file_in_the_configured_directory_creating_it()
    {
        string directory = Path.Combine(Path.GetTempPath(), "archunit-logging-" + Guid.NewGuid().ToString("N"));
        var logger = CheckLogger.Create(
            new CheckOptions
            {
                Logging = LoggingLevel.Info,
                LogFile = new LogFileOptions { Directory = directory, FileNamePrefix = "suite" },
            },
            Timestamp);

        logger.StartCheck("project files should exist");
        logger.EndCheck(0);
        logger.Flush();

        try
        {
            Assert.Equal(
                "[INFO] start check: project files should exist" + Environment.NewLine
                + "[INFO] end check: 0 violation(s)" + Environment.NewLine,
                File.ReadAllText(Path.Combine(directory, "suite-20260831-214630.log")));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void Flush_with_append_merges_into_an_existing_file()
    {
        string directory = Path.Combine(Path.GetTempPath(), "archunit-logging-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, "suite-20260831-214630.log");
        File.WriteAllText(path, "[INFO] end check: 1 violation(s)" + Environment.NewLine);

        var logger = CheckLogger.Create(
            new CheckOptions
            {
                Logging = LoggingLevel.Info,
                LogFile = new LogFileOptions { Directory = directory, FileNamePrefix = "suite", Append = true },
            },
            Timestamp);

        logger.StartCheck("project files should exist");
        logger.Flush();

        try
        {
            Assert.Equal(
                "[INFO] end check: 1 violation(s)" + Environment.NewLine
                + "[INFO] start check: project files should exist" + Environment.NewLine,
                File.ReadAllText(path));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void Flush_without_append_replaces_an_existing_file()
    {
        string directory = Path.Combine(Path.GetTempPath(), "archunit-logging-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, "suite-20260831-214630.log");
        File.WriteAllText(path, "stale content" + Environment.NewLine);

        var logger = CheckLogger.Create(
            new CheckOptions
            {
                Logging = LoggingLevel.Info,
                LogFile = new LogFileOptions { Directory = directory, FileNamePrefix = "suite" },
            },
            Timestamp);

        logger.StartCheck("project files should exist");
        logger.Flush();

        try
        {
            Assert.Equal(
                "[INFO] start check: project files should exist" + Environment.NewLine,
                File.ReadAllText(path));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void Flush_with_no_file_configured_writes_nothing()
    {
        var logger = CheckLogger.Create(new CheckOptions { Logging = LoggingLevel.Info });

        logger.StartCheck("project files should exist");
        logger.Flush();

        Assert.Equal(new[] { "[INFO] start check: project files should exist" }, logger.Lines);
    }

    [Fact]
    public void Flush_with_logging_off_writes_no_file()
    {
        string directory = Path.Combine(Path.GetTempPath(), "archunit-logging-" + Guid.NewGuid().ToString("N"));
        var logger = CheckLogger.Create(
            new CheckOptions { LogFile = new LogFileOptions { Directory = directory } },
            Timestamp);

        logger.StartCheck("project files should exist");
        logger.Flush();

        Assert.False(Directory.Exists(directory));
    }

    [Fact]
    public void Flush_with_no_lines_writes_no_file()
    {
        string directory = Path.Combine(Path.GetTempPath(), "archunit-logging-" + Guid.NewGuid().ToString("N"));
        var logger = CheckLogger.Create(
            new CheckOptions
            {
                Logging = LoggingLevel.Info,
                LogFile = new LogFileOptions { Directory = directory },
            },
            Timestamp);

        logger.Flush();

        Assert.False(Directory.Exists(directory));
    }

    [Fact]
    public void Flush_an_unwritable_path_surfaces_as_a_technical_error()
    {
        string directory = Path.Combine(Path.GetTempPath(), "archunit-logging-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string blocker = Path.Combine(directory, "blocker");
        File.WriteAllText(blocker, "a file where the log directory should be");

        var logger = CheckLogger.Create(
            new CheckOptions
            {
                Logging = LoggingLevel.Info,
                LogFile = new LogFileOptions { Directory = Path.Combine(blocker, "logs") },
            },
            Timestamp);

        logger.StartCheck("project files should exist");

        try
        {
            Assert.Throws<TechnicalError>(() => logger.Flush());
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
