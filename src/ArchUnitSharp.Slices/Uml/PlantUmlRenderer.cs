namespace ArchUnitSharp.Slices.Uml;

using ArchUnitSharp.Common.Extraction;
using ArchUnitSharp.Projection;

/// <summary>
/// Renders a projected slice graph as the supported PlantUML component-diagram subset: a
/// <c>@startuml</c>/<c>@enduml</c> document declaring one <c>component [Name]</c> per component and one
/// <c>[source] --&gt; [target]</c> arrow per dependency. This is the reverse direction of
/// <see cref="PlantUmlParser"/>: a diagram an architect can open, generated from the actual graph.
/// </summary>
/// <remarks>
/// <para>
/// Components are the supplied names plus every edge's endpoints, deduplicated and sorted ordinally;
/// edges are sorted by source then target, and every arrow is written in the two-dash form, so the
/// output is stable and reproducible. A component name is embedded between square brackets as-is — the
/// subset does not escape names — so a name that is empty or contains a closing bracket or a newline
/// is a <see cref="UserError"/> rather than malformed output.
/// </para>
/// <para>
/// This type is stateless and safe for concurrent use. The string it returns is freshly built on every
/// call.
/// </para>
/// </remarks>
internal static class PlantUmlRenderer
{
    /// <summary>
    /// Renders the projected dependencies as a PlantUML component diagram.
    /// </summary>
    /// <param name="edges">The projected slice dependencies to render. Must not be <see langword="null"/>.</param>
    /// <param name="components">The components to declare; every edge endpoint is declared even when absent here. Must not be <see langword="null"/>.</param>
    /// <returns>The diagram source, one statement per line.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="edges"/> or <paramref name="components"/> is <see langword="null"/>.</exception>
    /// <exception cref="UserError">A component or edge-endpoint name is empty or cannot be embedded in square brackets.</exception>
    public static string Render(IReadOnlyList<ProjectedEdge> edges, IReadOnlyList<string> components)
    {
        ArgumentNullException.ThrowIfNull(edges);
        ArgumentNullException.ThrowIfNull(components);

        string[] names = components
            .Concat(edges.SelectMany(static edge => new[] { edge.Source, edge.Target }))
            .Select(RequireName)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static name => name, StringComparer.Ordinal)
            .ToArray();

        var lines = new List<string> { "@startuml" };
        lines.AddRange(names.Select(static name => $"  component [{name}]"));

        foreach (ProjectedEdge edge in edges.OrderBy(static edge => edge.Source, StringComparer.Ordinal)
            .ThenBy(static edge => edge.Target, StringComparer.Ordinal))
        {
            lines.Add($"  [{RequireName(edge.Source)}] --> [{RequireName(edge.Target)}]");
        }

        lines.Add("@enduml");
        return string.Join('\n', lines) + "\n";
    }

    /// <summary>
    /// Validates a name as a component name: non-empty after trimming, and free of characters that
    /// would break the bracketed form.
    /// </summary>
    private static string RequireName(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        string trimmed = name.Trim();
        if (trimmed.Length == 0 || trimmed.Contains(']') || trimmed.Contains('\r') || trimmed.Contains('\n'))
        {
            throw new UserError($"'{name}' is not a valid PlantUML component name.");
        }

        return trimmed;
    }
}
