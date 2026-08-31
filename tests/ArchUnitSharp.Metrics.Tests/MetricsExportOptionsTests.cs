namespace ArchUnitSharp.Metrics.Tests;

public class MetricsExportOptionsTests
{
    [Fact]
    public void The_defaults_are_the_default_title_a_timestamp_and_no_custom_css()
    {
        var options = new MetricsExportOptions();

        Assert.Equal("ArchUnitSharp Metrics Report", options.Title);
        Assert.True(options.IncludeTimestamp);
        Assert.Null(options.CustomCss);
    }

    [Fact]
    public void A_title_must_not_be_null_or_empty()
    {
        Assert.Throws<ArgumentNullException>(() => new MetricsExportOptions { Title = null! });
        Assert.Throws<ArgumentException>(() => new MetricsExportOptions { Title = "" });
    }

    [Fact]
    public void A_custom_css_must_not_be_empty_when_given()
    {
        Assert.Throws<ArgumentException>(() => new MetricsExportOptions { CustomCss = "" });
    }

    [Fact]
    public void A_with_expression_validates_the_same_rules_and_leaves_the_original_unchanged()
    {
        var options = new MetricsExportOptions();
        MetricsExportOptions renamed = options with { Title = "Counts" };

        Assert.Equal("ArchUnitSharp Metrics Report", options.Title);
        Assert.Equal("Counts", renamed.Title);
        Assert.Throws<ArgumentException>(() => options with { CustomCss = "" });
    }

    [Fact]
    public void Two_options_with_the_same_values_are_equal()
    {
        Assert.Equal(
            new MetricsExportOptions { Title = "Counts", IncludeTimestamp = false, CustomCss = "body{}" },
            new MetricsExportOptions { Title = "Counts", IncludeTimestamp = false, CustomCss = "body{}" });
    }
}
