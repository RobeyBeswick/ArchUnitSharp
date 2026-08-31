using ArchUnitSharp.Metrics.Calculation;

namespace ArchUnitSharp.Metrics.Tests;

public class DistanceMetricsCalculationTests
{
    [Fact]
    public void Abstractness_is_the_abstract_types_over_the_types()
    {
        double value = Calculation.DistanceMetrics.ValueOf(
            Calculation.DistanceMetrics.Abstractness(),
            Info(types: 2, abstractTypes: 1, incoming: 2, outgoing: 2, files: 5));

        Assert.Equal(0.5, value);
    }

    [Fact]
    public void Abstractness_is_zero_for_a_file_with_no_types()
    {
        double value = Calculation.DistanceMetrics.ValueOf(
            Calculation.DistanceMetrics.Abstractness(),
            Info(types: 0, abstractTypes: 0, incoming: 0, outgoing: 0, files: 1));

        Assert.Equal(0.0, value);
    }

    [Fact]
    public void Instability_is_the_efferent_share_of_all_couplings()
    {
        double value = Calculation.DistanceMetrics.ValueOf(
            Calculation.DistanceMetrics.Instability(),
            Info(types: 2, abstractTypes: 1, incoming: 2, outgoing: 2, files: 5));

        Assert.Equal(0.5, value);
    }

    [Fact]
    public void Instability_is_zero_for_an_uncoupled_file()
    {
        double value = Calculation.DistanceMetrics.ValueOf(
            Calculation.DistanceMetrics.Instability(),
            Info());

        Assert.Equal(0.0, value);
    }

    [Fact]
    public void Distance_from_the_main_sequence_is_the_absolute_deviation_from_one()
    {
        double value = Calculation.DistanceMetrics.ValueOf(
            Calculation.DistanceMetrics.DistanceFromMainSequence(),
            Info(types: 2, abstractTypes: 1, incoming: 2, outgoing: 2, files: 5));

        Assert.Equal(0.0, value);
    }

    [Fact]
    public void Distance_is_one_for_a_concrete_uncoupled_file()
    {
        double value = Calculation.DistanceMetrics.ValueOf(
            Calculation.DistanceMetrics.DistanceFromMainSequence(),
            Info());

        Assert.Equal(1.0, value);
    }

    [Fact]
    public void Coupling_factor_is_the_couplings_over_the_possible_couplings()
    {
        double value = Calculation.DistanceMetrics.ValueOf(
            Calculation.DistanceMetrics.CouplingFactor(),
            Info(types: 2, abstractTypes: 1, incoming: 2, outgoing: 2, files: 5));

        Assert.Equal(0.5, value);
    }

    [Fact]
    public void Coupling_factor_is_zero_for_a_one_file_project()
    {
        double value = Calculation.DistanceMetrics.ValueOf(
            Calculation.DistanceMetrics.CouplingFactor(),
            Info(files: 1));

        Assert.Equal(0.0, value);
    }

    [Fact]
    public void Normalised_distance_discounts_a_short_file()
    {
        double value = Calculation.DistanceMetrics.ValueOf(
            Calculation.DistanceMetrics.NormalisedDistance(),
            Info(incoming: 1, files: 2, lines: 50));

        Assert.Equal(0.75, value);
    }

    [Fact]
    public void Normalised_distance_caps_the_discount_at_fifty_percent()
    {
        double value = Calculation.DistanceMetrics.ValueOf(
            Calculation.DistanceMetrics.NormalisedDistance(),
            Info(incoming: 1, files: 2, lines: 200));

        Assert.Equal(0.5, value);
    }

    [Fact]
    public void Normalised_distance_is_the_full_distance_for_an_empty_file()
    {
        double value = Calculation.DistanceMetrics.ValueOf(
            Calculation.DistanceMetrics.NormalisedDistance(),
            Info());

        Assert.Equal(1.0, value);
    }

    [Fact]
    public void In_zone_detects_the_zone_of_pain()
    {
        Assert.True(Calculation.DistanceMetrics.InZone(Info(incoming: 3, files: 4), DistanceZone.Pain));
    }

    [Fact]
    public void In_zone_detects_the_zone_of_uselessness()
    {
        Assert.True(Calculation.DistanceMetrics.InZone(
            Info(types: 1, abstractTypes: 1, outgoing: 3, files: 4),
            DistanceZone.Uselessness));
    }

    [Fact]
    public void In_zone_uses_strict_boundaries()
    {
        var painAbstractnessBoundary = Info(types: 10, abstractTypes: 3, incoming: 3, outgoing: 1, files: 5);
        var painInstabilityBoundary = Info(types: 2, incoming: 7, outgoing: 3, files: 8);
        var uselessnessAbstractnessBoundary = Info(types: 10, abstractTypes: 7, incoming: 0, outgoing: 3, files: 4);
        var uselessnessInstabilityBoundary = Info(types: 2, abstractTypes: 2, incoming: 3, outgoing: 7, files: 8);

        Assert.False(Calculation.DistanceMetrics.InZone(painAbstractnessBoundary, DistanceZone.Pain));
        Assert.False(Calculation.DistanceMetrics.InZone(painInstabilityBoundary, DistanceZone.Pain));
        Assert.False(Calculation.DistanceMetrics.InZone(uselessnessAbstractnessBoundary, DistanceZone.Uselessness));
        Assert.False(Calculation.DistanceMetrics.InZone(uselessnessInstabilityBoundary, DistanceZone.Uselessness));
    }

    [Fact]
    public void In_zone_rejects_an_unknown_zone()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Calculation.DistanceMetrics.InZone(Info(), (DistanceZone)99));
    }

    [Fact]
    public void The_factories_build_a_file_metric_with_a_file_subject()
    {
        Assert.Equal(MetricSubject.File, Calculation.DistanceMetrics.Instability().Subject);
        Assert.Equal(DistanceMetricKind.Instability, Calculation.DistanceMetrics.Instability().Kind);
        Assert.Equal(DistanceMetricKind.Abstractness, Calculation.DistanceMetrics.Abstractness().Kind);
        Assert.Equal(DistanceMetricKind.DistanceFromMainSequence, Calculation.DistanceMetrics.DistanceFromMainSequence().Kind);
        Assert.Equal(DistanceMetricKind.CouplingFactor, Calculation.DistanceMetrics.CouplingFactor().Kind);
        Assert.Equal(DistanceMetricKind.NormalisedDistance, Calculation.DistanceMetrics.NormalisedDistance().Kind);
    }

    [Fact]
    public void ValueOf_rejects_a_null_metric()
    {
        Assert.Throws<ArgumentNullException>(() =>
            Calculation.DistanceMetrics.ValueOf((DistanceMetric)null!, Info()));
    }

    [Fact]
    public void ValueOf_rejects_a_null_info()
    {
        Assert.Throws<ArgumentNullException>(() =>
            Calculation.DistanceMetrics.ValueOf(Calculation.DistanceMetrics.Instability(), null!));
    }

    [Fact]
    public void ValueOf_rejects_an_unknown_kind()
    {
        var metric = new DistanceMetric((DistanceMetricKind)99, MetricSubject.File);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Calculation.DistanceMetrics.ValueOf(metric, Info()));
    }

    [Fact]
    public void In_zone_rejects_null_info()
    {
        Assert.Throws<ArgumentNullException>(() =>
            Calculation.DistanceMetrics.InZone(null!, DistanceZone.Pain));
    }

    private static DistanceInfo Info(
        int types = 1,
        int abstractTypes = 0,
        int incoming = 0,
        int outgoing = 0,
        int files = 1,
        int lines = 0) =>
        new("src/Example.cs", types, abstractTypes, lines, incoming, outgoing, files);
}
