namespace ArchUnitSharp.Graph;

/// <summary>
/// A node of a <see cref="GraphSnapshot"/>: a distinct label the snapshot's scope collapses to,
/// together with the project files behind it. A node is either a file node — a label some project
/// files map to — or an external-module node, which carries no project files.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Label"/> is the node's report name: a file identifier when the query collapsed nothing,
/// a folder at the collapse depth, a collapse bucket's glob, or an external module's written name.
/// <see cref="Files"/> lists the identifiers of the project files that map to this node, so a renderer
/// can annotate or drill into a node; <see cref="External"/> is <see langword="true"/> exactly when the
/// node is an external module target, which is why its file list is empty.
/// </para>
/// <para>
/// The file list is copied on construction and copied again on every read, so a caller can never
/// corrupt a node through a reference it obtained from it. This type is immutable and value-semantic,
/// and safe for concurrent use.
/// </para>
/// </remarks>
public sealed record SnapshotNode
{
    private readonly string _label;
    private readonly string[] _files;

    /// <summary>
    /// The node's label. Must not be <see langword="null"/> or empty; both the constructor and a
    /// <see langword="with"/> expression route through the same validation, so neither can introduce
    /// a bad value.
    /// </summary>
    public string Label
    {
        get => _label;
        init => _label = RequireLabel(value);
    }

    /// <summary>
    /// The identifiers of the project files that map to this node, in the order they were supplied.
    /// Empty for an external-module node. Each access returns a fresh copy, so the returned list is
    /// always safe to hold or mutate.
    /// </summary>
    public IReadOnlyList<string> Files
    {
        get => _files.ToArray();
        init => _files = RequireFiles(value);
    }

    /// <summary>
    /// <see langword="true"/> when this node is an external module target — a name no project file
    /// maps to — and <see langword="false"/> when it is a label some project files map to.
    /// </summary>
    public bool External { get; init; }

    /// <summary>
    /// Creates a snapshot node.
    /// </summary>
    /// <param name="label">The node's label; must not be <see langword="null"/> or empty.</param>
    /// <param name="files">The identifiers of the files that map to this node; empty for an external-module node. Must not be <see langword="null"/>.</param>
    /// <param name="external"><see langword="true"/> when this node is an external module target.</param>
    /// <exception cref="ArgumentNullException"><paramref name="label"/> or <paramref name="files"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="label"/> is empty.</exception>
    public SnapshotNode(string label, IEnumerable<string> files, bool external)
    {
        _label = RequireLabel(label);
        _files = RequireFiles(files);
        External = external;
    }

    /// <summary>
    /// Two nodes are equal when their labels, external flags and file lists are equal.
    /// </summary>
    /// <param name="other">The other node.</param>
    /// <returns><see langword="true"/> when the nodes are equal.</returns>
    public bool Equals(SnapshotNode? other) =>
        other is not null
        && string.Equals(Label, other.Label, StringComparison.Ordinal)
        && External == other.External
        && Files.SequenceEqual(other.Files);

    /// <summary>
    /// A hash code consistent with <see cref="Equals(SnapshotNode?)"/>.
    /// </summary>
    /// <returns>A hash code over the label, external flag and file list.</returns>
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Label);
        hash.Add(External);
        foreach (string file in Files)
        {
            hash.Add(file);
        }

        return hash.ToHashCode();
    }

    private static string RequireLabel(string label) =>
        label is null
            ? throw new ArgumentNullException(nameof(Label))
            : label.Length == 0
                ? throw new ArgumentException("Label must not be empty.", nameof(Label))
                : label;

    private static string[] RequireFiles(IEnumerable<string> files)
    {
        ArgumentNullException.ThrowIfNull(files);
        return files.ToArray();
    }
}
