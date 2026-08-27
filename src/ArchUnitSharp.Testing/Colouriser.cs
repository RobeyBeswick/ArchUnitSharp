namespace ArchUnitSharp.Testing;

/// <summary>
/// The colour utilities on top of the message formatting: wraps text in the ANSI escape codes that
/// render it in a <see cref="Colour"/>, and colours a shaped <see cref="CheckResult"/> by its verdict —
/// green when it passed, red when it failed. Nothing here changes the message text; it only decorates
/// it, so a report that does not want colour simply calls <see cref="ResultFactory"/> and ignores this
/// type.
/// </summary>
/// <remarks>
/// <para>
/// The codes written are the standard ANSI SGR sequence: the chosen colour's code, then the text, then
/// the reset code, so the colour never bleeds into following output. Colour is opt-in; no type in this
/// library colours its own output.
/// </para>
/// <para>
/// This type is stateless and safe for concurrent use. The strings and <see cref="CheckResult"/> values
/// it returns are freshly built on every call.
/// </para>
/// </remarks>
public static class Colouriser
{
    /// <summary>
    /// The ANSI SGR escape introducer: <c>ESC</c> followed by <c>[</c>.
    /// </summary>
    private const string Escape = "\u001b[";

    /// <summary>
    /// The ANSI SGR reset code that restores the terminal's default rendering.
    /// </summary>
    private const string Reset = "\u001b[0m";

    /// <summary>
    /// Wraps <paramref name="text"/> in the ANSI escape codes that render it in
    /// <paramref name="colour"/>: the colour's code before the text and the reset code after it.
    /// </summary>
    /// <param name="text">The text to colour. Must not be <see langword="null"/>.</param>
    /// <param name="colour">The colour to render the text in.</param>
    /// <returns>The text wrapped in the colour's escape codes.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="text"/> is <see langword="null"/>.</exception>
    public static string Apply(string text, Colour colour)
    {
        ArgumentNullException.ThrowIfNull(text);
        return $"{Escape}{(int)colour}m{text}{Reset}";
    }

    /// <summary>
    /// Colours <paramref name="result"/>'s message by its verdict: green when
    /// <see cref="CheckResult.Passed"/> is <see langword="true"/>, red when it is
    /// <see langword="false"/>. Returns a new <see cref="CheckResult"/>; <paramref name="result"/> is
    /// unchanged.
    /// </summary>
    /// <param name="result">The result to colour. Must not be <see langword="null"/>.</param>
    /// <returns>A result with the same verdict and the message rendered in the verdict's colour.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="result"/> is <see langword="null"/>.</exception>
    public static CheckResult Apply(CheckResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        Colour colour = result.Passed ? Colour.Green : Colour.Red;
        return result with { Message = Apply(result.Message, colour) };
    }
}
