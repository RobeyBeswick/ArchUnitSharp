using ArchUnitSharp.Common.Extraction;
using ArchUnitSharp.Projection;

namespace ArchUnitSharp.Projection.Tests;

public class JohnsonTests
{
    [Fact]
    public void Empty_edges_yield_no_cycles()
    {
        IReadOnlyList<IReadOnlyList<string>> cycles =
            Johnson.FindElementaryCycles(Array.Empty<ProjectedEdge>());

        Assert.Empty(cycles);
    }

    [Fact]
    public void Acyclic_graph_yields_no_cycles()
    {
        IReadOnlyList<IReadOnlyList<string>> cycles =
            Johnson.FindElementaryCycles(Edges(("A", "B"), ("B", "C"), ("A", "C")));

        Assert.Empty(cycles);
    }

    [Fact]
    public void Two_node_cycle_is_reported()
    {
        IReadOnlyList<IReadOnlyList<string>> cycles =
            Johnson.FindElementaryCycles(Edges(("A", "B"), ("B", "A")));

        Assert.Equal(new[] { new[] { "A", "B" } }, cycles);
    }

    [Fact]
    public void Three_node_cycle_is_reported_once_starting_at_the_smallest_node()
    {
        IReadOnlyList<IReadOnlyList<string>> cycles =
            Johnson.FindElementaryCycles(Edges(("B", "C"), ("C", "A"), ("A", "B")));

        Assert.Equal(new[] { new[] { "A", "B", "C" } }, cycles);
    }

    [Fact]
    public void Disjoint_cycles_are_both_reported()
    {
        IReadOnlyList<IReadOnlyList<string>> cycles =
            Johnson.FindElementaryCycles(Edges(("A", "B"), ("B", "A"), ("C", "D"), ("D", "C")));

        Assert.Equal(new[] { new[] { "A", "B" }, new[] { "C", "D" } }, cycles);
    }

    [Fact]
    public void Figure_eight_sharing_a_node_reports_both_loops()
    {
        IReadOnlyList<IReadOnlyList<string>> cycles =
            Johnson.FindElementaryCycles(Edges(("A", "B"), ("B", "A"), ("A", "C"), ("C", "A")));

        Assert.Equal(new[] { new[] { "A", "B" }, new[] { "A", "C" } }, cycles);
    }

    [Fact]
    public void Complete_three_node_digraph_yields_five_cycles()
    {
        IReadOnlyList<IReadOnlyList<string>> cycles = Johnson.FindElementaryCycles(Edges(
            ("A", "B"), ("A", "C"), ("B", "A"), ("B", "C"), ("C", "A"), ("C", "B")));

        Assert.Equal(
            new[]
            {
                new[] { "A", "B" },
                new[] { "A", "C" },
                new[] { "B", "C" },
                new[] { "A", "B", "C" },
                new[] { "A", "C", "B" },
            },
            cycles);
    }

    [Fact]
    public void Self_loop_is_a_one_node_cycle()
    {
        IReadOnlyList<IReadOnlyList<string>> cycles =
            Johnson.FindElementaryCycles(Edges(("A", "A")));

        Assert.Equal(new[] { new[] { "A" } }, cycles);
    }

    [Fact]
    public void Cycles_are_sorted_by_length_then_contents()
    {
        IReadOnlyList<IReadOnlyList<string>> cycles = Johnson.FindElementaryCycles(Edges(
            ("A", "B"), ("B", "C"), ("C", "A"), ("B", "A")));

        Assert.Equal(new[] { new[] { "A", "B" }, new[] { "A", "B", "C" } }, cycles);
    }

    [Fact]
    public void Null_edges_are_rejected()
    {
        Assert.Throws<ArgumentNullException>(() =>
            Johnson.FindElementaryCycles(null!));
    }

    private static IReadOnlyList<ProjectedEdge> Edges(params (string Source, string Target)[] pairs) =>
        pairs
            .Select(static pair => new ProjectedEdge(
                pair.Source,
                pair.Target,
                external: false,
                ImportKind.Using,
                new[] { new Edge($"{pair.Source}.cs", $"{pair.Target}.cs", external: false, ImportKind.Using) }))
            .ToArray();
}
