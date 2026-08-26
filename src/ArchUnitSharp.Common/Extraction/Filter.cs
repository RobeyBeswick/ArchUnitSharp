namespace ArchUnitSharp.Common.Extraction;

/// <summary>
/// A <see cref="Pattern"/> bound to a <see cref="MatchTarget"/>. The target is a property of the
/// filter, never a choice at the call site, so matching is one generic function:
/// <see cref="Matches"/> picks the part of the identifier the target names and runs the pattern
/// against that part.
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
/// This type is immutable and safe for concurrent use: its two properties never change after
/// construction, and matching is stateless. Two filters are equal when their patterns and targets
/// are equal.
/// </para>
/// </remarks>
public sealed record Filter
{
    /// <summary>
    /// The pattern this filter matches with.
    /// </summary>
    public Pattern Pattern { get; }

    /// <summary>
    /// The part of an identifier the pattern is matched against.
    /// </summary>
    public MatchTarget Target { get; }

    /// <summary>
    /// Creates a filter from a pattern and a match target.
    /// </summary>
    /// <param name="pattern">The pattern to match with. Must not be <see langword="null"/>.</param>
    /// <param name="target">The part of an identifier the pattern is matched against.</param>
    /// <exception cref="ArgumentNullException"><paramref name="pattern"/> is <see langword="null"/>.</exception>
    public Filter(Pattern pattern, MatchTarget target)
    {
        ArgumentNullException.ThrowIfNull(pattern);
        Pattern = pattern;
        Target = target;
    }

    /// <summary>
    /// Returns <see langword="true"/> when the target part of <paramref name="identifier"/> is
    /// matched by this filter's pattern.
    /// </summary>
    /// <param name="identifier">The graph identifier to match against. Must not be <see langword="null"/>.</param>
    /// <returns><see langword="true"/> when the pattern matches the target part of the identifier.</returns>
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

        return Pattern.Matches(candidate);
    }

    /// <summary>
    /// Two filters are equal when their patterns and targets are equal.
    /// </summary>
    /// <param name="other">The other filter.</param>
    /// <returns><see langword="true"/> when the patterns and targets are equal.</returns>
    public bool Equals(Filter? other) =>
        other is not null && Target == other.Target && Pattern == other.Pattern;

    /// <summary>
    /// A hash code consistent with <see cref="Equals(Filter?)"/>.
    /// </summary>
    /// <returns>A hash code over the pattern and target.</returns>
    public override int GetHashCode() => HashCode.Combine(Pattern, Target);

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
