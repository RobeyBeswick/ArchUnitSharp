namespace ArchUnitSharp.Common.Extraction;

/// <summary>
/// The part of a graph identifier that a <see cref="Filter"/> matches its <see cref="Pattern"/>
/// against. The target is bound to the filter, never chosen at the call site, which keeps matching a
/// single generic function.
/// </summary>
/// <remarks>
/// <para>
/// The four targets are: <see cref="Filename"/>, the file's name with no directory part;
/// <see cref="Path"/>, the whole identifier; <see cref="PathWithoutFilename"/>, the identifier with
/// the file's name removed; and <see cref="Classname"/>, the class name derived from the identifier.
/// </para>
/// </remarks>
public enum MatchTarget
{
    /// <summary>The file's name, without any directory part. The identifier <c>src/Models/Car.cs</c> yields <c>Car.cs</c>.</summary>
    Filename = 0,

    /// <summary>The whole identifier, separators and all. The identifier <c>src/Models/Car.cs</c> yields <c>src/Models/Car.cs</c>.</summary>
    Path = 1,

    /// <summary>The identifier with the file's name removed. The identifier <c>src/Models/Car.cs</c> yields <c>src/Models</c>; a root-level file yields the empty string.</summary>
    PathWithoutFilename = 2,

    /// <summary>The class name derived from the identifier: its final extension removed and every separator turned into a dot. The identifier <c>src/Models/Car.cs</c> yields <c>src.Models.Car</c>.</summary>
    Classname = 3,
}
