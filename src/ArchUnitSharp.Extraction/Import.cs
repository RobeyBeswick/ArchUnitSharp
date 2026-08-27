namespace ArchUnitSharp.Extraction;

using ArchUnitSharp.Common.Extraction;

/// <summary>
/// A single <c>using</c> directive found in a C# source file: the kind of directive it is and the
/// name it references. The name is exactly as written — <c>System.Linq</c> for
/// <c>using System.Linq;</c>, the referenced type for <c>using static</c>, the right-hand side for
/// an alias — and is what <see cref="ImportResolver"/> binds to a target.
/// </summary>
/// <remarks>
/// <para>
/// The name is not yet a target: it is the raw reference a directive carries. Resolution — deciding
/// whether the name is a namespace or type the project declares and, if so, which files declare it —
/// is <see cref="ImportResolver"/>'s job. This type is the parser's output and the resolver's input.
/// </para>
/// <para>
/// This type is immutable and value-semantic: two imports with the same kind and name are equal.
/// </para>
/// </remarks>
public sealed record Import
{
    private readonly string _name;

    /// <summary>
    /// The kind of <c>using</c> directive this import is. A directive is classified as exactly one
    /// kind: <see cref="ImportKind.GlobalUsing"/> wins over <see cref="ImportKind.UsingStatic"/>
    /// which wins over <see cref="ImportKind.AliasUsing"/>, so a <c>global using static</c> is a
    /// <see cref="ImportKind.GlobalUsing"/>.
    /// </summary>
    public ImportKind Kind { get; init; }

    /// <summary>
    /// The name the directive references, exactly as written. For <c>using System.Linq;</c> this is
    /// <c>System.Linq</c>; for <c>using static System.Math;</c> it is <c>System.Math</c>; for
    /// <c>using Foo = System.Text;</c> it is <c>System.Text</c>. Must not be <see langword="null"/>
    /// or empty; both the constructor and a <see langword="with"/> expression route through the same
    /// validation, so neither can introduce a bad value.
    /// </summary>
    public string Name
    {
        get => _name;
        init => _name = Require(value);
    }

    /// <summary>
    /// Creates an import.
    /// </summary>
    /// <param name="kind">The kind of <c>using</c> directive this import is.</param>
    /// <param name="name">The referenced name; must not be <see langword="null"/> or empty.</param>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is empty.</exception>
    public Import(ImportKind kind, string name)
    {
        Kind = kind;
        _name = Require(name);
    }

    private static string Require(string name) =>
        name is null
            ? throw new ArgumentNullException(nameof(Name))
            : name.Length == 0
                ? throw new ArgumentException("Name must not be empty.", nameof(Name))
                : name;
}
