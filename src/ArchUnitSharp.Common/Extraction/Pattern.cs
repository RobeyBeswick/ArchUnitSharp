namespace ArchUnitSharp.Common.Extraction;

using System.Text.RegularExpressions;

/// <summary>
/// An immutable, value-semantic glob pattern. The glob is compiled to a <see cref="Regex"/> exactly
/// once, at construction, by <see cref="RegexFactory.CompileGlob"/>; this type is the boundary after
/// which nothing downstream ever sees a glob.
/// </summary>
/// <remarks>
/// <para>
/// Wildcards are the glob vocabulary: <c>*</c> within a segment, <c>**</c> across segments, <c>?</c>
/// one character, <c>[...]</c> a character class. Matching is case-sensitive. The candidate string
/// passed to <see cref="Matches"/> must already be normalised to forward-slash separators; use a
/// <see cref="Filter"/> when the candidate arrives as an unnormalised graph identifier.
/// </para>
/// <para>
/// This type is immutable and safe for concurrent use: once constructed, its state never changes, and
/// the compiled <see cref="Regex"/> it matches with is itself safe to share across threads. Two
/// patterns are equal when their globs are equal, so a pattern can be freely shared between filters.
/// </para>
/// </remarks>
public sealed record Pattern
{
    private readonly Regex _regex;

    /// <summary>
    /// The glob this pattern was created from, exactly as supplied. It is not itself normalised; the
    /// compilation in <see cref="RegexFactory.CompileGlob"/> is where separators are normalised.
    /// </summary>
    public string Glob { get; }

    /// <summary>
    /// Creates a pattern from a glob.
    /// </summary>
    /// <param name="glob">The glob to compile. Must not be <see langword="null"/> or empty.</param>
    /// <exception cref="ArgumentNullException"><paramref name="glob"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="glob"/> is empty.</exception>
    public Pattern(string glob)
    {
        ArgumentNullException.ThrowIfNull(glob);
        if (glob.Length == 0)
        {
            throw new ArgumentException("Pattern must not be empty.", nameof(glob));
        }

        Glob = glob;
        _regex = RegexFactory.CompileGlob(glob);
    }

    /// <summary>
    /// Returns <see langword="true"/> when the whole of <paramref name="input"/> is matched by this
    /// pattern.
    /// </summary>
    /// <param name="input">The candidate to match, already normalised to forward-slash separators. Must not be <see langword="null"/>.</param>
    /// <returns><see langword="true"/> when the pattern matches the whole candidate.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="input"/> is <see langword="null"/>.</exception>
    public bool Matches(string input)
    {
        ArgumentNullException.ThrowIfNull(input);
        return _regex.IsMatch(input);
    }

    /// <summary>
    /// Two patterns are equal when their globs are equal.
    /// </summary>
    /// <param name="other">The other pattern.</param>
    /// <returns><see langword="true"/> when the globs are equal.</returns>
    public bool Equals(Pattern? other) =>
        other is not null && string.Equals(Glob, other.Glob, StringComparison.Ordinal);

    /// <summary>
    /// The hash code of the glob.
    /// </summary>
    /// <returns>A hash code consistent with <see cref="Equals(Pattern?)"/>.</returns>
    public override int GetHashCode() => Glob.GetHashCode();
}
