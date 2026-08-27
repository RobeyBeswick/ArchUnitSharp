namespace ArchUnitSharp.Extraction;

using ArchUnitSharp.Common.Extraction;

/// <summary>
/// Applies the C#-specific analysis toggles to an enumerated file list: which files are test code
/// and which are generated source, per <see cref="CheckOptions.IgnoreTestCode"/> and
/// <see cref="CheckOptions.IgnoreGeneratedCode"/>. The pure half of the analysis toggles:
/// identifiers in, a decision out, no filesystem.
/// </summary>
/// <remarks>
/// <para>
/// Test code is a file in a folder named <c>test</c> or <c>tests</c> at any depth, matched
/// case-insensitively: <c>src/tests/ProgramTests.cs</c> is test code, but <c>src/App/test.cs</c> is
/// not — the file's own name is irrelevant, only the folders it sits in.
/// </para>
/// <para>
/// Generated source is a file whose name ends in <c>.g.cs</c> or <c>.designer.cs</c>, matched
/// case-insensitively.
/// </para>
/// <para>
/// A file that is both test code and generated source is excluded when either toggle is on. The
/// output preserves the input order, which is the enumeration's sorted order, so a filtered list
/// stays deterministic.
/// </para>
/// <para>
/// This type is stateless and safe for concurrent use. The list it returns is a fresh copy on every
/// call.
/// </para>
/// </remarks>
internal static class SourceFileFilter
{
    /// <summary>
    /// Removes the test and generated files the toggles exclude, preserving the input order.
    /// </summary>
    /// <param name="sourceFiles">The enumerated source files. Must not be <see langword="null"/>.</param>
    /// <param name="ignoreTestCode">Whether files in <c>test</c> or <c>tests</c> folders are excluded.</param>
    /// <param name="ignoreGeneratedCode">Whether generated source files are excluded.</param>
    /// <returns>The files that remain, in the input order.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="sourceFiles"/> is <see langword="null"/>.</exception>
    public static IReadOnlyList<SourceFile> Apply(
        IReadOnlyList<SourceFile> sourceFiles,
        bool ignoreTestCode,
        bool ignoreGeneratedCode)
    {
        ArgumentNullException.ThrowIfNull(sourceFiles);

        return sourceFiles
            .Where(file => !IsExcluded(file, ignoreTestCode, ignoreGeneratedCode))
            .ToArray();
    }

    private static bool IsExcluded(SourceFile file, bool ignoreTestCode, bool ignoreGeneratedCode) =>
        (ignoreTestCode && IsTestCode(file.Identifier))
        || (ignoreGeneratedCode && IsGeneratedCode(file.Identifier));

    private static bool IsTestCode(string identifier)
    {
        int separator = identifier.LastIndexOf('/');
        if (separator < 0)
        {
            return false;
        }

        foreach (string segment in identifier[..separator].Split('/'))
        {
            if (segment.Equals("test", StringComparison.OrdinalIgnoreCase)
                || segment.Equals("tests", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsGeneratedCode(string identifier) =>
        identifier.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase)
        || identifier.EndsWith(".designer.cs", StringComparison.OrdinalIgnoreCase);
}
