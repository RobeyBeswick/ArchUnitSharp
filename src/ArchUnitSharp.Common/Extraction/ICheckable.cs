namespace ArchUnitSharp.Common.Extraction;

/// <summary>
/// The seam the whole library hangs from: a rule that can be checked and reports the violations it
/// found. Every terminal implements this interface; every consumer programs against it and nothing
/// else.
/// </summary>
/// <remarks>
/// <para>
/// A rule is <em>checked</em> with <see cref="Check(CheckOptions?)"/> and the result is the list of
/// <see cref="Violation"/> instances it found — an empty list means the rule passed. A failing rule
/// yields violations in that list; it never throws. <see langword="null"/> options mean the defaults
/// carried by <see cref="CheckOptions"/>.
/// </para>
/// <para>
/// The one unexported member, <c>ProhibitExternalImplementation</c>, exists so that no code outside
/// this library can implement the interface: this is a seam for <em>consumption</em>, not for
/// extension. This type is safe for concurrent use.
/// </para>
/// </remarks>
public interface ICheckable
{
    /// <summary>
    /// Checks this rule and returns the violations it found. An empty list means the rule passed.
    /// </summary>
    /// <param name="options">The options to check with; <see langword="null"/> means the defaults in <see cref="CheckOptions"/>.</param>
    /// <returns>The violations found; empty when the rule passed.</returns>
    IReadOnlyList<Violation> Check(CheckOptions? options = null);

    internal void ProhibitExternalImplementation();
}
