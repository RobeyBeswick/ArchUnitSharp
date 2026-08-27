namespace ArchUnitSharp.Testing.Xunit;

using System.Runtime.CompilerServices;

/// <summary>
/// The import-time wiring of the xUnit integration: when this assembly is loaded — which happens as
/// soon as a test project references it and runs — a module initializer silently detects whether the
/// process is genuinely running under xUnit and records the outcome. The assertion surface reads that
/// outcome, so there is zero setup: no registration call, no configuration, just the reference.
/// </summary>
/// <remarks>
/// <para>
/// The detection is <em>silent</em>: it never reports anything and never throws. It asks one question —
/// is an xUnit <em>runner</em> or <em>execution</em> assembly loaded? — and stores a single boolean.
/// The runner and execution assemblies are present only while tests are actually being executed by
/// xUnit; merely referencing the <c>xunit</c> packages does not load them, so the check cannot be
/// satisfied by accident. When the check says yes, <see cref="XunitAssert"/> translates rule outcomes
/// through xUnit's own <c>Assert.True</c> / <c>Assert.False</c>; when it says no — a non-xUnit run,
/// such as NUnit or MSTest, with this package referenced — the same surface silently falls back to the
/// framework-agnostic <see cref="RuleAssert"/>, which is what covers those frameworks.
/// </para>
/// <para>
/// <see cref="Native"/> is written exactly once, by <see cref="Initialize"/>, and never mutated again,
/// so reading it concurrently is safe.
/// </para>
/// </remarks>
internal static class XunitAdapter
{
    /// <summary>
    /// Whether this process was running under xUnit when the assembly was imported:
    /// <see langword="true"/> makes <see cref="XunitAssert"/> native, <see langword="false"/> makes it
    /// fall back to the framework-agnostic <see cref="RuleAssert"/>.
    /// </summary>
    internal static bool Native;

    /// <summary>
    /// Runs when the assembly is imported and performs the silent detection: whether an xUnit runner or
    /// execution assembly is already loaded. Never throws and never reports, so importing this package
    /// in any test setup is harmless.
    /// </summary>
#pragma warning disable CA2255 // The import-time registration is the whole point of this adapter: it is the zero-setup contract the issue asks for, and the module initializer only sets one internal boolean.
    [ModuleInitializer]
    internal static void Initialize()
#pragma warning restore CA2255
    {
        Native = Detect(AppDomain.CurrentDomain.GetAssemblies().Select(static assembly => assembly.GetName().Name));
    }

    /// <summary>
    /// Decides whether any of <paramref name="assemblyNames"/> marks a live xUnit run: a name with the
    /// <c>xunit.runner.</c> or <c>xunit.execution.</c> prefix. Pure, so a test can drive it with
    /// hand-built name lists.
    /// </summary>
    /// <param name="assemblyNames">The names of the loaded assemblies. Must not be <see langword="null"/>.</param>
    /// <returns><see langword="true"/> when an xUnit runner or execution assembly is present.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="assemblyNames"/> is <see langword="null"/>.</exception>
    internal static bool Detect(IEnumerable<string?> assemblyNames)
    {
        ArgumentNullException.ThrowIfNull(assemblyNames);
        return assemblyNames.Any(static name => name is not null && IsXunitRunner(name));
    }

    /// <summary>
    /// Whether <paramref name="assemblyName"/> is an xUnit runner or execution assembly: the only
    /// assemblies the xUnit runner loads that mere package references never do.
    /// </summary>
    /// <param name="assemblyName">An assembly's simple name; <see langword="null"/> when unknown.</param>
    /// <returns><see langword="true"/> when the name marks a live xUnit run.</returns>
    internal static bool IsXunitRunner(string assemblyName) =>
        assemblyName.StartsWith("xunit.runner.", StringComparison.Ordinal)
        || assemblyName.StartsWith("xunit.execution.", StringComparison.Ordinal);
}
