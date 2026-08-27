namespace ArchUnitSharp.Files;

using System.Text;
using ArchUnitSharp.Common.Extraction;
using ArchUnitSharp.Files.Assertion;

/// <summary>
/// The predicate and object of a <c>should depend on files</c> / <c>should not depend on files</c>
/// rule: the files a rule's subject must or must not depend on. Built from <see cref="Should.DependOn"/>
/// and <see cref="ShouldNot.DependOn"/>; its selector methods narrow the object with the same words a
/// scope uses, so an object reads as <c>files with name 'X'</c>, and selectors combine with AND — a
/// dependency target must match every one of them. Checking it runs the shared
/// <see cref="FilesAssertion.DependOn"/> assertion, which is where the empty-test guard lives.
/// </summary>
/// <remarks>
/// <para>
/// With the positive mood, every selected file must depend on at least one file that matches every
/// object selector; a selected file that depends on none is reported as one <see cref="FileViolation"/>.
/// With the negated mood, no selected file may depend on any file that matches every object selector;
/// each offending dependency is reported as one <see cref="DependencyViolation"/>.
/// </para>
/// <para>
/// A self-edge is not a dependency — a file never depends on itself — and an external target is not a
/// file, so neither is ever a dependency the rule counts.
/// </para>
/// <para>
/// Every selector returns a new <see cref="DependOn"/> instance and never mutates the one it was
/// called on, so a half-built object can be stored in a variable and branched from without one branch
/// seeing another's selectors. This type is immutable and safe for concurrent use.
/// </para>
/// </remarks>
public sealed class DependOn : ICheckable
{
    private readonly Files _files;
    private readonly bool _negate;
    private readonly Filter[] _filters;

    /// <summary>
    /// Creates the object of a <c>should (not) depend on files</c> rule over <paramref name="files"/>
    /// with the given mood. Callers obtain a <see cref="DependOn"/> from <see cref="Should.DependOn"/>
    /// or <see cref="ShouldNot.DependOn"/> rather than constructing one.
    /// </summary>
    /// <param name="files">The selection the rule asserts over.</param>
    /// <param name="negate">
    /// <see langword="false"/> for the positive mood, <see langword="true"/> for the negated mood.
    /// </param>
    internal DependOn(Files files, bool negate)
    {
        _files = files;
        _negate = negate;
        _filters = Array.Empty<Filter>();
    }

    private DependOn(Files files, bool negate, Filter[] filters)
    {
        _files = files;
        _negate = negate;
        _filters = filters;
    }

    /// <summary>
    /// Narrows the object to the files whose name matches <paramref name="glob"/>, the same selector a
    /// scope uses. Returns a new <see cref="DependOn"/>; the current object is unchanged.
    /// </summary>
    /// <param name="glob">The glob to match the dependency target's name against. Must not be <see langword="null"/> or empty.</param>
    /// <returns>A new object narrowed to the files whose name matches.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="glob"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="glob"/> is empty.</exception>
    public DependOn WithName(string glob) => Add(new Filter(new Pattern(glob), MatchTarget.Filename));

    /// <summary>
    /// Narrows the object to the files that sit in the folder matching <paramref name="glob"/>, the
    /// same selector a scope uses. Returns a new <see cref="DependOn"/>; the current object is
    /// unchanged.
    /// </summary>
    /// <param name="glob">The glob to match the dependency target's folder against. Must not be <see langword="null"/> or empty.</param>
    /// <returns>A new object narrowed to the files whose folder matches.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="glob"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="glob"/> is empty.</exception>
    public DependOn InFolder(string glob) => Add(new Filter(new Pattern(glob), MatchTarget.PathWithoutFilename));

    /// <summary>
    /// Narrows the object to the files whose whole path matches <paramref name="glob"/>, the same
    /// selector a scope uses. Returns a new <see cref="DependOn"/>; the current object is unchanged.
    /// </summary>
    /// <param name="glob">The glob to match the dependency target's whole path against. Must not be <see langword="null"/> or empty.</param>
    /// <returns>A new object narrowed to the files whose path matches.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="glob"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="glob"/> is empty.</exception>
    public DependOn InPath(string glob) => Add(new Filter(new Pattern(glob), MatchTarget.Path));

    /// <summary>
    /// Narrows the object to the files whose file name matches <paramref name="glob"/>, the same
    /// selector a scope uses. Returns a new <see cref="DependOn"/>; the current object is unchanged.
    /// </summary>
    /// <param name="glob">The glob to match the dependency target's file name against. Must not be <see langword="null"/> or empty.</param>
    /// <returns>A new object narrowed to the files whose file name matches.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="glob"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="glob"/> is empty.</exception>
    public DependOn InFile(string glob) => Add(new Filter(new Pattern(glob), MatchTarget.Classname));

    /// <summary>
    /// Checks this <c>should (not) depend on files</c> rule and returns the violations it found. An
    /// empty list means the rule passed.
    /// </summary>
    /// <param name="options">The options to check with; <see langword="null"/> means the defaults in <see cref="CheckOptions"/>.</param>
    /// <returns>The violations found; empty when the rule passed.</returns>
    public IReadOnlyList<Violation> Check(CheckOptions? options = null) =>
        FilesAssertion.DependOn(this, options);

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
    /// Describes this object as the object of a rule, for a report: the noun phrase <c>files</c>
    /// followed by one clause per selector, in the selector's own words. An object narrowed by
    /// <c>WithName("Car.cs")</c> is described as <c>files with name 'Car.cs'</c>.
    /// </summary>
    internal string DescribeObject()
    {
        var builder = new StringBuilder("files");
        foreach (Filter filter in _filters)
        {
            builder.Append(' ');
            builder.Append(Files.SelectorWord(filter.Target));
            builder.Append(" '");
            builder.Append(filter.Pattern.Glob);
            builder.Append('\'');
        }

        return builder.ToString();
    }

    private DependOn Add(Filter filter)
    {
        var filters = new Filter[_filters.Length + 1];
        Array.Copy(_filters, filters, _filters.Length);
        filters[_filters.Length] = filter;
        return new DependOn(_files, _negate, filters);
    }
}
