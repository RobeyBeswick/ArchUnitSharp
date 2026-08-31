namespace ArchUnitSharp.Common.Extraction;

/// <summary>
/// A <see cref="Pattern"/> bound to a <see cref="MatchTarget"/>. The target is a property of the
/// filter, never a choice at the call site, so matching is one generic function:
/// <see cref="Matches"/> picks the part of the identifier the target names and runs the pattern
/// against that part. A filter may also carry exclusions: further filters that, when they match the
/// same identifier, veto the parent's match — "everything under <c>app/</c>, but not the generated
/// folder" is one filter, not an inverted rule.
/// </summary>
/// <remarks>
/// <para>
/// Given an identifier, <see cref="Matches"/> first normalises its separators, then extracts the
/// target's substring — the file name for <see cref="MatchTarget.Filename"/>, the whole identifier
/// for <see cref="MatchTarget.Path"/>, the identifier without its file name for
/// <see cref="MatchTarget.PathWithoutFilename"/>, and the derived class name for
/// <see cref="MatchTarget.Classname"/> — and finally matches the pattern against that substring.
/// Because the target is bound here, a filter like <c>new Filter(new Pattern("*.cs"),
/// MatchTarget.Filename)</c> and one like <c>new Filter(new Pattern("*.cs"), MatchTarget.Path)</c>
/// behave differently on the same identifier.
/// </para>
/// <para>
/// An exclusion is itself a filter and is evaluated against the same identifier, so it may name any
/// target: an exclusion that shares its parent's target narrows it (a folder filter excluded by a
/// folder glob), and an explicitly targeted exclusion names a different part of the identifier (a
/// folder filter excluded by a file name). When any exclusion matches, the parent does not — the
/// exclusion is a veto, not a second match. The fluent surface's <c>except</c> companion builds
/// these; <see cref="WithExclusion"/> adds one to an existing filter.
/// </para>
/// <para>
/// This type is immutable and safe for concurrent use: its properties never change after
/// construction, and matching is stateless. Two filters are equal when their patterns, targets and
/// exclusions are equal.
/// </para>
/// </remarks>
public sealed record Filter
{
    private readonly Filter[] _exclusions;

    /// <summary>
    /// The pattern this filter matches with.
    /// </summary>
    public Pattern Pattern { get; }

    /// <summary>
    /// The part of an identifier the pattern is matched against.
    /// </summary>
    public MatchTarget Target { get; }

    /// <summary>
    /// The exclusions that veto this filter's match: an identifier an exclusion matches is not
    /// matched by this filter, whatever its own pattern and target say. Empty when no <c>except</c>
    /// companion has been applied. Each access returns a fresh copy, so the returned list is always
    /// safe to hold or mutate.
    /// </summary>
    public IReadOnlyList<Filter> Exclusions => (Filter[])_exclusions.Clone();

    /// <summary>
    /// Creates a filter from a pattern and a match target, with no exclusions.
    /// </summary>
    /// <param name="pattern">The pattern to match with. Must not be <see langword="null"/>.</param>
    /// <param name="target">The part of an identifier the pattern is matched against.</param>
    /// <exception cref="ArgumentNullException"><paramref name="pattern"/> is <see langword="null"/>.</exception>
    public Filter(Pattern pattern, MatchTarget target)
        : this(pattern, target, Array.Empty<Filter>())
    {
    }

    /// <summary>
    /// Creates a filter from a pattern, a match target and a set of exclusions. The exclusions are
    /// copied on construction, so later mutation of the supplied list does not affect this filter.
    /// </summary>
    /// <param name="pattern">The pattern to match with. Must not be <see langword="null"/>.</param>
    /// <param name="target">The part of an identifier the pattern is matched against.</param>
    /// <param name="exclusions">The exclusions that veto the filter's match. Must not be <see langword="null"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="pattern"/> or <paramref name="exclusions"/> is <see langword="null"/>.</exception>
    public Filter(Pattern pattern, MatchTarget target, IReadOnlyList<Filter> exclusions)
    {
        ArgumentNullException.ThrowIfNull(pattern);
        ArgumentNullException.ThrowIfNull(exclusions);
        Pattern = pattern;
        Target = target;
        _exclusions = exclusions.ToArray();
    }

    /// <summary>
    /// Returns <see langword="true"/> when the target part of <paramref name="identifier"/> is
    /// matched by this filter's pattern and by none of its exclusions. Each exclusion is itself
    /// matched against the same identifier, so it may name any target.
    /// </summary>
    /// <param name="identifier">The graph identifier to match against. Must not be <see langword="null"/>.</param>
    /// <returns><see langword="true"/> when the pattern matches the target part of the identifier and no exclusion does.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="identifier"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><see cref="Target"/> is not a defined <see cref="MatchTarget"/> value.</exception>
    public bool Matches(string identifier)
    {
        ArgumentNullException.ThrowIfNull(identifier);

        string normalised = identifier.Replace('\\', '/');
        string candidate = Target switch
        {
            MatchTarget.Path => normalised,
            MatchTarget.Filename => FilenameOf(normalised),
            MatchTarget.PathWithoutFilename => PathWithoutFilenameOf(normalised),
            MatchTarget.Classname => ClassnameOf(normalised),
            _ => throw new ArgumentOutOfRangeException(nameof(Target), Target, "Target is not a defined MatchTarget value."),
        };

        if (!Pattern.Matches(candidate))
        {
            return false;
        }

        foreach (Filter exclusion in _exclusions)
        {
            if (exclusion.Matches(identifier))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Returns a new filter that matches exactly what this filter matches, except also excluded by
    /// <paramref name="exclusion"/>. This filter is unchanged.
    /// </summary>
    /// <param name="exclusion">The exclusion to add. Must not be <see langword="null"/>.</param>
    /// <returns>A new filter with the exclusion added.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="exclusion"/> is <see langword="null"/>.</exception>
    public Filter WithExclusion(Filter exclusion)
    {
        ArgumentNullException.ThrowIfNull(exclusion);

        var exclusions = new Filter[_exclusions.Length + 1];
        Array.Copy(_exclusions, exclusions, _exclusions.Length);
        exclusions[_exclusions.Length] = exclusion;
        return new Filter(Pattern, Target, exclusions);
    }

    /// <summary>
    /// Two filters are equal when their patterns, targets and exclusions are equal.
    /// </summary>
    /// <param name="other">The other filter.</param>
    /// <returns><see langword="true"/> when the patterns, targets and exclusions are equal.</returns>
    public bool Equals(Filter? other) =>
        other is not null
            && Target == other.Target
            && Pattern == other.Pattern
            && _exclusions.SequenceEqual(other._exclusions);

    /// <summary>
    /// A hash code consistent with <see cref="Equals(Filter?)"/>.
    /// </summary>
    /// <returns>A hash code over the pattern, target and exclusions.</returns>
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Pattern);
        hash.Add(Target);
        foreach (Filter exclusion in _exclusions)
        {
            hash.Add(exclusion);
        }

        return hash.ToHashCode();
    }

    private static string FilenameOf(string path)
    {
        int separator = path.LastIndexOf('/');
        return separator < 0 ? path : path.Substring(separator + 1);
    }

    private static string PathWithoutFilenameOf(string path)
    {
        int separator = path.LastIndexOf('/');
        return separator < 0 ? string.Empty : path.Substring(0, separator);
    }

    private static string ClassnameOf(string path) => StripExtension(path).Replace('/', '.');

    private static string StripExtension(string path)
    {
        int separator = path.LastIndexOf('/');
        int dot = path.LastIndexOf('.');
        return dot < 0 || dot < separator ? path : path.Substring(0, dot);
    }
}
