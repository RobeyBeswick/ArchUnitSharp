using ArchUnitSharp.Metrics.Extraction;

namespace ArchUnitSharp.Metrics.Tests;

public class MetricsExtractorTests
{
    [Fact]
    public void Extract_counts_lines_statements_imports_classes_and_interfaces()
    {
        const string source =
            "using System;\n" +
            "\n" +
            "namespace App;\n" +
            "\n" +
            "public class Car\n" +
            "{\n" +
            "    private int _speed;\n" +
            "\n" +
            "    public void Drive()\n" +
            "    {\n" +
            "        _speed = 0;\n" +
            "        if (_speed > 10)\n" +
            "        {\n" +
            "            _speed = 10;\n" +
            "        }\n" +
            "    }\n" +
            "}\n";

        FileInfo info = MetricsExtractor.Extract("src/Models/Car.cs", source);

        Assert.Equal("src/Models/Car.cs", info.Path);
        Assert.Equal(14, info.LinesOfCode);
        Assert.Equal(3, info.StatementCount);
        Assert.Equal(1, info.ImportCount);
        Assert.Equal(1, info.ClassCount);
        Assert.Equal(0, info.InterfaceCount);
    }

    [Fact]
    public void Extract_counts_a_using_inside_a_namespace_and_a_global_using()
    {
        const string source =
            "global using System;\n" +
            "namespace App\n" +
            "{\n" +
            "    using System.Linq;\n" +
            "    public class Car { }\n" +
            "}\n";

        FileInfo info = MetricsExtractor.Extract("src/Car.cs", source);

        Assert.Equal(2, info.ImportCount);
        Assert.Equal(1, info.ClassCount);
    }

    [Fact]
    public void Extract_does_not_count_statements_or_imports_in_disabled_regions()
    {
        const string source =
            "using System;\n" +
            "#if false\n" +
            "using System.IO;\n" +
            "Console.WriteLine(\"x\");\n" +
            "#endif\n" +
            "public class Car { }\n";

        FileInfo info = MetricsExtractor.Extract("src/Car.cs", source);

        Assert.Equal(1, info.ImportCount);
        Assert.Equal(0, info.StatementCount);
        Assert.Equal(1, info.ClassCount);
    }

    [Fact]
    public void Extract_counts_top_level_statements()
    {
        const string source =
            "using System;\n" +
            "Console.WriteLine(\"hi\");\n";

        FileInfo info = MetricsExtractor.Extract("src/Program.cs", source);

        Assert.Equal(1, info.StatementCount);
        Assert.Equal(0, info.ClassCount);
    }

    [Fact]
    public void Extract_counts_a_local_function_as_a_statement()
    {
        const string source =
            "public class Car\n" +
            "{\n" +
            "    public void Drive()\n" +
            "    {\n" +
            "        int Go() => 1;\n" +
            "        Go();\n" +
            "    }\n" +
            "}\n";

        FileInfo info = MetricsExtractor.Extract("src/Car.cs", source);

        Assert.Equal(2, info.StatementCount);
    }

    [Fact]
    public void Extract_names_nested_classes_and_keeps_their_members_to_themselves()
    {
        const string source =
            "namespace App;\n" +
            "public class Outer\n" +
            "{\n" +
            "    private int a, b;\n" +
            "    public class Inner { public void Go() { } }\n" +
            "}\n";

        FileInfo info = MetricsExtractor.Extract("src/Outer.cs", source);

        Assert.Equal(2, info.ClassCount);
        Assert.Equal(
            new[] { "src/Outer.cs:App.Outer", "src/Outer.cs:App.Outer.Inner" },
            info.ClassInfos.Select(static classInfo => classInfo.Identifier));

        ClassInfo outer = info.ClassInfos.Single(classInfo => classInfo.Name == "App.Outer");
        Assert.Empty(outer.Methods);
        Assert.Equal(new[] { "a", "b" }, outer.Fields.Select(static field => field.Name));

        ClassInfo inner = info.ClassInfos.Single(classInfo => classInfo.Name == "App.Outer.Inner");
        Assert.Equal(new[] { "Go" }, inner.Methods.Select(static method => method.Name));
        Assert.Empty(inner.Fields);
    }

    [Fact]
    public void Extract_does_not_count_records_or_structs_as_classes()
    {
        const string source =
            "namespace App;\n" +
            "public record Point(int X, int Y);\n" +
            "public struct Pair { public int A; public int B; }\n" +
            "public class Real { }\n";

        FileInfo info = MetricsExtractor.Extract("src/Models.cs", source);

        Assert.Equal(1, info.ClassCount);
        Assert.Equal(0, info.InterfaceCount);
        Assert.Equal(new[] { "App.Real" }, info.ClassInfos.Select(static classInfo => classInfo.Name));
    }

    [Fact]
    public void Extract_counts_interface_declarations()
    {
        const string source =
            "namespace App;\n" +
            "public interface IThing { void Do(); }\n" +
            "public class Car : IThing { public void Do() { } }\n";

        FileInfo info = MetricsExtractor.Extract("src/Car.cs", source);

        Assert.Equal(1, info.InterfaceCount);
        Assert.Equal(1, info.ClassCount);
        Assert.Equal(new[] { "App.Car" }, info.ClassInfos.Select(static classInfo => classInfo.Name));
    }

    [Fact]
    public void Extract_counts_only_plain_methods_not_constructors_operators_or_accessors()
    {
        const string source =
            "namespace App;\n" +
            "public class Car\n" +
            "{\n" +
            "    public Car() { }\n" +
            "    ~Car() { }\n" +
            "    public int X { get; set; }\n" +
            "    public static Car operator +(Car a, Car b) => a;\n" +
            "    public void Drive() { }\n" +
            "}\n";

        FileInfo info = MetricsExtractor.Extract("src/Car.cs", source);
        ClassInfo car = Assert.Single(info.ClassInfos);

        MethodInfo method = Assert.Single(car.Methods);
        Assert.Equal("Drive", method.Name);
    }

    [Fact]
    public void Extract_lists_a_classes_methods_and_fields_sorted_by_name()
    {
        const string source =
            "namespace App;\n" +
            "public class Car\n" +
            "{\n" +
            "    private int _wheels;\n" +
            "    private string _name;\n" +
            "    public void Drive() { }\n" +
            "    public void Stop() { }\n" +
            "}\n";

        FileInfo info = MetricsExtractor.Extract("src/Car.cs", source);
        ClassInfo car = Assert.Single(info.ClassInfos);

        Assert.Equal(new[] { "Drive", "Stop" }, car.Methods.Select(static method => method.Name));
        Assert.Equal(new[] { "_name", "_wheels" }, car.Fields.Select(static field => field.Name));
    }

    [Fact]
    public void Extract_counts_windows_line_endings_once()
    {
        FileInfo info = MetricsExtractor.Extract("src/Car.cs", "namespace App;\r\n\r\npublic class Car { }\r\n");

        Assert.Equal(2, info.LinesOfCode);
    }

    [Fact]
    public void Extract_records_the_fields_each_method_accesses()
    {
        const string source =
            "namespace App;\n" +
            "public class Car\n" +
            "{\n" +
            "    private int _speed;\n" +
            "    private int _gear;\n" +
            "    public void Drive() { _speed = 1; _gear = 2; }\n" +
            "    public void Stop() { _speed = 0; }\n" +
            "}\n";

        FileInfo info = MetricsExtractor.Extract("src/Car.cs", source);
        ClassInfo car = Assert.Single(info.ClassInfos);

        Assert.Equal(
            new[] { "_gear", "_speed" },
            car.Methods.Single(method => method.Name == "Drive").AccessedFields);
        Assert.Equal(
            new[] { "_speed" },
            car.Methods.Single(method => method.Name == "Stop").AccessedFields);
    }

    [Fact]
    public void Extract_records_the_methods_that_access_each_field()
    {
        const string source =
            "namespace App;\n" +
            "public class Car\n" +
            "{\n" +
            "    private int _speed;\n" +
            "    private int _gear;\n" +
            "    public void Drive() { _speed = 1; _gear = 2; }\n" +
            "    public void Stop() { _speed = 0; }\n" +
            "}\n";

        FileInfo info = MetricsExtractor.Extract("src/Car.cs", source);
        ClassInfo car = Assert.Single(info.ClassInfos);

        Assert.Equal(
            new[] { "Drive", "Stop" },
            car.Fields.Single(field => field.Name == "_speed").AccessedBy);
        Assert.Equal(
            new[] { "Drive" },
            car.Fields.Single(field => field.Name == "_gear").AccessedBy);
    }

    [Fact]
    public void Extract_does_not_count_locals_or_other_identifiers_as_field_accesses()
    {
        const string source =
            "namespace App;\n" +
            "public class Car\n" +
            "{\n" +
            "    private int _speed;\n" +
            "    public void Drive() { int count = _speed; }\n" +
            "}\n";

        FileInfo info = MetricsExtractor.Extract("src/Car.cs", source);
        ClassInfo car = Assert.Single(info.ClassInfos);

        Assert.Equal(
            new[] { "_speed" },
            car.Methods.Single(method => method.Name == "Drive").AccessedFields);
    }

    [Fact]
    public void Extract_counts_a_field_referenced_through_this_and_expression_bodies()
    {
        const string source =
            "namespace App;\n" +
            "public class Car\n" +
            "{\n" +
            "    private int _speed;\n" +
            "    public int Get() => this._speed;\n" +
            "}\n";

        FileInfo info = MetricsExtractor.Extract("src/Car.cs", source);
        ClassInfo car = Assert.Single(info.ClassInfos);

        Assert.Equal(
            new[] { "_speed" },
            car.Methods.Single(method => method.Name == "Get").AccessedFields);
    }

    [Fact]
    public void Extract_does_not_scan_constructors_or_accessors_for_field_accesses()
    {
        const string source =
            "namespace App;\n" +
            "public class Car\n" +
            "{\n" +
            "    private int _speed;\n" +
            "    public Car() { _speed = 1; }\n" +
            "    public int Speed { get => _speed; set => _speed = value; }\n" +
            "    public void Drive() { _speed = 2; }\n" +
            "}\n";

        FileInfo info = MetricsExtractor.Extract("src/Car.cs", source);
        ClassInfo car = Assert.Single(info.ClassInfos);

        MethodInfo drive = Assert.Single(car.Methods);
        Assert.Equal("Drive", drive.Name);
        Assert.Equal(new[] { "_speed" }, drive.AccessedFields);
        Assert.Equal(
            new[] { "Drive" },
            car.Fields.Single(field => field.Name == "_speed").AccessedBy);
    }

    [Fact]
    public void Extract_leaves_a_field_no_method_accesses_with_an_empty_list()
    {
        const string source =
            "namespace App;\n" +
            "public class Car\n" +
            "{\n" +
            "    private int _speed;\n" +
            "    private int _unused;\n" +
            "    public void Drive() { _speed = 1; }\n" +
            "}\n";

        FileInfo info = MetricsExtractor.Extract("src/Car.cs", source);
        ClassInfo car = Assert.Single(info.ClassInfos);

        Assert.Empty(car.Fields.Single(field => field.Name == "_unused").AccessedBy);
    }

    [Fact]
    public void Extract_does_not_count_the_methods_own_name_as_a_field_access()
    {
        const string source =
            "namespace App;\n" +
            "public class Car\n" +
            "{\n" +
            "    private int _speed;\n" +
            "    public int _speed() => 1;\n" +
            "}\n";

        FileInfo info = MetricsExtractor.Extract("src/Car.cs", source);
        ClassInfo car = Assert.Single(info.ClassInfos);

        Assert.Empty(Assert.Single(car.Methods).AccessedFields);
    }

    [Fact]
    public void Extract_handles_an_empty_source()
    {
        FileInfo info = MetricsExtractor.Extract("src/Empty.cs", string.Empty);

        Assert.Equal(0, info.LinesOfCode);
        Assert.Equal(0, info.StatementCount);
        Assert.Equal(0, info.ImportCount);
        Assert.Equal(0, info.ClassCount);
        Assert.Equal(0, info.InterfaceCount);
        Assert.Empty(info.ClassInfos);
    }

    [Fact]
    public void Extract_qualifies_a_class_name_with_its_file_scoped_namespace()
    {
        FileInfo info = MetricsExtractor.Extract("src/Car.cs", "namespace App.Models;\npublic class Car { }\n");

        Assert.Equal(new[] { "App.Models.Car" }, info.ClassInfos.Select(static classInfo => classInfo.Name));
    }

    [Fact]
    public void Extract_rejects_a_null_path()
    {
        Assert.Throws<ArgumentNullException>(() => MetricsExtractor.Extract(null!, "text"));
    }

    [Fact]
    public void Extract_rejects_null_source_text()
    {
        Assert.Throws<ArgumentNullException>(() => MetricsExtractor.Extract("a.cs", null!));
    }
}
