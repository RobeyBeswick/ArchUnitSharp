using ArchUnitSharp.Metrics.Calculation;

namespace ArchUnitSharp.Metrics.Tests;

public class LcomMetricsCalculationTests
{
    [Fact]
    public void Lcom96a_is_zero_when_every_method_accesses_every_field()
    {
        ClassInfo car = Class(("A", new[] { "_a", "_b" }), ("B", new[] { "_a", "_b" }));

        Assert.Equal(0.0, Calculation.LcomMetrics.ValueOf(Calculation.LcomMetrics.Lcom96a(), car));
    }

    [Fact]
    public void Lcom96a_is_one_when_each_method_accesses_its_own_field()
    {
        ClassInfo car = Class(("A", new[] { "_a" }), ("B", new[] { "_b" }));

        Assert.Equal(1.0, Calculation.LcomMetrics.ValueOf(Calculation.LcomMetrics.Lcom96a(), car));
    }

    [Fact]
    public void Lcom96a_measures_the_normalised_method_field_distance()
    {
        ClassInfo car = Class(
            ("A", new[] { "_a", "_b" }),
            ("B", new[] { "_a" }),
            ("C", new[] { "_a" }));

        Assert.Equal(0.5, Calculation.LcomMetrics.ValueOf(Calculation.LcomMetrics.Lcom96a(), car));
    }

    [Fact]
    public void Lcom96a_is_zero_for_a_single_method_class()
    {
        ClassInfo car = Class(("A", new[] { "_a" }));

        Assert.Equal(0.0, Calculation.LcomMetrics.ValueOf(Calculation.LcomMetrics.Lcom96a(), car));
    }

    [Fact]
    public void Lcom96a_is_zero_for_a_class_without_fields()
    {
        ClassInfo car = Class(("A", Array.Empty<string>()), ("B", Array.Empty<string>()));

        Assert.Equal(0.0, Calculation.LcomMetrics.ValueOf(Calculation.LcomMetrics.Lcom96a(), car));
    }

    [Fact]
    public void Lcom96b_is_zero_when_every_method_accesses_every_field()
    {
        ClassInfo car = Class(("A", new[] { "_a", "_b" }), ("B", new[] { "_a", "_b" }));

        Assert.Equal(0.0, Calculation.LcomMetrics.ValueOf(Calculation.LcomMetrics.Lcom96b(), car));
    }

    [Fact]
    public void Lcom96b_is_half_when_each_method_accesses_its_own_of_two_fields()
    {
        ClassInfo car = Class(("A", new[] { "_a" }), ("B", new[] { "_b" }));

        Assert.Equal(0.5, Calculation.LcomMetrics.ValueOf(Calculation.LcomMetrics.Lcom96b(), car));
    }

    [Fact]
    public void Lcom96b_measures_the_method_field_density_complement()
    {
        ClassInfo car = Class(("A", new[] { "_a" }), ("B", Array.Empty<string>()));

        Assert.Equal(0.5, Calculation.LcomMetrics.ValueOf(Calculation.LcomMetrics.Lcom96b(), car));
    }

    [Fact]
    public void Lcom96b_is_zero_for_a_single_method_class()
    {
        ClassInfo car = Class(("A", new[] { "_a" }));

        Assert.Equal(0.0, Calculation.LcomMetrics.ValueOf(Calculation.LcomMetrics.Lcom96b(), car));
    }

    [Fact]
    public void Lcom96b_is_zero_for_a_class_without_fields()
    {
        ClassInfo car = Class(("A", Array.Empty<string>()), ("B", Array.Empty<string>()));

        Assert.Equal(0.0, Calculation.LcomMetrics.ValueOf(Calculation.LcomMetrics.Lcom96b(), car));
    }

    [Fact]
    public void Lcom1_is_the_number_of_disjoint_pairs_when_no_pair_shares()
    {
        ClassInfo car = Class(
            ("A", new[] { "_a" }),
            ("B", new[] { "_b" }),
            ("C", new[] { "_c" }));

        Assert.Equal(3.0, Calculation.LcomMetrics.ValueOf(Calculation.LcomMetrics.Lcom1(), car));
    }

    [Fact]
    public void Lcom1_is_zero_when_every_pair_shares_a_field()
    {
        ClassInfo car = Class(
            ("A", new[] { "_a" }),
            ("B", new[] { "_a" }),
            ("C", new[] { "_a" }));

        Assert.Equal(0.0, Calculation.LcomMetrics.ValueOf(Calculation.LcomMetrics.Lcom1(), car));
    }

    [Fact]
    public void Lcom1_is_the_difference_of_disjoint_and_sharing_pairs()
    {
        ClassInfo car = Class(
            ("A", new[] { "_a" }),
            ("B", new[] { "_a" }),
            ("C", new[] { "_b" }));

        Assert.Equal(1.0, Calculation.LcomMetrics.ValueOf(Calculation.LcomMetrics.Lcom1(), car));
    }

    [Fact]
    public void Lcom1_clamps_a_negative_difference_at_zero()
    {
        ClassInfo car = Class(
            ("A", new[] { "_a", "_b" }),
            ("B", new[] { "_a", "_b" }),
            ("C", new[] { "_a" }));

        Assert.Equal(0.0, Calculation.LcomMetrics.ValueOf(Calculation.LcomMetrics.Lcom1(), car));
    }

    [Fact]
    public void Lcom1_is_zero_for_a_single_method_class()
    {
        ClassInfo car = Class(("A", new[] { "_a" }));

        Assert.Equal(0.0, Calculation.LcomMetrics.ValueOf(Calculation.LcomMetrics.Lcom1(), car));
    }

    [Fact]
    public void Lcom4_counts_the_connected_components_of_the_method_graph()
    {
        ClassInfo car = Class(
            ("A", new[] { "_a" }),
            ("B", new[] { "_a" }),
            ("C", new[] { "_b" }));

        Assert.Equal(2.0, Calculation.LcomMetrics.ValueOf(Calculation.LcomMetrics.Lcom4(), car));
    }

    [Fact]
    public void Lcom4_is_one_when_all_methods_are_connected()
    {
        ClassInfo car = Class(
            ("A", new[] { "_a" }),
            ("B", new[] { "_a" }),
            ("C", new[] { "_a" }));

        Assert.Equal(1.0, Calculation.LcomMetrics.ValueOf(Calculation.LcomMetrics.Lcom4(), car));
    }

    [Fact]
    public void Lcom4_traverses_indirect_field_sharing()
    {
        ClassInfo car = Class(
            ("A", new[] { "_a" }),
            ("B", new[] { "_a", "_b" }),
            ("C", new[] { "_b" }),
            ("D", new[] { "_c" }));

        Assert.Equal(2.0, Calculation.LcomMetrics.ValueOf(Calculation.LcomMetrics.Lcom4(), car));
    }

    [Fact]
    public void Lcom4_counts_each_method_as_its_own_component_when_no_method_accesses_a_field()
    {
        var car = new ClassInfo(
            "App.Car",
            "src/Car.cs",
            new[] { new MethodInfo("A"), new MethodInfo("B") },
            new[] { new FieldInfo("_a"), new FieldInfo("_b") });

        Assert.Equal(2.0, Calculation.LcomMetrics.ValueOf(Calculation.LcomMetrics.Lcom4(), car));
    }

    [Fact]
    public void Lcom96a_and_lcom96b_count_unused_fields_in_the_field_count()
    {
        var car = new ClassInfo(
            "App.Car",
            "src/Car.cs",
            new[]
            {
                new MethodInfo("A", new[] { "_a" }),
                new MethodInfo("B", new[] { "_a" }),
            },
            new[]
            {
                new FieldInfo("_a", new[] { "A", "B" }),
                new FieldInfo("_unused"),
            });

        Assert.Equal(1.0, Calculation.LcomMetrics.ValueOf(Calculation.LcomMetrics.Lcom96a(), car));
        Assert.Equal(0.5, Calculation.LcomMetrics.ValueOf(Calculation.LcomMetrics.Lcom96b(), car));
    }

    [Fact]
    public void Lcom4_is_zero_for_a_class_without_methods()
    {
        var car = new ClassInfo("App.Car", "src/Car.cs", Array.Empty<MethodInfo>(), Array.Empty<FieldInfo>());

        Assert.Equal(0.0, Calculation.LcomMetrics.ValueOf(Calculation.LcomMetrics.Lcom4(), car));
    }

    [Fact]
    public void Lcom96a_lcom3_lcom5_and_lcom_star_share_one_formula()
    {
        ClassInfo car = Class(
            ("A", new[] { "_a", "_b" }),
            ("B", new[] { "_a" }),
            ("C", new[] { "_a" }));

        double reference = Calculation.LcomMetrics.ValueOf(Calculation.LcomMetrics.Lcom96a(), car);

        Assert.Equal(reference, Calculation.LcomMetrics.ValueOf(Calculation.LcomMetrics.Lcom3(), car));
        Assert.Equal(reference, Calculation.LcomMetrics.ValueOf(Calculation.LcomMetrics.Lcom5(), car));
        Assert.Equal(reference, Calculation.LcomMetrics.ValueOf(Calculation.LcomMetrics.LcomStar(), car));
    }

    [Fact]
    public void Lcom96b_and_lcom2_share_one_formula()
    {
        ClassInfo car = Class(
            ("A", new[] { "_a", "_b" }),
            ("B", new[] { "_a" }),
            ("C", new[] { "_a" }));

        double reference = Calculation.LcomMetrics.ValueOf(Calculation.LcomMetrics.Lcom96b(), car);

        Assert.Equal(reference, Calculation.LcomMetrics.ValueOf(Calculation.LcomMetrics.Lcom2(), car));
    }

    [Fact]
    public void The_factories_build_class_level_metrics_with_matching_kinds()
    {
        Assert.Equal(MetricSubject.Class, Calculation.LcomMetrics.Lcom96a().Subject);
        Assert.Equal(LcomMetricKind.Lcom96a, Calculation.LcomMetrics.Lcom96a().Kind);
        Assert.Equal(LcomMetricKind.Lcom96b, Calculation.LcomMetrics.Lcom96b().Kind);
        Assert.Equal(LcomMetricKind.Lcom1, Calculation.LcomMetrics.Lcom1().Kind);
        Assert.Equal(LcomMetricKind.Lcom2, Calculation.LcomMetrics.Lcom2().Kind);
        Assert.Equal(LcomMetricKind.Lcom3, Calculation.LcomMetrics.Lcom3().Kind);
        Assert.Equal(LcomMetricKind.Lcom4, Calculation.LcomMetrics.Lcom4().Kind);
        Assert.Equal(LcomMetricKind.Lcom5, Calculation.LcomMetrics.Lcom5().Kind);
        Assert.Equal(LcomMetricKind.LcomStar, Calculation.LcomMetrics.LcomStar().Kind);
    }

    [Fact]
    public void ValueOf_rejects_a_null_metric()
    {
        Assert.Throws<ArgumentNullException>(() =>
            Calculation.LcomMetrics.ValueOf((LcomMetric)null!, Class(("A", new[] { "_a" }))));
    }

    [Fact]
    public void ValueOf_rejects_a_null_class_info()
    {
        Assert.Throws<ArgumentNullException>(() =>
            Calculation.LcomMetrics.ValueOf(Calculation.LcomMetrics.Lcom4(), (ClassInfo)null!));
    }

    private static ClassInfo Class(params (string Method, string[] Fields)[] methods)
    {
        MethodInfo[] methodInfos = methods
            .Select(static m => new MethodInfo(m.Method, m.Fields))
            .ToArray();
        FieldInfo[] fieldInfos = methods
            .SelectMany(static m => m.Fields)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static name => name, StringComparer.Ordinal)
            .Select(field => new FieldInfo(
                field,
                methodInfos
                    .Where(method => method.AccessedFields.Contains(field, StringComparer.Ordinal))
                    .Select(static method => method.Name)
                    .ToArray()))
            .ToArray();
        return new ClassInfo("App.Car", "src/Car.cs", methodInfos, fieldInfos);
    }
}
