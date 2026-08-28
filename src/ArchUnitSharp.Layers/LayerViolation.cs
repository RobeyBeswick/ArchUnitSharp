namespace ArchUnitSharp.Layers;

using ArchUnitSharp.Common.Extraction;

/// <summary>
/// A violation produced by a layers rule predicate — <c>may not depend on layers(...)</c> or
/// <c>may only depend on layers(...)</c>: a dependency the rule forbids. Carries the offending
/// dependency as the importing file and the file it imports, together with the subject layer whose
/// rule was violated and the target layer the imported file belongs to, and nothing else.
/// </summary>
/// <remarks>
/// <para>
/// A layers predicate reports one of these per offending dependency — a subject-layer file that
/// depends on several forbidden target-layer files yields several violations — with
/// <see cref="Source"/> the importing file (a file of the rule's subject layer),
/// <see cref="Target"/> the imported file (a file of some other declared layer),
/// <see cref="SubjectLayer"/> the layer the rule asserts over and <see cref="TargetLayer"/> the
/// declared layer that makes the dependency forbidden. The meaning is supplied by the rule that
/// produced it. It carries <see cref="ViolationKind.Rule"/>, the same kind the files module's
/// predicate violations carry.
/// </para>
/// <para>
/// This type is immutable and value-semantic; two violations with the same four values are equal.
/// </para>
/// </remarks>
public sealed record LayerViolation : Violation
{
    private readonly string _subjectLayer;
    private readonly string _source;
    private readonly string _target;
    private readonly string _targetLayer;

    /// <summary>
    /// The layer the rule asserts over. Must not be <see langword="null"/> or empty; both the
    /// constructor and a <see langword="with"/> expression route through the same validation, so
    /// neither can introduce a bad value.
    /// </summary>
    public string SubjectLayer
    {
        get => _subjectLayer;
        init => _subjectLayer = Require(value, nameof(SubjectLayer));
    }

    /// <summary>
    /// The file of the subject layer that imports. Must not be <see langword="null"/> or empty; both
    /// the constructor and a <see langword="with"/> expression route through the same validation, so
    /// neither can introduce a bad value.
    /// </summary>
    public string Source
    {
        get => _source;
        init => _source = Require(value, nameof(Source));
    }

    /// <summary>
    /// The imported file. Must not be <see langword="null"/> or empty; both the constructor and a
    /// <see langword="with"/> expression route through the same validation, so neither can introduce
    /// a bad value.
    /// </summary>
    public string Target
    {
        get => _target;
        init => _target = Require(value, nameof(Target));
    }

    /// <summary>
    /// The declared layer the imported file belongs to, which makes the dependency forbidden. Must not
    /// be <see langword="null"/> or empty; both the constructor and a <see langword="with"/> expression
    /// route through the same validation, so neither can introduce a bad value.
    /// </summary>
    public string TargetLayer
    {
        get => _targetLayer;
        init => _targetLayer = Require(value, nameof(TargetLayer));
    }

    /// <summary>
    /// Creates a violation for a forbidden dependency between two layers.
    /// </summary>
    /// <param name="subjectLayer">The layer the rule asserts over; must not be <see langword="null"/> or empty.</param>
    /// <param name="source">The importing file of the subject layer; must not be <see langword="null"/> or empty.</param>
    /// <param name="target">The imported file; must not be <see langword="null"/> or empty.</param>
    /// <param name="targetLayer">The layer the imported file belongs to; must not be <see langword="null"/> or empty.</param>
    /// <exception cref="ArgumentNullException">Any argument is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Any argument is empty.</exception>
    public LayerViolation(string subjectLayer, string source, string target, string targetLayer)
        : base(ViolationKind.Rule)
    {
        _subjectLayer = Require(subjectLayer, nameof(SubjectLayer));
        _source = Require(source, nameof(Source));
        _target = Require(target, nameof(Target));
        _targetLayer = Require(targetLayer, nameof(TargetLayer));
    }

    private static string Require(string value, string field) =>
        value is null
            ? throw new ArgumentNullException(field)
            : value.Length == 0
                ? throw new ArgumentException($"{field} must not be empty.", field)
                : value;
}
