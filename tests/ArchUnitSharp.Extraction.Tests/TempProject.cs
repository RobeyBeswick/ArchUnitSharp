namespace ArchUnitSharp.Extraction.Tests;

/// <summary>
/// A throwaway directory tree on disk for a single test. Each instance lives under the system temp
/// directory with a unique name, is created empty, and is deleted when disposed.
/// </summary>
internal sealed class TempProject : IDisposable
{
    public TempProject()
    {
        Root = Path.Combine(
            Path.GetTempPath(),
            "ArchUnitSharp.Extraction.Tests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Root);
    }

    /// <summary>The absolute path of this temp tree's root directory.</summary>
    public string Root { get; }

    /// <summary>
    /// Creates a directory under the root, including any intermediate directories, and returns its
    /// absolute path.
    /// </summary>
    public string CreateDirectory(string relativePath)
    {
        string path = Path.Combine(Root, relativePath);
        Directory.CreateDirectory(path);
        return path;
    }

    /// <summary>
    /// Writes a file under the root, creating any intermediate directories, and returns its absolute
    /// path.
    /// </summary>
    public string WriteFile(string relativePath, string content = "")
    {
        string path = Path.Combine(Root, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
        return path;
    }

    /// <summary>
    /// Creates a directory symlink at <paramref name="relativeLinkPath"/> pointing at
    /// <paramref name="targetPath"/>. Returns <see langword="false"/> when the platform refuses to
    /// create directory symlinks (Windows without the privilege or developer mode), so a caller can
    /// skip a symlink test that cannot run.
    /// </summary>
    public bool TryCreateDirectoryLink(string relativeLinkPath, string targetPath)
    {
        string linkPath = Path.Combine(Root, relativeLinkPath);
        Directory.CreateDirectory(Path.GetDirectoryName(linkPath)!);
        try
        {
            Directory.CreateSymbolicLink(linkPath, targetPath);
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    /// <summary>
    /// Creates a file symlink at <paramref name="relativeLinkPath"/> pointing at
    /// <paramref name="targetPath"/>. Returns <see langword="false"/> when the platform refuses to
    /// create file symlinks, so a caller can skip a symlink test that cannot run.
    /// </summary>
    public bool TryCreateFileLink(string relativeLinkPath, string targetPath)
    {
        string linkPath = Path.Combine(Root, relativeLinkPath);
        Directory.CreateDirectory(Path.GetDirectoryName(linkPath)!);
        try
        {
            File.CreateSymbolicLink(linkPath, targetPath);
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(Root, recursive: true);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
