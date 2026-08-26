namespace ArchUnitSharp.Common.Extraction;

using System.Text;

/// <summary>
/// The named matcher factories: the one seam the fluent surface uses to turn a user pattern into a
/// ready-to-use <see cref="Filter"/>. There is one factory method per selector kind — filename,
/// folder, path, classname and exact file — and the matching itself stays the single generic
/// <see cref="Filter.Matches"/>. Adding a new selector means adding a factory method here, never a
/// new branch in the matcher.
/// </summary>
/// <remarks>
/// <para>
/// Every factory ultimately compiles through <see cref="RegexFactory.CompileGlob"/> (via
/// <see cref="Pattern"/>), so the library keeps exactly one place that turns a user pattern into a
/// compiled <see cref="System.Text.RegularExpressions.Regex"/>. The glob factories bind the pattern
/// to the matching <see cref="MatchTarget"/>; <see cref="ExactFile"/> treats its argument as a
/// literal identifier, so glob characters in it have no special meaning.
/// </para>
/// <para>
/// This type is stateless and safe for concurrent use; the filters it returns are immutable.
/// </para>
/// </remarks>
public static class MatcherFactory
{
    /// <summary>
    /// A filter that matches the file-name part of an identifier against <paramref name="glob"/>. A
    /// <c>*.cs</c> pattern matches <c>src/Models/Car.cs</c> (its name is <c>Car.cs</c>) but not
    /// <c>src/Models/Car.txt</c>.
    /// </summary>
    /// <param name="glob">The glob to match file names with. Must not be <see langword="null"/> or empty.</param>
    /// <returns>A <see cref="Filter"/> bound to <see cref="MatchTarget.Filename"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="glob"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="glob"/> is empty.</exception>
    public static Filter Filename(string glob) => new(new Pattern(glob), MatchTarget.Filename);

    /// <summary>
    /// A filter that matches the directory part of an identifier against <paramref name="glob"/>. A
    /// <c>src/Models</c> pattern matches <c>src/Models/Car.cs</c> but not <c>src/Other/Car.cs</c>.
    /// A root-level file has an empty directory part.
    /// </summary>
    /// <param name="glob">The glob to match directories with. Must not be <see langword="null"/> or empty.</param>
    /// <returns>A <see cref="Filter"/> bound to <see cref="MatchTarget.PathWithoutFilename"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="glob"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="glob"/> is empty.</exception>
    public static Filter Folder(string glob) => new(new Pattern(glob), MatchTarget.PathWithoutFilename);

    /// <summary>
    /// A filter that matches the whole identifier against <paramref name="glob"/>. A
    /// <c>**/*.cs</c> pattern matches <c>src/Models/Car.cs</c> and <c>Car.cs</c>.
    /// </summary>
    /// <param name="glob">The glob to match whole identifiers with. Must not be <see langword="null"/> or empty.</param>
    /// <returns>A <see cref="Filter"/> bound to <see cref="MatchTarget.Path"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="glob"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="glob"/> is empty.</exception>
    public static Filter Path(string glob) => new(new Pattern(glob), MatchTarget.Path);

    /// <summary>
    /// A filter that matches the class name derived from an identifier against <paramref name="glob"/>.
    /// A <c>**/*Controller</c> pattern matches <c>src/Controllers/HomeController.cs</c> (whose derived
    /// class name is <c>src.Controllers.HomeController</c>).
    /// </summary>
    /// <param name="glob">The glob to match class names with. Must not be <see langword="null"/> or empty.</param>
    /// <returns>A <see cref="Filter"/> bound to <see cref="MatchTarget.Classname"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="glob"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="glob"/> is empty.</exception>
    public static Filter Classname(string glob) => new(new Pattern(glob), MatchTarget.Classname);

    /// <summary>
    /// A filter that matches the whole identifier as a literal, so <paramref name="identifier"/> is
    /// treated exactly and glob characters in it have no special meaning. <c>a*.cs</c> matches a file
    /// whose full identifier is literally <c>a*.cs</c> and nothing else. Backslash separators are
    /// normalised, so an identifier behaves the same on every operating system.
    /// </summary>
    /// <param name="identifier">The exact file identifier to match. Must not be <see langword="null"/> or empty.</param>
    /// <returns>A <see cref="Filter"/> bound to <see cref="MatchTarget.Path"/> that matches exactly the given identifier.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="identifier"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="identifier"/> is empty.</exception>
    public static Filter ExactFile(string identifier) =>
        new(new Pattern(EscapeLiteral(identifier)), MatchTarget.Path);

    private static string EscapeLiteral(string identifier)
    {
        ArgumentNullException.ThrowIfNull(identifier);
        if (identifier.Length == 0)
        {
            throw new ArgumentException("An exact file identifier must not be empty.", nameof(identifier));
        }

        var builder = new StringBuilder(identifier.Length * 2);
        foreach (char c in identifier)
        {
            switch (c)
            {
                case '*':
                    builder.Append("[*]");
                    break;

                case '?':
                    builder.Append("[?]");
                    break;

                case '[':
                    builder.Append("[[]");
                    break;

                default:
                    builder.Append(c);
                    break;
            }
        }

        return builder.ToString();
    }
}
