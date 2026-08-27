namespace ArchUnitSharp.Extraction;

using System.Collections.Concurrent;
using ArchUnitSharp.Common.Extraction;

/// <summary>
/// Memoises the extraction pipeline so a test suite that runs dozens of rules over one project reads
/// and parses its source files once. The pipeline — enumerate, filter by the analysis toggles, read
/// and resolve, normalise — runs once per distinct input and its <see cref="Graph"/> is reused for
/// every later call with the same input.
/// </summary>
/// <remarks>
/// <para>
/// The cache key captures every input that can change the extracted graph: the project
/// <see cref="ProjectLocation"/> (what <see cref="ProjectLocator.Locate()"/> produces), the excluded
/// directories of the <see cref="SourceEnumerationOptions"/>, and the analysis toggles
/// <see cref="CheckOptions.IgnoreTestCode"/> and <see cref="CheckOptions.IgnoreGeneratedCode"/>.
/// The key is built by a single function so a new input cannot silently be left out of it.
/// </para>
/// <para>
/// Two escape hatches exist. A call that passes <see cref="CheckOptions.ClearCache"/> set to
/// <see langword="true"/> bypasses the cache: the graph is rebuilt from source and the fresh result
/// replaces the entry, so the next call with the same input reuses it. <see cref="Clear"/> empties
/// the whole cache — the global "clear graph cache" — for a consumer that knows the sources changed
/// out of band.
/// </para>
/// <para>
/// The cache is global to the process and thread-safe: <see cref="Get"/> and <see cref="Clear"/> may
/// be called concurrently. Under contention two threads may both run the pipeline for the same key;
/// the results are identical and only one is kept, so the waste is recomputation, never corruption.
/// A pipeline that fails (a <see cref="TechnicalError"/>) is never cached, so a project that becomes
/// readable later is not blocked by an earlier failure.
/// </para>
/// <para>
/// The <see cref="Graph"/> returned is the same immutable instance on every hit, so callers may hold
/// it and compare by reference.
/// </para>
/// </remarks>
public static class GraphCache
{
    private static readonly ConcurrentDictionary<GraphCacheKey, Graph> _cache = new();

    /// <summary>
    /// Returns the project's dependency graph, extracting it if it is not cached. Identical inputs
    /// return the same immutable <see cref="Graph"/> instance while its cache entry is valid.
    /// </summary>
    /// <param name="location">The project to analyse, as produced by <see cref="ProjectLocator.Locate()"/>. Must not be <see langword="null"/>.</param>
    /// <param name="enumerationOptions">The enumeration exclusions; <see langword="null"/> means <see cref="SourceEnumerationOptions.DefaultExcludedDirectories"/>.</param>
    /// <param name="checkOptions">The analysis toggles and the cache bypass; <see langword="null"/> means the defaults in <see cref="CheckOptions"/>.</param>
    /// <returns>The project's graph.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="location"/> is <see langword="null"/>.</exception>
    /// <exception cref="TechnicalError">The project cannot be enumerated or read.</exception>
    public static Graph Get(
        ProjectLocation location,
        SourceEnumerationOptions? enumerationOptions = null,
        CheckOptions? checkOptions = null)
    {
        ArgumentNullException.ThrowIfNull(location);

        GraphCacheKey key = BuildKey(location, enumerationOptions, checkOptions);

        if (checkOptions?.ClearCache == true)
        {
            Graph fresh = Build(location, enumerationOptions, checkOptions);
            _cache[key] = fresh;
            return fresh;
        }

        return _cache.GetOrAdd(key, _ => Build(location, enumerationOptions, checkOptions));
    }

    /// <summary>
    /// Empties the graph cache so the next <see cref="Get"/> rebuilds every graph from source. The
    /// global escape hatch for a consumer that knows the project's sources changed out of band.
    /// </summary>
    public static void Clear() => _cache.Clear();

    /// <summary>
    /// Folds every input that can change the extracted graph into a cache key. The one place the key
    /// is built, so a new input cannot be forgotten.
    /// </summary>
    private static GraphCacheKey BuildKey(
        ProjectLocation location,
        SourceEnumerationOptions? enumerationOptions,
        CheckOptions? checkOptions) =>
        new(
            location,
            BuildExclusionsKey(enumerationOptions),
            checkOptions?.IgnoreTestCode ?? false,
            checkOptions?.IgnoreGeneratedCode ?? false);

    /// <summary>
    /// The excluded directory names as a value: behaviour-equivalent bags (same names, any case and
    /// order) fold to the same string, because exclusion is matched case-insensitively.
    /// </summary>
    private static string BuildExclusionsKey(SourceEnumerationOptions? enumerationOptions)
    {
        string[] names = (enumerationOptions?.ExcludedDirectories
                ?? SourceEnumerationOptions.DefaultExcludedDirectories)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(static name => name.ToLowerInvariant())
            .OrderBy(static name => name, StringComparer.Ordinal)
            .ToArray();

        return string.Join("\u0001", names);
    }

    private static Graph Build(
        ProjectLocation location,
        SourceEnumerationOptions? enumerationOptions,
        CheckOptions? checkOptions)
    {
        IReadOnlyList<SourceFile> sourceFiles = SourceFileFilter.Apply(
            SourceEnumerator.Enumerate(location, enumerationOptions),
            checkOptions?.IgnoreTestCode ?? false,
            checkOptions?.IgnoreGeneratedCode ?? false);

        return new Graph(ImportExtractor.Extract(sourceFiles));
    }

    private sealed record GraphCacheKey(
        ProjectLocation Location,
        string ExcludedDirectories,
        bool IgnoreTestCode,
        bool IgnoreGeneratedCode);
}
