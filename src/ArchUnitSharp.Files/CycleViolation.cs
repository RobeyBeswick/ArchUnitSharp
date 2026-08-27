namespace ArchUnitSharp.Files;

using ArchUnitSharp.Common.Extraction;

/// <summary>
/// A violation produced by the <c>should have no cycles</c> files rule predicate: the projected
/// dependency graph of the rule's selection contains a cycle. Carries the cycle as the ordered files
/// that form it, and renders it as the readable path a report prints.
/// </summary>
/// <remarks>
/// <para>
/// The cycle is carried as <see cref="Files"/>: the ordered file identifiers that form the closed
/// loop, first and last equal — <c>src/A.cs, src/B.cs, src/A.cs</c> for a two-file cycle.
/// <see cref="Path"/> renders that loop as a path a message can print:
/// <c>src/A.cs → src/B.cs → src/A.cs</c>.
/// </para>
/// <para>
/// This type is immutable and value-semantic; two violations whose cycles are equal are equal.
/// </para>
/// </remarks>
public sealed record CycleViolation : Violation
{
    private readonly string[] _files;

    /// <summary>
    /// The ordered file identifiers of the cycle, closed: the first and last entries name the same
    /// file, so the list reads as a loop. Must not be <see langword="null"/>, must carry at least
    /// three files and must close; both the constructor and a <see langword="with"/> expression route
    /// through the same validation, so neither can introduce a bad value.
    /// </summary>
    public IReadOnlyList<string> Files
    {
        get => _files.ToArray();
        init => _files = Require(value);
    }

    /// <summary>
    /// The cycle as a readable path: the ordered files joined with an arrow, so a two-file cycle
    /// renders as <c>src/A.cs → src/B.cs → src/A.cs</c>. The message a report prints for this
    /// violation.
    /// </summary>
    public string Path => string.Join(" → ", _files);

    /// <summary>
    /// Creates a violation for a cycle.
    /// </summary>
    /// <param name="files">The ordered file identifiers of the cycle, closed; must not be <see langword="null"/>, must carry at least three files, and the last must equal the first.</param>
    /// <exception cref="ArgumentNullException"><paramref name="files"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="files"/> carries fewer than three files, contains a <see langword="null"/> or empty identifier, or does not close.</exception>
    public CycleViolation(IReadOnlyList<string> files)
        : base(ViolationKind.Rule)
    {
        _files = Require(files);
    }

    /// <summary>
    /// Two violations are equal when their cycles are equal.
    /// </summary>
    /// <param name="other">The other violation.</param>
    /// <returns><see langword="true"/> when the violations are equal.</returns>
    public bool Equals(CycleViolation? other) =>
        other is not null
        && base.Equals(other)
        && Files.SequenceEqual(other.Files);

    /// <summary>
    /// A hash code consistent with <see cref="Equals(CycleViolation?)"/>.
    /// </summary>
    /// <returns>A hash code over the cycle.</returns>
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(base.GetHashCode());
        foreach (string file in Files)
        {
            hash.Add(file);
        }

        return hash.ToHashCode();
    }

    private static string[] Require(IReadOnlyList<string> files)
    {
        ArgumentNullException.ThrowIfNull(files);
        if (files.Count < 3)
        {
            throw new ArgumentException("A cycle must carry at least three files.", nameof(Files));
        }

        string[] copy = files.ToArray();
        for (int index = 0; index < copy.Length; index++)
        {
            if (copy[index] is null || copy[index].Length == 0)
            {
                throw new ArgumentException("A cycle must not contain a null or empty file.", nameof(Files));
            }
        }

        if (!string.Equals(copy[0], copy[^1], StringComparison.Ordinal))
        {
            throw new ArgumentException("A cycle must close: the last file must equal the first.", nameof(Files));
        }

        return copy;
    }
}
