namespace ArchUnitSharp.Testing;

/// <summary>
/// The terminal colours a report can render with, as ANSI foreground colours: the value each member
/// carries is the colour's SGR code, which <see cref="Colouriser"/> wraps text in. Colouring is
/// layered on top of the message formatting — a report may render the shaped message from
/// <see cref="ResultFactory"/> in colour without ever touching the message text itself.
/// </summary>
public enum Colour
{
    /// <summary>ANSI black.</summary>
    Black = 30,

    /// <summary>ANSI red.</summary>
    Red = 31,

    /// <summary>ANSI green.</summary>
    Green = 32,

    /// <summary>ANSI yellow.</summary>
    Yellow = 33,

    /// <summary>ANSI blue.</summary>
    Blue = 34,

    /// <summary>ANSI magenta.</summary>
    Magenta = 35,

    /// <summary>ANSI cyan.</summary>
    Cyan = 36,

    /// <summary>ANSI white.</summary>
    White = 37,
}
