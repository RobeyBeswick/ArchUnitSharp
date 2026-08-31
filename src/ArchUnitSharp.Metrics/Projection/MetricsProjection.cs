namespace ArchUnitSharp.Metrics.Projection;

using ArchUnitSharp.Common.Extraction;
using ArchUnitSharp.Metrics;

/// <summary>
/// The metrics module's pure projection logic: which files of a <see cref="Graph"/> a scope's file
/// selectors name, and — once the selected files are extracted — which of them and of their classes a
/// rule's <c>for classes matching</c> selector leaves in scope. File selectors combine with AND, and
/// the empty selector list selects everything, exactly as the files module's scope behaves.
/// </summary>
/// <remarks>
/// <para>
/// The files of a graph are its nodes, which the self-edge every file carries makes visible: a file
/// appears as the <see cref="Edge.Source"/> of its own self-edge, so the node set is exactly the set
/// of distinct edge sources. An external target is never a source, so it never appears as a file.
/// Results are sorted ordinally so reports are stable and reproducible.
/// </para>
/// <para>
/// The class selector narrows by class: <see cref="SelectClasses"/> keeps the classes of the selected
/// files whose fully qualified name matches every class filter, and <see cref="SelectFileSubjects"/>
/// keeps the files that contain at least one such class. With no class filter both keep everything —
/// a file-level rule over an unscoped <c>metrics</c> measures every selected file, and a class-level
/// rule measures every class of them.
/// </para>
/// <para>
/// This type is stateless and safe for concurrent use. The lists it returns are fresh copies on every
/// call.
/// </para>
/// </remarks>
internal static class MetricsProjection
{
    /// <summary>
    /// Returns the identifiers of the files every file filter selects, sorted ordinally.
    /// </summary>
    /// <param name="graph">The project's dependency graph. Must not be <see langword="null"/>.</param>
    /// <param name="filters">The scope's file selectors. Must not be <see langword="null"/>.</param>
    /// <returns>The selected files' identifiers, sorted.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="graph"/> or <paramref name="filters"/> is <see langword="null"/>.</exception>
    public static IReadOnlyList<string> SelectFiles(Graph graph, IReadOnlyList<Filter> filters)
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(filters);

        return graph.Edges
            .Select(static edge => edge.Source)
            .Distinct(StringComparer.Ordinal)
            .Where(identifier => filters.All(filter => filter.Matches(identifier)))
            .OrderBy(static identifier => identifier, StringComparer.Ordinal)
            .ToArray();
    }

    /// <summary>
    /// Returns the files of a rule's file-level subjects: every extracted file that contains at least
    /// one class whose fully qualified name matches every class filter, or every file when there are no
    /// class filters. The file's own counts are unchanged by the class selector — a file that survives
    /// it is measured whole. Sorted by path.
    /// </summary>
    /// <param name="files">The selected files, extracted. Must not be <see langword="null"/>.</param>
    /// <param name="classFilters">The scope's class selectors. Must not be <see langword="null"/>.</param>
    /// <returns>The files in scope for a file-level metric, sorted by path.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="files"/> or <paramref name="classFilters"/> is <see langword="null"/>.</exception>
    public static IReadOnlyList<FileInfo> SelectFileSubjects(
        IReadOnlyList<FileInfo> files,
        IReadOnlyList<Filter> classFilters)
    {
        ArgumentNullException.ThrowIfNull(files);
        ArgumentNullException.ThrowIfNull(classFilters);

        if (classFilters.Count == 0)
        {
            return files.ToArray();
        }

        return files
            .Where(file => file.ClassInfos.Any(
                classInfo => classFilters.All(filter => filter.Matches(classInfo.Name))))
            .OrderBy(static file => file.Path, StringComparer.Ordinal)
            .ToArray();
    }

    /// <summary>
    /// Returns the classes of a rule's class-level subjects: every class of the selected files whose
    /// fully qualified name matches every class filter, or every class when there are no class filters.
    /// Sorted by identifier, so reports are stable.
    /// </summary>
    /// <param name="files">The selected files, extracted. Must not be <see langword="null"/>.</param>
    /// <param name="classFilters">The scope's class selectors. Must not be <see langword="null"/>.</param>
    /// <returns>The classes in scope for a class-level metric, sorted by identifier.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="files"/> or <paramref name="classFilters"/> is <see langword="null"/>.</exception>
    public static IReadOnlyList<ClassInfo> SelectClasses(
        IReadOnlyList<FileInfo> files,
        IReadOnlyList<Filter> classFilters)
    {
        ArgumentNullException.ThrowIfNull(files);
        ArgumentNullException.ThrowIfNull(classFilters);

        return files
            .SelectMany(static file => file.ClassInfos)
            .Where(classInfo =>
                classFilters.Count == 0 || classFilters.All(filter => filter.Matches(classInfo.Name)))
            .OrderBy(static classInfo => classInfo.Identifier, StringComparer.Ordinal)
            .ToArray();
    }
}
