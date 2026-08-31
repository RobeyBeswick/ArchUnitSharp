namespace ArchUnitSharp.Slices.Uml;

using System.Text.RegularExpressions;
using ArchUnitSharp.Common.Extraction;

/// <summary>
/// The line-based parser for the supported PlantUML component-diagram subset: <c>component [Name]</c>
/// declarations, <c>[source] -&gt; [target]</c> and <c>[source] --&gt; [target]</c> arrows, single-quote
/// and <c>//</c> comments, and <c>@startuml</c>/<c>@enduml</c> directives. A line the subset does not
/// recognise is ignored — <c>skinparam</c>, titles and the like — so a diagram that uses more of
/// PlantUML than this subset still parses.
/// </summary>
/// <remarks>
/// <para>
/// Each line is trimmed and its trailing <c>'</c> comment stripped before it is read. A whole-line
/// comment (<c>' ...</c> or <c>// ...</c>) and a directive line (<c>@startuml</c>, <c>@enduml</c> or any
/// other <c>@</c>-prefixed line) are skipped. A <c>component</c> line that is not a well-formed
/// <c>component [Name]</c> declaration, and a line that starts with <c>[</c> but is not a well-formed
/// bracketed arrow, are a <see cref="UserError"/> naming the offending line number — a typo in the
/// diagram must not silently vanish. An arrow's endpoints are components too, declared or not.
/// </para>
/// <para>
/// This type is stateless and safe for concurrent use.
/// </para>
/// </remarks>
internal static class PlantUmlParser
{
    private static readonly Regex ComponentPattern = new(
        @"^component\s+\[([^\]]+)\]",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly Regex DependencyPattern = new(
        @"^\[([^\]]+)\]\s*-{1,2}>\s*\[([^\]]+)\]",
        RegexOptions.CultureInvariant);

    /// <summary>
    /// Parses diagram text into an immutable <see cref="PlantUmlDiagram"/>. The text may carry the
    /// <c>@startuml</c>/<c>@enduml</c> delimiters or omit them; a malformed declaration or arrow is a
    /// <see cref="UserError"/> naming the line.
    /// </summary>
    /// <param name="text">The diagram text. Must not be <see langword="null"/> or blank.</param>
    /// <returns>The parsed diagram.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="text"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="text"/> is blank.</exception>
    /// <exception cref="UserError">A line is a malformed <c>component</c> declaration or bracketed arrow.</exception>
    public static PlantUmlDiagram Parse(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (text.Trim().Length == 0)
        {
            throw new ArgumentException("Diagram text must not be empty.", nameof(text));
        }

        var components = new List<string>();
        var dependencies = new List<PlantUmlDependency>();

        string[] lines = text.Split('\n');
        for (int index = 0; index < lines.Length; index++)
        {
            ParseLine(lines[index], index + 1, components, dependencies);
        }

        return new PlantUmlDiagram(components, dependencies);
    }

    /// <summary>
    /// Reads one line of the diagram into the component and dependency lists.
    /// </summary>
    private static void ParseLine(
        string line,
        int lineNumber,
        List<string> components,
        List<PlantUmlDependency> dependencies)
    {
        line = line.Trim();
        if (line.Length == 0 || line[0] == '@' || line[0] == '\'' || line.StartsWith("//", StringComparison.Ordinal))
        {
            return;
        }

        int comment = line.IndexOf('\'');
        if (comment >= 0)
        {
            line = line.Substring(0, comment).Trim();
        }

        if (line.Length == 0)
        {
            return;
        }

        Match component = ComponentPattern.Match(line);
        if (component.Success)
        {
            string name = component.Groups[1].Value.Trim();
            if (name.Length == 0)
            {
                throw new UserError(
                    $"Malformed component declaration on line {lineNumber}: '{line}'. Expected 'component [Name]'.");
            }

            components.Add(name);
            return;
        }

        if (line.StartsWith("component", StringComparison.OrdinalIgnoreCase))
        {
            throw new UserError(
                $"Malformed component declaration on line {lineNumber}: '{line}'. Expected 'component [Name]'.");
        }

        Match dependency = DependencyPattern.Match(line);
        if (dependency.Success)
        {
            string source = dependency.Groups[1].Value.Trim();
            string target = dependency.Groups[2].Value.Trim();
            if (source.Length == 0 || target.Length == 0)
            {
                throw new UserError(
                    $"Malformed dependency arrow on line {lineNumber}: '{line}'. Expected '[Source] --> [Target]'.");
            }

            dependencies.Add(new PlantUmlDependency(source, target));
            components.Add(source);
            components.Add(target);
            return;
        }

        if (line[0] == '[')
        {
            throw new UserError(
                $"Malformed dependency arrow on line {lineNumber}: '{line}'. Expected '[Source] --> [Target]'.");
        }
    }
}
