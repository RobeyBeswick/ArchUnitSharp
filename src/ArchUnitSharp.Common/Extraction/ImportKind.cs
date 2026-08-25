namespace ArchUnitSharp.Common.Extraction;

/// <summary>
/// The kind of a C# import that an <see cref="Edge"/> records. Flag values so that parallel edges
/// between the same two files can be merged with their import kinds unioned, and so that filters
/// can test membership with <see cref="Enum.HasFlag"/>.
/// </summary>
[Flags]
public enum ImportKind
{
    /// <summary>No import kind. The zero value; never produced by parsing a real import.</summary>
    None = 0,

    /// <summary>A regular <c>using Namespace;</c> directive.</summary>
    Using = 1 << 0,

    /// <summary>A <c>using static Type;</c> directive.</summary>
    UsingStatic = 1 << 1,

    /// <summary>A <c>global using Namespace;</c> directive.</summary>
    GlobalUsing = 1 << 2,

    /// <summary>An aliased <c>using Alias = Namespace;</c> directive.</summary>
    AliasUsing = 1 << 3,

    /// <summary>An <c>extern alias Name;</c> directive.</summary>
    ExternAlias = 1 << 4,
}
