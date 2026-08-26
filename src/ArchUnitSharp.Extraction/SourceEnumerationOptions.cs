namespace ArchUnitSharp.Extraction;

/// <summary>
/// The options bag passed to <see cref="SourceEnumerator.Enumerate"/>: which directory names the
/// walk excludes. A <see langword="null"/> bag at the call site means
/// <see cref="DefaultExcludedDirectories"/>.
/// </summary>
/// <remarks>
/// <para>
/// The default exclusion set covers the categories a source walk must not descend into: build output
/// (<c>bin</c>, <c>obj</c>, <c>TestResults</c>), vendored dependencies (<c>node_modules</c>,
/// <c>packages</c>, <c>vendor</c>), version-control directories (<c>.git</c>, <c>.svn</c>, <c>.hg</c>)
/// and IDE caches (<c>.vs</c>, <c>.idea</c>). Exclusion is by directory name at any depth, matched
/// case-insensitively so <c>BIN</c> and <c>bin</c> are both skipped on every operating system.
/// </para>
/// <para>
/// This type is immutable: the list supplied to the constructor is copied in, and every read returns
/// a fresh copy, so neither the caller nor a consumer can corrupt the instance through a reference
/// they obtained from it. Sharing one instance across concurrent enumerations is safe.
/// </para>
/// </remarks>
public sealed class SourceEnumerationOptions
{
    private static readonly string[] _defaultExcludedDirectories =
    {
        "bin",
        "obj",
        "TestResults",
        ".git",
        ".svn",
        ".hg",
        ".vs",
        ".idea",
        "node_modules",
        "packages",
        "vendor",
    };

    private readonly string[] _excludedDirectories;

    /// <summary>
    /// Creates options with the given excluded directory names, or with the defaults when
    /// <paramref name="excludedDirectories"/> is <see langword="null"/>.
    /// </summary>
    /// <param name="excludedDirectories">The directory names to exclude from enumeration; <see langword="null"/> means <see cref="DefaultExcludedDirectories"/>.</param>
    public SourceEnumerationOptions(IEnumerable<string>? excludedDirectories = null)
    {
        _excludedDirectories = (excludedDirectories ?? _defaultExcludedDirectories).ToArray();
    }

    /// <summary>
    /// The default exclusion set. Each access returns a fresh copy, so mutating the returned list
    /// cannot affect the defaults.
    /// </summary>
    public static IReadOnlyList<string> DefaultExcludedDirectories => _defaultExcludedDirectories.ToArray();

    /// <summary>
    /// The directory names this enumeration excludes. Each access returns a fresh copy, so mutating
    /// the returned list cannot affect this instance.
    /// </summary>
    public IReadOnlyList<string> ExcludedDirectories => _excludedDirectories.ToArray();
}
