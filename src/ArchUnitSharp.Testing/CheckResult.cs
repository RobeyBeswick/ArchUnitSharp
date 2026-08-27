namespace ArchUnitSharp.Testing;

/// <summary>
/// The shaped outcome of one rule check: whether the rule passed and the report message that goes with
/// that verdict. Produced by <see cref="ResultFactory.Create"/> from the <see cref="Common.Extraction.Violation"/>
/// list a check returns — an empty list is a pass, any violations a fail.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Passed"/> is the pass flag a framework binding or command-line report branches on;
/// <see cref="Message"/> is the prose that report prints. The two travel together so a consumer has one
/// value to carry, never a separately-maintained flag and string. This type is immutable and
/// value-semantic: two results with the same verdict and message are equal, and sharing one across
/// concurrent reports is safe.
/// </para>
/// </remarks>
/// <param name="Passed">
/// <see langword="true"/> when the rule passed — the violation list it was shaped from was empty —
/// <see langword="false"/> otherwise.
/// </param>
/// <param name="Message">The report message: the rule's violations rendered by <see cref="ViolationFactory"/>, or the pass line when the rule passed. Must not be <see langword="null"/>.</param>
public sealed record CheckResult(bool Passed, string Message);
