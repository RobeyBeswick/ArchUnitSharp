namespace ArchUnitSharp.Slices.Projection;

/// <summary>
/// One dependency a <see cref="SlicesProjection"/> finds between a slice's <c>from</c> files and the
/// rule's <c>to</c> files: a dependency edge whose importing file belongs to a slice and matches the
/// rule's <c>from</c> filter, and whose imported file matches the rule's <c>to</c>
/// filter. The raw dependency is carried as the two concrete file identifiers so a violation can name
/// them; the slice name is the slice the dependency is contained in.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Slice"/> is the name of the slice the importing file belongs to — the slice that
/// "contains" the dependency — so the same dependency is never reported against two slices: each file
/// belongs to exactly one slice. <see cref="Source"/> and <see cref="Target"/> are the project-relative
/// identifiers of the two files.
/// </para>
/// <para>
/// This type is immutable and value-semantic: two dependencies with the same three values are equal.
/// </para>
/// </remarks>
internal sealed record SliceDependency
{
    private readonly string _slice;
    private readonly string _source;
    private readonly string _target;

    /// <summary>
    /// The slice that contains the dependency.
    /// </summary>
    internal string Slice
    {
        get => _slice;
        init => _slice = Require(value, nameof(Slice));
    }

    /// <summary>
    /// The importing file's project-relative identifier.
    /// </summary>
    internal string Source
    {
        get => _source;
        init => _source = Require(value, nameof(Source));
    }

    /// <summary>
    /// The imported file's project-relative identifier.
    /// </summary>
    internal string Target
    {
        get => _target;
        init => _target = Require(value, nameof(Target));
    }

    /// <summary>
    /// Creates a slice-contained dependency.
    /// </summary>
    /// <param name="slice">The slice that contains the dependency. Must not be <see langword="null"/> or empty.</param>
    /// <param name="source">The importing file's identifier. Must not be <see langword="null"/> or empty.</param>
    /// <param name="target">The imported file's identifier. Must not be <see langword="null"/> or empty.</param>
    /// <exception cref="ArgumentNullException"><paramref name="slice"/>, <paramref name="source"/> or <paramref name="target"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="slice"/>, <paramref name="source"/> or <paramref name="target"/> is empty.</exception>
    internal SliceDependency(string slice, string source, string target)
    {
        _slice = Require(slice, nameof(slice));
        _source = Require(source, nameof(source));
        _target = Require(target, nameof(target));
    }

    private static string Require(string value, string parameterName) =>
        value is null
            ? throw new ArgumentNullException(parameterName)
            : value.Length == 0
                ? throw new ArgumentException($"{parameterName} must not be empty.", parameterName)
                : value;
}
