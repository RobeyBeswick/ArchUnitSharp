namespace ArchUnitSharp.Tests;

/// <summary>
/// Serializes the tests that mutate the process-global current working directory. xUnit v2 runs test
/// classes as parallel collections, so without this collection <see cref="ProjectTests"/> and
/// <see cref="ProjectLayersTests"/> would run concurrently and one's temporary
/// <see cref="Environment.CurrentDirectory"/> could leak into the other's
/// <c>ProjectLocator.Locate()</c> call. Both classes opt into this collection so they never overlap.
/// </summary>
[CollectionDefinition("cwd")]
public class CwdCollection
{
}
