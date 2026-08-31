using ArchUnitSharp.Metrics.Calculation;

namespace ArchUnitSharp.Metrics.Tests;

public class MetricsCalculationTests
{
    [Fact]
    public void LinesOfCode_reads_the_files_line_count()
    {
        int value = Calculation.CountMetrics.ValueOf(
            Calculation.CountMetrics.LinesOfCode(),
            File(lines: 42, statements: 0, imports: 0, classes: 0, interfaces: 0));

        Assert.Equal(42, value);
    }

    [Fact]
    public void Statements_reads_the_files_statement_count()
    {
        int value = Calculation.CountMetrics.ValueOf(
            Calculation.CountMetrics.Statements(),
            File(lines: 0, statements: 7, imports: 0, classes: 0, interfaces: 0));

        Assert.Equal(7, value);
    }

    [Fact]
    public void Imports_reads_the_files_import_count()
    {
        int value = Calculation.CountMetrics.ValueOf(
            Calculation.CountMetrics.Imports(),
            File(lines: 0, statements: 0, imports: 3, classes: 0, interfaces: 0));

        Assert.Equal(3, value);
    }

    [Fact]
    public void Classes_reads_the_files_class_count()
    {
        int value = Calculation.CountMetrics.ValueOf(
            Calculation.CountMetrics.Classes(),
            File(lines: 0, statements: 0, imports: 0, classes: 2, interfaces: 0));

        Assert.Equal(2, value);
    }

    [Fact]
    public void Interfaces_reads_the_files_interface_count()
    {
        int value = Calculation.CountMetrics.ValueOf(
            Calculation.CountMetrics.Interfaces(),
            File(lines: 0, statements: 0, imports: 0, classes: 0, interfaces: 1));

        Assert.Equal(1, value);
    }

    [Fact]
    public void MethodCount_counts_the_classes_methods()
    {
        int value = Calculation.CountMetrics.ValueOf(
            Calculation.CountMetrics.MethodCount(),
            Class(methods: 5, fields: 0));

        Assert.Equal(5, value);
    }

    [Fact]
    public void FieldCount_counts_the_classes_fields()
    {
        int value = Calculation.CountMetrics.ValueOf(
            Calculation.CountMetrics.FieldCount(),
            Class(methods: 0, fields: 4));

        Assert.Equal(4, value);
    }

    [Fact]
    public void A_file_metric_over_a_class_raises()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Calculation.CountMetrics.ValueOf(
                Calculation.CountMetrics.LinesOfCode(),
                Class(methods: 0, fields: 0)));
    }

    [Fact]
    public void A_class_metric_over_a_file_raises()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Calculation.CountMetrics.ValueOf(
                Calculation.CountMetrics.MethodCount(),
                File(lines: 0, statements: 0, imports: 0, classes: 0, interfaces: 0)));
    }

    [Fact]
    public void The_factories_build_a_file_metric_with_a_file_subject()
    {
        Assert.Equal(MetricSubject.File, Calculation.CountMetrics.Imports().Subject);
        Assert.Equal(CountMetricKind.Imports, Calculation.CountMetrics.Imports().Kind);
    }

    [Fact]
    public void The_factories_build_a_class_metric_with_a_class_subject()
    {
        Assert.Equal(MetricSubject.Class, Calculation.CountMetrics.FieldCount().Subject);
        Assert.Equal(CountMetricKind.FieldCount, Calculation.CountMetrics.FieldCount().Kind);
    }

    [Fact]
    public void ValueOf_rejects_a_null_metric_for_a_file()
    {
        Assert.Throws<ArgumentNullException>(() =>
            Calculation.CountMetrics.ValueOf(
                (Metric)null!,
                File(lines: 0, statements: 0, imports: 0, classes: 0, interfaces: 0)));
    }

    [Fact]
    public void ValueOf_rejects_a_null_file()
    {
        Assert.Throws<ArgumentNullException>(() =>
            Calculation.CountMetrics.ValueOf(Calculation.CountMetrics.Classes(), (FileInfo)null!));
    }

    [Fact]
    public void ValueOf_rejects_a_null_metric_for_a_class()
    {
        Assert.Throws<ArgumentNullException>(() =>
            Calculation.CountMetrics.ValueOf((Metric)null!, Class(methods: 0, fields: 0)));
    }

    [Fact]
    public void ValueOf_rejects_a_null_class_info()
    {
        Assert.Throws<ArgumentNullException>(() =>
            Calculation.CountMetrics.ValueOf(Calculation.CountMetrics.MethodCount(), (ClassInfo)null!));
    }

    private static FileInfo File(int lines, int statements, int imports, int classes, int interfaces) =>
        new("src/App/Program.cs", lines, statements, imports, classes, interfaces, Array.Empty<ClassInfo>());

    private static ClassInfo Class(int methods, int fields)
    {
        MethodInfo[] methodInfos = Enumerable.Range(0, methods)
            .Select(static index => new MethodInfo($"M{index}"))
            .ToArray();
        FieldInfo[] fieldInfos = Enumerable.Range(0, fields)
            .Select(static index => new FieldInfo($"F{index}"))
            .ToArray();
        return new ClassInfo("App.Car", "src/App/Car.cs", methodInfos, fieldInfos);
    }
}
