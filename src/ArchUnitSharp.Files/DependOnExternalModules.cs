namespace ArchUnitSharp.Files;

using System.Text;
using ArchUnitSharp.Common.Extraction;
using ArchUnitSharp.Files.Assertion;

/// <summary>
/// The predicate and object of a <c>should depend on external modules</c> /
/// <c>should not depend on external modules</c> rule: the third-party modules a rule's subject must
/// or must not depend on. Built from <see cref="Should.DependOnExternalModules"/> and
/// <see cref="ShouldNot.DependOnExternalModules"/>; its <see cref="Matching"/> selector narrows the
/// object to the external modules whose name matches the given glob, and repeats combine with OR — a
/// module is selected when any one selector matches its name. Checking it runs the shared
/// <see cref="FilesAssertion.DependOnExternalModules"/> assertion, which routes an empty selection or
/// object through the shared <see cref="EmptyTestGuard"/>.
/// </summary>
/// <remarks>
/// <para>
/// With the positive mood, every selected file must depend on at least one external module whose name
/// matches at least one selector; a selected file that depends on none is reported as one
/// <see cref="FileViolation"/>. With the negated mood, no selected file may depend on an external
/// module whose name matches any selector; each offending dependency is reported as one
/// <see cref="DependencyViolation"/>.
/// </para>
/// <para>
/// An external module is the target of an external edge: a name no file in the project declares, kept
/// as written — <c>System.Linq</c> for <c>using System.Linq;</c>. An internal target is a file, not a
/// module, so it is never a dependency this rule counts, and a self-edge is never a dependency either.
/// </para>
/// <para>
/// Every selector returns a new <see cref="DependOnExternalModules"/> instance and never mutates the
/// one it was called on, so a half-built object can be stored in a variable and branched from without
/// one branch seeing another's selectors. This type is immutable and safe for concurrent use.
/// </para>
/// </remarks>
public sealed class DependOnExternalModules : ICheckable
{
    private readonly Files _files;
    private readonly bool _negate;
    private readonly Filter[] _filters;

    /// <summary>
    /// Creates the object of a <c>should (not) depend on external modules</c> rule over
    /// <paramref name="files"/> with the given mood. Callers obtain a
    /// <see cref="DependOnExternalModules"/> from <see cref="Should.DependOnExternalModules"/> or
    /// <see cref="ShouldNot.DependOnExternalModules"/> rather than constructing one.
    /// </summary>
    /// <param name="files">The selection the rule asserts over.</param>
    /// <param name="negate">
    /// <see langword="false"/> for the positive mood, <see langword="true"/> for the negated mood.
    /// </param>
    internal DependOnExternalModules(Files files, bool negate)
    {
        _files = files;
        _negate = negate;
        _filters = Array.Empty<Filter>();
    }

    private DependOnExternalModules(Files files, bool negate, Filter[] filters)
    {
        _files = files;
        _negate = negate;
        _filters = filters;
    }

    /// <summary>
    /// Narrows the object to the external modules whose name matches <paramref name="glob"/>, the
    /// module name being the target of an external edge as written. Repeats combine with OR: a module
    /// is selected when any one selector matches its name. Returns a new
    /// <see cref="DependOnExternalModules"/>; the current object is unchanged.
    /// </summary>
    /// <param name="glob">The glob to match the external module's name against. Must not be <see langword="null"/> or empty.</param>
    /// <returns>A new object narrowed to the external modules whose name matches.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="glob"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="glob"/> is empty.</exception>
    public DependOnExternalModules Matching(string glob) =>
        Add(new Filter(new Pattern(glob), MatchTarget.Path));

    /// <summary>
    /// Checks this <c>should (not) depend on external modules</c> rule and returns the violations it
    /// found. An empty list means the rule passed.
    /// </summary>
    /// <param name="options">The options to check with; <see langword="null"/> means the defaults in <see cref="CheckOptions"/>.</param>
    /// <returns>The violations found; empty when the rule passed.</returns>
    public IReadOnlyList<Violation> Check(CheckOptions? options = null) =>
        FilesAssertion.DependOnExternalModules(this, options);

    /// <inheritdoc/>
    void ICheckable.ProhibitExternalImplementation()
    {
    }

    /// <summary>
    /// The selection the rule asserts over.
    /// </summary>
    internal Files Subject => _files;

    /// <summary>
    /// <see langword="true"/> for the negated mood, <see langword="false"/> for the positive mood.
    /// </summary>
    internal bool Negate => _negate;

    /// <summary>
    /// The object's selectors, in the order they were applied.
    /// </summary>
    internal IReadOnlyList<Filter> ObjectFilters => _filters;

    /// <summary>
    /// Describes this object as the object of a rule, for a report: the noun phrase
    /// <c>external modules</c> followed by one clause per selector. An object narrowed by
    /// <c>Matching("System.*")</c> is described as <c>external modules matching 'System.*'</c>.
    /// </summary>
    internal string DescribeObject()
    {
        var builder = new StringBuilder("external modules");
        foreach (Filter filter in _filters)
        {
            builder.Append(" matching '");
            builder.Append(filter.Pattern.Glob);
            builder.Append('\'');
        }

        return builder.ToString();
    }

    private DependOnExternalModules Add(Filter filter)
    {
        var filters = new Filter[_filters.Length + 1];
        Array.Copy(_filters, filters, _filters.Length);
        filters[_filters.Length] = filter;
        return new DependOnExternalModules(_files, _negate, filters);
    }
}
