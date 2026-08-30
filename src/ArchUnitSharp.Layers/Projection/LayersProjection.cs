namespace ArchUnitSharp.Layers.Projection;

using ArchUnitSharp.Common.Extraction;

/// <summary>
/// The layers module's pure projection logic: which files a named layer contains, and which
/// cross-layer dependencies a graph's edges produce once its files are assigned to layers. A file
/// belongs to a layer when any of that layer's declarations' filters matches its identifier; a file
/// that matches none belongs to no layer.
/// </summary>
/// <remarks>
/// <para>
/// The files of a graph are its nodes, visible through the self-edge every file carries. A file is in
/// a layer when at least one declaration of that layer matches it, so a layer declared in two places
/// contains the union of both. A dependency edge whose importing or imported file belongs to no layer
/// is ignored, and a dependency between two files of the same layer is intra-layer — never reported as
/// a cross-layer dependency.
/// </para>
/// <para>
/// Cross-layer dependencies are produced per (source layer, target layer) pair per edge: a file that
/// belongs to two layers and depends on a file in a third yields one dependency per source layer.
/// Results are sorted — by source layer, target layer, source file, then target file — so reports are
/// stable and reproducible.
/// </para>
/// <para>
/// This type is stateless and safe for concurrent use. The lists it returns are fresh copies on every
/// call.
/// </para>
/// </remarks>
internal static class LayersProjection
{
    /// <summary>
    /// Returns the identifiers of the files that belong to <paramref name="layerName"/>: every file
    /// matching any declaration of that layer, sorted ordinally.
    /// </summary>
    /// <param name="graph">The project's dependency graph. Must not be <see langword="null"/>.</param>
    /// <param name="declarations">The layer declarations. Must not be <see langword="null"/>.</param>
    /// <param name="layerName">The layer to select. Must not be <see langword="null"/> or empty.</param>
    /// <returns>The layer's files, sorted.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="graph"/>, <paramref name="declarations"/> or <paramref name="layerName"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="layerName"/> is empty.</exception>
    public static IReadOnlyList<string> FilesOfLayer(
        Graph graph,
        IReadOnlyList<LayerDeclaration> declarations,
        string layerName)
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(declarations);
        _ = LayerDeclaration.RequireName(layerName);

        return DistinctFiles(graph)
            .Where(file => declarations.Any(d =>
                d.Name == layerName && d.Filter.Matches(file)))
            .OrderBy(static file => file, StringComparer.Ordinal)
            .ToArray();
    }

    /// <summary>
    /// Returns every cross-layer dependency in <paramref name="graph"/> under the given
    /// <paramref name="declarations"/>: one per dependency edge per (source layer, target layer) pair,
    /// excluding intra-layer dependencies and edges whose endpoint belongs to no layer. The result is
    /// sorted by source layer, target layer, source file, then target file.
    /// </summary>
    /// <param name="graph">The project's dependency graph. Must not be <see langword="null"/>.</param>
    /// <param name="declarations">The layer declarations. Must not be <see langword="null"/>.</param>
    /// <returns>The cross-layer dependencies, sorted.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="graph"/> or <paramref name="declarations"/> is <see langword="null"/>.</exception>
    public static IReadOnlyList<CrossLayerDependency> CrossLayerDependencies(
        Graph graph,
        IReadOnlyList<LayerDeclaration> declarations)
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(declarations);

        Dictionary<string, string[]> layersByFile = LayersByFile(graph, declarations);

        var result = new List<CrossLayerDependency>();
        foreach (Edge edge in graph.Edges)
        {
            if (edge.External || edge.Source == edge.Target)
            {
                continue;
            }

            if (!layersByFile.TryGetValue(edge.Source, out string[]? sourceLayers)
                || !layersByFile.TryGetValue(edge.Target, out string[]? targetLayers))
            {
                continue;
            }

            foreach (string sourceLayer in sourceLayers)
            {
                foreach (string targetLayer in targetLayers)
                {
                    if (sourceLayer == targetLayer)
                    {
                        continue;
                    }

                    result.Add(new CrossLayerDependency(sourceLayer, targetLayer, edge.Source, edge.Target));
                }
            }
        }

        return result
            .OrderBy(static dependency => dependency.SourceLayer, StringComparer.Ordinal)
            .ThenBy(static dependency => dependency.TargetLayer, StringComparer.Ordinal)
            .ThenBy(static dependency => dependency.Source, StringComparer.Ordinal)
            .ThenBy(static dependency => dependency.Target, StringComparer.Ordinal)
            .ToArray();
    }

    private static string[] DistinctFiles(Graph graph) =>
        graph.Edges
            .Select(static edge => edge.Source)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

    private static Dictionary<string, string[]> LayersByFile(
        Graph graph,
        IReadOnlyList<LayerDeclaration> declarations)
    {
        var map = new Dictionary<string, string[]>(StringComparer.Ordinal);

        foreach (string file in DistinctFiles(graph))
        {
            string[] names = declarations
                .Where(declaration => declaration.Filter.Matches(file))
                .Select(static declaration => declaration.Name)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(static name => name, StringComparer.Ordinal)
                .ToArray();

            if (names.Length > 0)
            {
                map[file] = names;
            }
        }

        return map;
    }
}
