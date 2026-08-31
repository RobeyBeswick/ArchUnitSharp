using System.Reflection;

namespace ArchUnitSharp.Metrics.Tests;

public class ThresholdVerbTests
{
    private static readonly string[] TheSixVerbs =
    {
        "ShouldBeBelow",
        "ShouldBeAbove",
        "ShouldBe",
        "ShouldBeBelowOrEqual",
        "ShouldBeAboveOrEqual",
        "ShouldSatisfy",
    };

    [Theory]
    [InlineData(typeof(MetricSelection))]
    [InlineData(typeof(DistanceMetricSelection))]
    [InlineData(typeof(LcomMetricSelection))]
    [InlineData(typeof(CustomMetricSelection))]
    public void Each_selection_exposes_exactly_the_six_threshold_verbs(Type selection)
    {
        Assert.Equal(
            TheSixVerbs.OrderBy(name => name, StringComparer.Ordinal),
            ThresholdVerbs(selection));
    }

    [Fact]
    public void No_type_in_the_module_adds_a_synonym_threshold_verb()
    {
        string[] verbs = typeof(Metrics)
            .Assembly
            .GetExportedTypes()
            .SelectMany(type => ThresholdVerbs(type))
            .ToArray();

        Assert.All(verbs, verb => Assert.Contains(verb, TheSixVerbs));
    }

    private static string[] ThresholdVerbs(Type type) =>
        type
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Select(method => method.Name)
            .Where(name => name.StartsWith("Should", StringComparison.Ordinal))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
}
