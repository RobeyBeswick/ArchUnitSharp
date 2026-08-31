namespace ArchUnitSharp.Slices;

using System.Text.RegularExpressions;
using ArchUnitSharp.Common.Extraction;

/// <summary>
/// The internal data model of one slice definition: the instruction that assigns each file of a
/// <see cref="Graph"/> to a slice, or to no slice. A definition is one matcher with a capture group
/// whose captured text is the slice's name: a <c>defined by</c> glob whose <c>(**)</c> captures, or a
/// <c>defined by regex</c> pattern whose first group captures.
/// </summary>
/// <remarks>
/// <para>
/// A file is assigned to the slice named by the first definition that captures a non-empty name for
/// it; a file no definition captures a name for is not sliced. An empty capture is not a slice name,
/// so a file whose captured text is empty is treated as unsliced by the same rule that matched it,
/// and a trailing path separator is trimmed from a captured name, so a <c>(**)</c> capture between
/// separators names the segments alone.
/// </para>
/// <para>
/// <see cref="ByPattern"/> compiles its glob through the kernel's <see cref="RegexFactory"/>, the
/// library's one glob-to-regex boundary, and requires the glob to contain a <c>(**)</c> capture; a
/// capture-less glob cannot name a slice and is a <see cref="UserError"/>. <see cref="ByRegex"/>
/// anchors its pattern to the whole identifier and requires it to contain a capture group.
/// </para>
/// <para>
/// This type is immutable and safe for concurrent use: the compiled regex never changes, and matching
/// is stateless.
/// </para>
/// </remarks>
internal sealed class SliceDefinition
{
    private readonly Regex _regex;

    /// <summary>
    /// The definition as the word of a rule description, for a report: <c>defined by 'glob'</c> or
    /// <c>defined by regex 'pattern'</c>.
    /// </summary>
    internal string Description { get; }

    /// <summary>
    /// Creates a slice definition from a matcher and its description.
    /// </summary>
    /// <param name="description">The definition's words in a rule description. Must not be <see langword="null"/>.</param>
    /// <param name="regex">The compiled matcher whose first group names the slice. Must not be <see langword="null"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="description"/> or <paramref name="regex"/> is <see langword="null"/>.</exception>
    internal SliceDefinition(string description, Regex regex)
    {
        ArgumentNullException.ThrowIfNull(description);
        ArgumentNullException.ThrowIfNull(regex);
        Description = description;
        _regex = regex;
    }

    /// <summary>
    /// Creates a <c>defined by</c> definition from a glob with a <c>(**)</c> capture: a file whose
    /// whole path matches is assigned to the slice named by the captured text.
    /// </summary>
    /// <param name="glob">The glob to match each file's whole path against. Must not be <see langword="null"/> or empty, and must contain a <c>(**)</c> capture.</param>
    /// <returns>The definition.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="glob"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="glob"/> is empty.</exception>
    /// <exception cref="UserError"><paramref name="glob"/> contains no <c>(**)</c> capture, so it cannot name a slice.</exception>
    internal static SliceDefinition ByPattern(string glob)
    {
        ArgumentNullException.ThrowIfNull(glob);
        if (glob.Length == 0)
        {
            throw new ArgumentException("A slice pattern must not be empty.", nameof(glob));
        }

        Regex regex = RegexFactory.CompileGlob(glob);
        RequireCapture(regex, $"A defined-by glob must contain a (**) capture: '{glob}'.");
        return new SliceDefinition($"defined by '{glob}'", regex);
    }

    /// <summary>
    /// Creates a <c>defined by regex</c> definition from a regex pattern with a capture group: a file
    /// whose whole path matches is assigned to the slice named by the first group's captured text.
    /// The pattern is anchored, so it must match the whole identifier.
    /// </summary>
    /// <param name="pattern">The regex to match each file's whole path against. Must not be <see langword="null"/> or empty, and must contain a capture group.</param>
    /// <returns>The definition.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="pattern"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="pattern"/> is empty.</exception>
    /// <exception cref="UserError"><paramref name="pattern"/> contains no capture group, so it cannot name a slice.</exception>
    internal static SliceDefinition ByRegex(string pattern)
    {
        ArgumentNullException.ThrowIfNull(pattern);
        if (pattern.Length == 0)
        {
            throw new ArgumentException("A slice regex must not be empty.", nameof(pattern));
        }

        Regex regex = new("^(?:" + pattern + ")$", RegexOptions.CultureInvariant);
        RequireCapture(regex, $"A defined-by-regex pattern must contain a capture group: '{pattern}'.");
        return new SliceDefinition($"defined by regex '{pattern}'", regex);
    }

    /// <summary>
    /// Returns the name of the slice <paramref name="identifier"/> is assigned to by this definition,
    /// or <see langword="null"/> when the identifier does not match or its capture is empty. A
    /// trailing path separator in the captured text is trimmed, so a <c>(**)</c> capture between
    /// separators names the segments alone.
    /// </summary>
    /// <param name="identifier">The file's graph identifier. Must not be <see langword="null"/>.</param>
    /// <returns>The slice's name, or <see langword="null"/> when this definition does not slice the file.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="identifier"/> is <see langword="null"/>.</exception>
    internal string? SliceOf(string identifier)
    {
        ArgumentNullException.ThrowIfNull(identifier);

        Match match = _regex.Match(identifier);
        if (!match.Success)
        {
            return null;
        }

        string label = match.Groups[1].Value.TrimEnd('/');
        return label.Length == 0 ? null : label;
    }

    private static void RequireCapture(Regex regex, string message)
    {
        if (regex.GetGroupNumbers().Length < 2)
        {
            throw new UserError(message);
        }
    }
}
