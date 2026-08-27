namespace ArchUnitSharp.Tests;

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
            "ArchUnitSharp.Tests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Root);
    }

    /// <summary>The absolute path of this temp tree's root directory.</summary>
    public string Root { get; }

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
