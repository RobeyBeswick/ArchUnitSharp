namespace ArchUnitSharp.Common.Extraction;

/// <summary>
/// A violation produced by the empty-test guard: the rule being checked matched nothing, which is a
/// failure rather than a pass, unless the consumer opted out via an "allow empty tests" option.
/// </summary>
/// <remarks>
/// <para>
/// The only datum an empty rule can offer is <em>which rule</em> was empty, carried as
/// <see cref="RuleDescription"/> — the rule text a consumer would show in a report. There is no
/// offending edge or node to record because nothing was matched.
/// </para>
/// <para>
/// This type is immutable and value-semantic; two violations with the same rule description are
/// equal.
/// </para>
/// </remarks>
public sealed record EmptyTestViolation : Violation
{
    private readonly string _ruleDescription;

    /// <summary>
    /// The rule that matched nothing, in the form a consumer would show in a report. Must not be
    /// <see langword="null"/> or empty; both the constructor and a <see langword="with"/> expression
    /// route through the same validation, so neither can introduce a bad value.
    /// </summary>
    public string RuleDescription
    {
        get => _ruleDescription;
        init => _ruleDescription = Require(value);
    }

    /// <summary>
    /// Creates an empty-test violation for the rule that matched nothing.
    /// </summary>
    /// <param name="ruleDescription">The rule that matched nothing; must not be <see langword="null"/> or empty.</param>
    /// <exception cref="ArgumentNullException"><paramref name="ruleDescription"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="ruleDescription"/> is empty.</exception>
    public EmptyTestViolation(string ruleDescription)
        : base(ViolationKind.EmptyTest)
    {
        _ruleDescription = Require(ruleDescription);
    }

    private static string Require(string ruleDescription) =>
        ruleDescription is null
            ? throw new ArgumentNullException(nameof(RuleDescription))
            : ruleDescription.Length == 0
                ? throw new ArgumentException("Rule description must not be empty.", nameof(RuleDescription))
                : ruleDescription;
}
