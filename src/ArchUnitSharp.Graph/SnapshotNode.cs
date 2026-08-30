namespace ArchUnitSharp.Graph;

/// <summary>
/// A node of a <see cref="GraphSnapshot"/>: a distinct label the snapshot's scope collapses to,
/// together with the project files behind it.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Label"/> is the node's report name: a file identifier when the query collapsed nothing,
/// a folder at the collapse depth, or a collapse bucket's glob. <see cref="Files"/> lists the
/// identifiers of the project files that map to this node, so a renderer can annotate or drill into
/// a node; a collapsed node — a folder or bucket — carries every file behind its label.
/// </para>
/// <para>
/// The file list is copied on construction and copied again on every read, so a caller can never
/// corrupt a node through a reference it obtained from it. This type is immutable and value-semantic:
/// two nodes with the same label and the same files are equal. It is safe for concurrent use.
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
    /// The identifiers of the project files that map to this node, in sorted order. Never empty; each
    /// access returns a fresh copy, so the returned list is always safe to hold or mutate.
    /// </summary>
    public IReadOnlyList<string> Files
    {
        get => _files.ToArray();
        init => _files = RequireFiles(value);
    }

    /// <summary>
    /// Creates a snapshot node.
    /// </summary>
    /// <param name="label">The node's label; must not be <see langword="null"/> or empty.</param>
    /// <param name="files">The identifiers of the files that map to this node; must not be <see langword="null"/> or empty.</param>
    /// <exception cref="ArgumentNullException"><paramref name="label"/> or <paramref name="files"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="label"/> is empty, or <paramref name="files"/> is empty.</exception>
    public SnapshotNode(string label, IEnumerable<string> files)
    {
        _label = RequireLabel(label);
        _files = RequireFiles(files);
    }

    /// <summary>
    /// Two nodes are equal when their labels and file lists are equal.
    /// </summary>
    /// <param name="other">The other node.</param>
    /// <returns><see langword="true"/> when the nodes are equal.</returns>
    public bool Equals(SnapshotNode? other) =>
        other is not null
        && string.Equals(Label, other.Label, StringComparison.Ordinal)
        && Files.SequenceEqual(other.Files);

    /// <summary>
    /// A hash code consistent with <see cref="Equals(SnapshotNode?)"/>.
    /// </summary>
    /// <returns>A hash code over the label and file list.</returns>
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Label);
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
        string[] copy = files.ToArray();
        if (copy.Length == 0)
        {
            throw new ArgumentException("A snapshot node must carry at least one file.", nameof(Files));
        }

        return copy.OrderBy(static file => file, StringComparer.Ordinal).ToArray();
    }
}
