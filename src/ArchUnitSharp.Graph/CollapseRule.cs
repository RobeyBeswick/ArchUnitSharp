namespace ArchUnitSharp.Graph;

using ArchUnitSharp.Common.Extraction;

/// <summary>
/// The internal data model of one collapse rule: the instruction that relabels a file's identifier to
/// a coarser node label. <see cref="FolderDepth"/> relabels every file to the folder at a fixed depth;
/// <see cref="Pattern"/> relabels the files whose whole path matches a glob to a single bucket.
/// </summary>
/// <remarks>
/// <para>
/// A <see cref="FolderDepth"/> rule relabels every file it is applied to — the folder is always
/// defined — so a folder-depth rule in a list makes every later rule unreachable. Put pattern rules
/// first when combining the two. A <see cref="Pattern"/> rule relabels only the files whose
/// identifier matches its filter's glob, and the bucket's label is the glob itself.
/// </para>
/// <para>
/// This type is immutable and value-semantic, and safe for concurrent use.
/// </para>
/// </remarks>
internal abstract record CollapseRule
{
    /// <summary>
    /// Relabels each file to the folder of its identifier truncated to <see cref="Depth"/> path
    /// segments; a file at a root-level or a depth of zero relabels to the root bucket.
    /// </summary>
    public sealed record FolderDepth(int Depth) : CollapseRule;

    /// <summary>
    /// Relabels each file whose whole identifier matches <see cref="Filter"/> to a single bucket
    /// labeled with the filter's glob.
    /// </summary>
    public sealed record Pattern(Filter Filter) : CollapseRule;
}
