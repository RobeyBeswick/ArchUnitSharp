namespace ArchUnitSharp.Files;

/// <summary>
/// The per-file view an <c>adhere to</c> rule hands its custom predicate: the file's identity as the
/// graph knows it and the content the extraction pipeline read. A predicate receives one of these per
/// selected file and returns whether the file satisfies the rule.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Path"/> is the file's project-relative identifier — the same value that is a graph node
/// and that the match targets in the kernel are designed to consume. <see cref="NameWithoutExtension"/>,
/// <see cref="Extension"/> and <see cref="Directory"/> are derived from it the same way the kernel
/// derives its match targets, so a <c>src/Models/Car.cs</c> file carries the name <c>Car</c>, the
/// extension <c>.cs</c> and the directory <c>src/Models</c>. A root-level file carries an empty
/// directory and a file with no extension carries an empty extension.
/// </para>
/// <para>
/// <see cref="SourceText"/> is the file's full text as read from disk, and
/// <see cref="NonBlankLineCount"/> counts its non-blank lines — a line with only whitespace is blank.
/// A file is passed to a predicate only when its source is available; a selection built without
/// sources raises a <see cref="Common.Extraction.UserError"/> instead of fabricating empty text.
/// </para>
/// <para>
/// This type is immutable and value-semantic; two details with the same six values are equal.
/// </para>
/// </remarks>
public sealed record FileDetail
{
    private readonly string _path;
    private readonly string _nameWithoutExtension;
    private readonly string _extension;
    private readonly string _directory;
    private readonly string _sourceText;
    private readonly int _nonBlankLineCount;

    /// <summary>
    /// The file's project-relative path, the identifier the graph knows the file by. Must not be
    /// <see langword="null"/> or empty; both the constructor and a <see langword="with"/> expression
    /// route through the same validation, so neither can introduce a bad value.
    /// </summary>
    public string Path
    {
        get => _path;
        init => _path = RequireNonEmpty(value, nameof(Path));
    }

    /// <summary>
    /// The file's name without its extension, so a file at <c>src/Models/Car.cs</c> has the name
    /// <c>Car</c>. Must not be <see langword="null"/> or empty; both the constructor and a
    /// <see langword="with"/> expression route through the same validation, so neither can introduce
    /// a bad value.
    /// </summary>
    public string NameWithoutExtension
    {
        get => _nameWithoutExtension;
        init => _nameWithoutExtension = RequireNonEmpty(value, nameof(NameWithoutExtension));
    }

    /// <summary>
    /// The file's extension including its dot, so a file at <c>src/Models/Car.cs</c> has the
    /// extension <c>.cs</c>. A file with no extension carries the empty string. Must not be
    /// <see langword="null"/>; both the constructor and a <see langword="with"/> expression route
    /// through the same validation, so neither can introduce a bad value.
    /// </summary>
    public string Extension
    {
        get => _extension;
        init => _extension = RequireNonNull(value, nameof(Extension));
    }

    /// <summary>
    /// The directory the file sits in, so a file at <c>src/Models/Car.cs</c> is in the directory
    /// <c>src/Models</c>. A root-level file carries the empty string. Must not be
    /// <see langword="null"/>; both the constructor and a <see langword="with"/> expression route
    /// through the same validation, so neither can introduce a bad value.
    /// </summary>
    public string Directory
    {
        get => _directory;
        init => _directory = RequireNonNull(value, nameof(Directory));
    }

    /// <summary>
    /// The file's full source text as read from disk. An empty file carries the empty string. Must
    /// not be <see langword="null"/>; both the constructor and a <see langword="with"/> expression
    /// route through the same validation, so neither can introduce a bad value.
    /// </summary>
    public string SourceText
    {
        get => _sourceText;
        init => _sourceText = RequireNonNull(value, nameof(SourceText));
    }

    /// <summary>
    /// The number of non-blank lines in <see cref="SourceText"/>: a line whose content is only
    /// whitespace does not count. Must not be negative; both the constructor and a
    /// <see langword="with"/> expression route through the same validation, so neither can introduce
    /// a bad value.
    /// </summary>
    public int NonBlankLineCount
    {
        get => _nonBlankLineCount;
        init => _nonBlankLineCount = RequireNonNegative(value, nameof(NonBlankLineCount));
    }

    /// <summary>
    /// Creates a file's detail.
    /// </summary>
    /// <param name="path">The file's project-relative path; must not be <see langword="null"/> or empty.</param>
    /// <param name="nameWithoutExtension">The file's name without its extension; must not be <see langword="null"/> or empty.</param>
    /// <param name="extension">The file's extension with its dot, or the empty string when the file has none.</param>
    /// <param name="directory">The directory the file sits in, or the empty string for a root-level file.</param>
    /// <param name="sourceText">The file's full source text.</param>
    /// <param name="nonBlankLineCount">The file's non-blank line count; must not be negative.</param>
    /// <exception cref="ArgumentNullException"><paramref name="path"/>, <paramref name="nameWithoutExtension"/>, <paramref name="extension"/>, <paramref name="directory"/> or <paramref name="sourceText"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="path"/> or <paramref name="nameWithoutExtension"/> is empty, or <paramref name="nonBlankLineCount"/> is negative.</exception>
    public FileDetail(
        string path,
        string nameWithoutExtension,
        string extension,
        string directory,
        string sourceText,
        int nonBlankLineCount)
    {
        _path = RequireNonEmpty(path, nameof(Path));
        _nameWithoutExtension = RequireNonEmpty(nameWithoutExtension, nameof(NameWithoutExtension));
        _extension = RequireNonNull(extension, nameof(Extension));
        _directory = RequireNonNull(directory, nameof(Directory));
        _sourceText = RequireNonNull(sourceText, nameof(SourceText));
        _nonBlankLineCount = RequireNonNegative(nonBlankLineCount, nameof(NonBlankLineCount));
    }

    private static string RequireNonEmpty(string value, string propertyName) =>
        value is null
            ? throw new ArgumentNullException(propertyName)
            : value.Length == 0
                ? throw new ArgumentException($"{propertyName} must not be empty.", propertyName)
                : value;

    private static string RequireNonNull(string value, string propertyName) =>
        value is null ? throw new ArgumentNullException(propertyName) : value;

    private static int RequireNonNegative(int value, string propertyName) =>
        value < 0
            ? throw new ArgumentException($"{propertyName} must not be negative.", propertyName)
            : value;
}
