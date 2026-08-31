namespace ArchUnitSharp.Slices;

using ArchUnitSharp.Common.Extraction;
using ArchUnitSharp.Projection;
using ArchUnitSharp.Slices.Projection;

/// <summary>
/// The slicing projections of the slices module, exported for direct use: the ready-made
/// <see cref="MapFunction"/> hooks a consumer passes to <see cref="ArchUnitSharp.Projection.Projection.Edges"/>,
/// <see cref="ArchUnitSharp.Projection.Projection.ToNodes"/> or
/// <see cref="ArchUnitSharp.Projection.Projection.Cycles"/> to project a graph by slice,
/// without building a <see cref="Slices"/> policy. <c>slice by pattern</c>, <c>slice by regex</c> and
/// <c>slice by file suffix</c> name each file's slice; <c>identity</c> keeps the files themselves.
/// </summary>
/// <remarks>
/// <para>
/// Each relabelling projection relabels an edge's endpoints to their slice labels and drops any edge
/// whose endpoint belongs to no slice — external edges too, because an external target is not a
/// file. A file's self-edge maps to a self-loop on its slice, which node projection consumes and the
/// edge and cycle projections filter out. <see cref="Identity"/> is the exception: it keeps every
/// edge, external ones included, under each file's own identifier. It is the projection layer's own
/// identity map, re-exported here so the four slicing projections live on one surface.
/// </para>
/// <para>
/// This type is stateless and safe for concurrent use. <see cref="ByPattern"/> and
/// <see cref="ByRegex"/> compile their pattern on every call and reject one that cannot name a slice;
/// <see cref="ByFileSuffix"/> and <see cref="Identity"/> return the same instances on every call.
/// </para>
/// </remarks>
public static class Slice
{
    private static readonly MapFunction ByFileSuffixProjection =
        SlicesProjection.Map(SlicesProjection.FileSuffix);

    /// <summary>
    /// <c>slice by pattern</c>: the projection that relabels each file to the slice named by the
    /// <c>(**)</c> capture of <paramref name="glob"/>, and drops any file the glob does not match or
    /// captures an empty name for. A file at <c>src/features/billing/order.cs</c> under
    /// <c>src/features/(**)/*.cs</c> relabels to the slice <c>billing</c>.
    /// </summary>
    /// <param name="glob">The glob to match each file's whole path against. Must not be <see langword="null"/> or empty, and must contain a <c>(**)</c> capture.</param>
    /// <returns>The slicing projection.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="glob"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="glob"/> is empty.</exception>
    /// <exception cref="UserError"><paramref name="glob"/> contains no <c>(**)</c> capture, so it cannot name a slice.</exception>
    public static MapFunction ByPattern(string glob) =>
        SlicesProjection.Map(SliceDefinition.ByPattern(glob).SliceOf);

    /// <summary>
    /// <c>slice by regex</c>: the projection that relabels each file to the slice named by the first
    /// capture group of the anchored pattern <paramref name="pattern"/>, and drops any file the pattern
    /// does not match or captures an empty name for.
    /// </summary>
    /// <param name="pattern">The regex to match each file's whole path against. Must not be <see langword="null"/> or empty, and must contain a capture group.</param>
    /// <returns>The slicing projection.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="pattern"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="pattern"/> is empty.</exception>
    /// <exception cref="UserError"><paramref name="pattern"/> contains no capture group, so it cannot name a slice.</exception>
    public static MapFunction ByRegex(string pattern) =>
        SlicesProjection.Map(SliceDefinition.ByRegex(pattern).SliceOf);

    /// <summary>
    /// <c>slice by file suffix</c>: the projection that relabels each file to its extension — the
    /// final dot and what follows it — so <c>src/Models/Car.cs</c> relabels to the slice <c>.cs</c>.
    /// A file with no extension has no suffix and is dropped.
    /// </summary>
    /// <returns>The slicing projection.</returns>
    public static MapFunction ByFileSuffix() => ByFileSuffixProjection;

    /// <summary>
    /// <c>identity</c>: the projection that keeps every file under its own identifier — the slicing
    /// where each file is its own slice. Re-exported from the projection layer so the four slicing
    /// projections live on one surface.
    /// </summary>
    public static MapFunction Identity => MapFunctions.Identity;
}
