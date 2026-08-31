using ArchUnitSharp.Common.Extraction;
using ArchUnitSharp.Metrics.Rendering;

namespace ArchUnitSharp.Metrics.Tests;

public class MetricsReportDataTests
{
    [Fact]
    public void Count_measures_every_count_metric_over_the_selected_subjects()
    {
        const string source =
            "namespace App;\n" +
            "public class Car\n" +
            "{\n" +
            "    private int _speed;\n" +
            "    public void Drive() { _speed = 0; }\n" +
            "}\n";

        IReadOnlyDictionary<string, string> data = MetricsReportData.Count(
            Project("src/Models/Car.cs", source));

        Assert.Equal(7, data.Count);
        Assert.Equal("1", data["method count [src/Models/Car.cs:App.Car]"]);
        Assert.Equal("1", data["field count [src/Models/Car.cs:App.Car]"]);
        Assert.Equal("6", data["lines of code [src/Models/Car.cs]"]);
        Assert.Equal("1", data["statements [src/Models/Car.cs]"]);
        Assert.Equal("0", data["imports [src/Models/Car.cs]"]);
        Assert.Equal("1", data["classes [src/Models/Car.cs]"]);
        Assert.Equal("0", data["interfaces [src/Models/Car.cs]"]);
    }

    [Fact]
    public void Count_measures_a_class_selector_only_over_the_matching_classes_and_whole_files()
    {
        const string source =
            "namespace App;\n" +
            "public class Car { }\n" +
            "public class Bike { }\n";

        IReadOnlyDictionary<string, string> data = MetricsReportData.Count(
            Project("src/Car.cs", source).ForClassesMatching("*.Bike"));

        Assert.Equal(7, data.Count);
        Assert.Equal("0", data["method count [src/Car.cs:App.Bike]"]);
        Assert.Equal("0", data["field count [src/Car.cs:App.Bike]"]);
        Assert.False(data.ContainsKey("method count [src/Car.cs:App.Car]"));
        Assert.Equal("2", data["classes [src/Car.cs]"]);
        Assert.Equal("0", data["interfaces [src/Car.cs]"]);
        Assert.Equal("3", data["lines of code [src/Car.cs]"]);
    }

    [Fact]
    public void Lcom_measures_all_eight_metrics_over_each_selected_class()
    {
        const string split =
            "namespace App;\n" +
            "public class Split\n" +
            "{\n" +
            "    private int _a;\n" +
            "    private int _b;\n" +
            "    public void A() { _a = 1; }\n" +
            "    public void B() { _b = 2; }\n" +
            "}\n";

        IReadOnlyDictionary<string, string> data = MetricsReportData.Lcom(
            Project("src/Split.cs", split));

        Assert.Equal(8, data.Count);
        Assert.Equal("1", data["lcom96a [src/Split.cs:App.Split]"]);
        Assert.Equal("0.5", data["lcom96b [src/Split.cs:App.Split]"]);
        Assert.Equal("1", data["lcom1 [src/Split.cs:App.Split]"]);
        Assert.Equal("0.5", data["lcom2 [src/Split.cs:App.Split]"]);
        Assert.Equal("1", data["lcom3 [src/Split.cs:App.Split]"]);
        Assert.Equal("2", data["lcom4 [src/Split.cs:App.Split]"]);
        Assert.Equal("1", data["lcom5 [src/Split.cs:App.Split]"]);
        Assert.Equal("1", data["lcom* [src/Split.cs:App.Split]"]);
    }

    [Fact]
    public void Distance_measures_all_five_metrics_over_each_selected_file()
    {
        var sources = new Dictionary<string, string>
        {
            ["src/Models/IThing.cs"] = "namespace App;\npublic interface IThing { }\n",
            ["src/Services/Car.cs"] = "namespace App;\npublic class Car : IThing { }\n",
            ["src/App/Program.cs"] = "namespace App;\npublic class Program { }\n",
        };
        var scope = new Metrics(
            Graph(
                Self("src/Models/IThing.cs"),
                Self("src/Services/Car.cs"),
                Self("src/App/Program.cs"),
                Using("src/Services/Car.cs", "src/Models/IThing.cs"),
                Using("src/App/Program.cs", "src/Services/Car.cs")),
            identifier => sources[identifier]);

        IReadOnlyDictionary<string, string> data = MetricsReportData.Distance(scope);

        Assert.Equal(15, data.Count);
        Assert.Equal("0", data["abstractness [src/Services/Car.cs]"]);
        Assert.Equal("0.5", data["instability [src/Services/Car.cs]"]);
        Assert.Equal("0.5", data["distance from main sequence [src/Services/Car.cs]"]);
        Assert.Equal("0.5", data["coupling factor [src/Services/Car.cs]"]);
        Assert.Equal("0.495", data["normalised distance [src/Services/Car.cs]"]);
        Assert.Equal("1", data["abstractness [src/Models/IThing.cs]"]);
        Assert.Equal("0", data["instability [src/Models/IThing.cs]"]);
        Assert.Equal("1", data["instability [src/App/Program.cs]"]);
    }

    [Fact]
    public void An_empty_scope_yields_an_empty_data_map()
    {
        IReadOnlyDictionary<string, string> data = MetricsReportData.Count(new Metrics(Graph()));

        Assert.Empty(data);
    }

    [Fact]
    public void A_scope_without_a_source_provider_raises_a_user_error()
    {
        var scope = new Metrics(Graph(Self("src/A.cs")));

        Assert.Throws<UserError>(() => MetricsReportData.Count(scope));
    }

    [Fact]
    public void Each_method_rejects_a_null_scope()
    {
        Assert.Throws<ArgumentNullException>(() => MetricsReportData.Count(null!));
        Assert.Throws<ArgumentNullException>(() => MetricsReportData.Lcom(null!));
        Assert.Throws<ArgumentNullException>(() => MetricsReportData.Distance(null!));
    }

    private static Metrics Project(string path, string source) =>
        new(Graph(Self(path)), _ => source);

    private static Graph Graph(params Edge[] edges) => new(edges);

    private static Edge Self(string file) => new(file, file, external: false, ImportKind.None);

    private static Edge Using(string source, string target) =>
        new(source, target, external: false, ImportKind.Using);
}
