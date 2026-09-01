namespace ArchUnitSharp.Dogfood.Tests;

using ArchUnitSharp.Extraction;

/// <summary>
/// Locates the repository's project for the dogfood suite: the root directory holding
/// <c>ArchUnitSharp.sln</c>, found by walking up from the test assembly's output directory. The
/// library is then run against this root — its own source.
/// </summary>
/// <remarks>
/// <para>
/// The locator searches for a <c>.sln</c> at or above the start directory first and only falls back
/// to a <c>.csproj</c> when no ancestor holds a solution, so handed the test output directory it
/// walks straight past the test project's own <c>.csproj</c> to <c>ArchUnitSharp.sln</c> at the
/// repository root — the whole repository, not just this suite's handful of files.
/// </para>
/// <para>
/// The located project is computed once per process. Extraction of the whole source tree is
/// memoised by the graph cache, so every rule in the suite shares one <see cref="Graph"/> read from
/// disk once.
/// </para>
/// </remarks>
internal static class Repository
{
    /// <summary>
    /// The repository's located project, computed lazily on first use and reused by every rule.
    /// </summary>
    public static ProjectLocation Location { get; } = Locate();

    private static ProjectLocation Locate() => ProjectLocator.Locate(AppContext.BaseDirectory);
}
