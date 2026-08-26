namespace ArchUnitSharp.Extraction;

/// <summary>
/// Normalises path separators to forward slashes so a path behaves identically on every operating
/// system. The rest of extraction assumes this: every identifier and absolute path that leaves this
/// project is forward-slash normalised, never a mixture of conventions.
/// </summary>
internal static class PathNormaliser
{
    /// <summary>Returns <paramref name="path"/> with every backslash replaced by a forward slash.</summary>
    public static string Normalise(string path) => path.Replace('\\', '/');
}
