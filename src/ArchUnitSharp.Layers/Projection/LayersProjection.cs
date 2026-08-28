namespace ArchUnitSharp.Layers.Projection;

using ArchUnitSharp.Common.Extraction;

/// <summary>
/// The layers module's pure projection logic: which files of a <see cref="Graph"/> a declared
/// <see cref="Layer"/> selects, and which of the subject layer's dependencies reach another declared
/// layer. File filters combine the same way the files module's scope selectors do — a file is
/// selected when the layer's filter matches its identifier — and selection results are sorted
/// ordinally so reports are stable and reproducible.
/// </summary>
/// <remarks>
/// <para>
/// The files of a graph are its nodes, which the self-edge every file carries makes visible: a file
/// appears as the <see cref="Edge.Source"/> of its own self-edge, so the node set is exactly the set
/// of distinct edge sources. An external target is never a source, so it never appears as a file and
/// an external edge never becomes a cross-layer dependency.
/// </para>
/// <para>
/// A cross-layer dependency is an edge from a file of the subject layer to a file that belongs to at
/// least one other declared layer. Self-edges, external edges, edges from files outside the subject
/// layer and edges to files in no declared layer are all filtered out, and a target that belongs only
/// to the subject layer is an intra-layer dependency and is never reported. Results are sorted by
/// source then target so reports are reproducible, and the target layers of a dependency are listed
/// in declaration order, which keeps the assertion's choice of violation target deterministic.
/// </para>
/// <para>
/// This type is stateless and safe for concurrent use. The lists it returns are fresh copies on every
/// call.
/// </para>
/// </remarks>
internal static class LayersProjection
{
    /// <summary>
    /// Returns the identifiers of the files <paramref name="filter"/> selects from
    /// <paramref name="graph"/>, sorted ordinally.
    /// </summary>
    /// <param name="graph">The project's dependency graph. Must not be <see langword="null"/>.</param>
    /// <param name="filter">The layer's defining filter. Must not be <see langword="null"/>.</param>
    /// <returns>The selected files' identifiers, sorted.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="graph"/> or <paramref name="filter"/> is <see langword="null"/>.</exception>
    public static IReadOnlyList<string> FilesOf(Graph graph, Filter filter)
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(filter);

        return graph.Edges
            .Select(static edge => edge.Source)
            .Distinct(StringComparer.Ordinal)
            .Where(identifier => filter.Matches(identifier))
            .OrderBy(static identifier => identifier, StringComparer.Ordinal)
            .ToArray();
    }

    /// <summary>
    /// Returns the cross-layer dependencies of <paramref name="subject"/>: every edge from a file the
    /// subject layer selects to a file that belongs to at least one other declared layer, with the
    /// target's declared layers (excluding the subject) listed in declaration order. A self-edge, an
    /// external edge, an edge from a file outside the subject layer, an intra-layer target and a
    /// target in no declared layer are all filtered out. The result is sorted by source then target,
    /// so reports are reproducible.
    /// </summary>
    /// <param name="graph">The project's dependency graph. Must not be <see langword="null"/>.</param>
    /// <param name="subject">The rule's subject layer. Must not be <see langword="null"/>.</param>
    /// <param name="layers">Every declared layer, the subject included. Must not be <see langword="null"/>.</param>
    /// <returns>The subject layer's cross-layer dependencies, sorted by source then target.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="graph"/>, <paramref name="subject"/> or <paramref name="layers"/> is <see langword="null"/>.</exception>
    public static IReadOnlyList<CrossLayerDependency> CrossLayerDependencies(
        Graph graph,
        Layer subject,
        IReadOnlyList<Layer> layers)
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(subject);
        ArgumentNullException.ThrowIfNull(layers);

        var subjectFiles = new HashSet<string>(FilesOf(graph, subject.Filter), StringComparer.Ordinal);

        var layerFiles = new List<(string Name, HashSet<string> Files)>(layers.Count);
        foreach (Layer layer in layers)
        {
            layerFiles.Add((layer.Name, new HashSet<string>(FilesOf(graph, layer.Filter), StringComparer.Ordinal)));
        }

        var dependencies = new List<CrossLayerDependency>();
        foreach (Edge edge in graph.Edges)
        {
            if (edge.External || edge.Source == edge.Target || !subjectFiles.Contains(edge.Source))
            {
                continue;
            }

            string[] targetLayers = layerFiles
                .Where(entry =>
                    !string.Equals(entry.Name, subject.Name, StringComparison.Ordinal)
                    && entry.Files.Contains(edge.Target))
                .Select(static entry => entry.Name)
                .ToArray();
            if (targetLayers.Length == 0)
            {
                continue;
            }

            dependencies.Add(new CrossLayerDependency(edge.Source, edge.Target, targetLayers));
        }

        return dependencies
            .OrderBy(static dependency => dependency.Source, StringComparer.Ordinal)
            .ThenBy(static dependency => dependency.Target, StringComparer.Ordinal)
            .ToArray();
    }
}
