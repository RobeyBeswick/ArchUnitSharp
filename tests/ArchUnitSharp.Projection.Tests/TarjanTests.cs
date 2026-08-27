using ArchUnitSharp.Common.Extraction;
using ArchUnitSharp.Projection;

namespace ArchUnitSharp.Projection.Tests;

public class TarjanTests
{
    [Fact]
    public void Empty_edges_yield_no_components()
    {
        IReadOnlyList<IReadOnlyList<string>> components =
            Tarjan.FindStronglyConnectedComponents(Array.Empty<ProjectedEdge>());

        Assert.Empty(components);
    }

    [Fact]
    public void Single_node_without_edges_yields_one_singleton_component()
    {
        IReadOnlyList<IReadOnlyList<string>> components =
            Tarjan.FindStronglyConnectedComponents(Edges(("A", "B")));

        Assert.Equal(new[] { "A" }, components[0]);
        Assert.Equal(new[] { "B" }, components[1]);
        Assert.Equal(2, components.Count);
    }

    [Fact]
    public void Acyclic_chain_yields_a_singleton_per_node()
    {
        IReadOnlyList<IReadOnlyList<string>> components =
            Tarjan.FindStronglyConnectedComponents(Edges(("A", "B"), ("B", "C")));

        Assert.Equal(new[] { "A" }, components[0]);
        Assert.Equal(new[] { "B" }, components[1]);
        Assert.Equal(new[] { "C" }, components[2]);
    }

    [Fact]
    public void Two_node_cycle_yields_one_component_of_both()
    {
        IReadOnlyList<IReadOnlyList<string>> components =
            Tarjan.FindStronglyConnectedComponents(Edges(("A", "B"), ("B", "A")));

        Assert.Equal(new[] { "A", "B" }, components.Single());
    }

    [Fact]
    public void Three_node_cycle_yields_one_component_of_all_three()
    {
        IReadOnlyList<IReadOnlyList<string>> components =
            Tarjan.FindStronglyConnectedComponents(Edges(("A", "B"), ("B", "C"), ("C", "A")));

        Assert.Equal(new[] { "A", "B", "C" }, components.Single());
    }

    [Fact]
    public void Disjoint_cycles_yield_one_component_each()
    {
        IReadOnlyList<IReadOnlyList<string>> components =
            Tarjan.FindStronglyConnectedComponents(Edges(("A", "B"), ("B", "A"), ("C", "D"), ("D", "C")));

        Assert.Equal(2, components.Count);
        Assert.Contains(components, static c => c.SequenceEqual(new[] { "A", "B" }));
        Assert.Contains(components, static c => c.SequenceEqual(new[] { "C", "D" }));
    }

    [Fact]
    public void Diamond_without_back_edges_yields_only_singletons()
    {
        IReadOnlyList<IReadOnlyList<string>> components =
            Tarjan.FindStronglyConnectedComponents(Edges(("A", "B"), ("A", "C"), ("B", "D"), ("C", "D")));

        Assert.Equal(4, components.Count);
        Assert.All(components, static c => Assert.Single(c));
    }

    [Fact]
    public void Self_loop_is_a_singleton_component()
    {
        IReadOnlyList<IReadOnlyList<string>> components =
            Tarjan.FindStronglyConnectedComponents(Edges(("A", "A")));

        Assert.Equal(new[] { "A" }, components.Single());
    }

    [Fact]
    public void Components_are_sorted_for_reproducible_output()
    {
        IReadOnlyList<IReadOnlyList<string>> components =
            Tarjan.FindStronglyConnectedComponents(Edges(("C", "D"), ("D", "C"), ("A", "B"), ("B", "A")));

        Assert.Equal(new[] { "A", "B" }, components[0]);
        Assert.Equal(new[] { "C", "D" }, components[1]);
    }

    [Fact]
    public void Nodes_within_a_component_are_sorted()
    {
        IReadOnlyList<IReadOnlyList<string>> components =
            Tarjan.FindStronglyConnectedComponents(Edges(("D", "A"), ("A", "B"), ("B", "D")));

        Assert.Equal(new[] { "A", "B", "D" }, components.Single());
    }

    [Fact]
    public void Null_edges_are_rejected()
    {
        Assert.Throws<ArgumentNullException>(() =>
            Tarjan.FindStronglyConnectedComponents(null!));
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
