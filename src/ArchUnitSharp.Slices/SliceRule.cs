namespace ArchUnitSharp.Slices;

using ArchUnitSharp.Common.Extraction;

/// <summary>
/// The internal data model of one slice rule: the subject is the whole slicing (every file a
/// definition assigns to a slice), and the rule asserts about dependencies from files matching
/// <see cref="From"/> to files matching <see cref="To"/>. It is produced by <c>should (not) contain
/// dependency(from, to)</c> and checked by <see cref="Assertion.SlicesAssertion"/>.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="From"/> and <see cref="To"/> are whole-path filters: a dependency from a sliced file
/// whose whole path matches <see cref="From"/> to a file whose whole path matches
/// <see cref="To"/> is a dependency the rule counts. <see cref="Negate"/> is <see langword="true"/>
/// for the <c>should not</c> mood — no slice may contain such a dependency, and each one found is a
/// violation — and <see langword="false"/> for the <c>should</c> mood — every slice must contain at
/// least one, and a slice without one is a violation.
/// </para>
/// <para>
/// This type is immutable and safe for concurrent use.
/// </para>
/// </remarks>
internal sealed class SliceRule
{
    /// <summary>
    /// The whole-path filter the importing file of a counted dependency must match.
    /// </summary>
    internal Filter From { get; }

    /// <summary>
    /// The whole-path filter the imported file of a counted dependency must match.
    /// </summary>
    internal Filter To { get; }

    /// <summary>
    /// <see langword="true"/> for the negated mood (<c>should not</c>), <see langword="false"/> for
    /// the positive mood (<c>should</c>).
    /// </summary>
    internal bool Negate { get; }

    /// <summary>
    /// Creates a slice rule. The globs are compiled to whole-path filters by
    /// <see cref="Pattern"/>, which rejects <see langword="null"/> and empty globs.
    /// </summary>
    /// <param name="fromGlob">The glob the importing file must match. Must not be <see langword="null"/> or empty.</param>
    /// <param name="toGlob">The glob the imported file must match. Must not be <see langword="null"/> or empty.</param>
    /// <param name="negate"><see langword="true"/> for the negated mood, <see langword="false"/> for the positive mood.</param>
    /// <exception cref="ArgumentNullException"><paramref name="fromGlob"/> or <paramref name="toGlob"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="fromGlob"/> or <paramref name="toGlob"/> is empty.</exception>
    internal SliceRule(string fromGlob, string toGlob, bool negate)
    {
        From = new Filter(new Pattern(fromGlob), MatchTarget.Path);
        To = new Filter(new Pattern(toGlob), MatchTarget.Path);
        Negate = negate;
    }
}
