namespace ArchUnitSharp.Common.Extraction;

/// <summary>
/// An <see cref="Error"/> caused by the API being used incorrectly, not by the library or the
/// environment: a syntax error in a pattern, an identifier that cannot be resolved, an option bag
/// carrying an impossible combination.
/// </summary>
/// <remarks>
/// <para>
/// A consumer catches this kind to distinguish "the API was used incorrectly" from
/// <see cref="TechnicalError"/>, which means the library or its environment failed. The message
/// describes the misuse; an optional <see cref="Exception.InnerException"/> carries the underlying
/// cause.
/// </para>
/// <para>
/// This type is sealed and safe for concurrent use.
/// </para>
/// </remarks>
public sealed class UserError : Error
{
    /// <summary>
    /// Creates a <see cref="UserError"/> with no message and no inner exception.
    /// </summary>
    public UserError() { }

    /// <summary>
    /// Creates a <see cref="UserError"/> with a message and no inner exception.
    /// </summary>
    /// <param name="message">The message describing the misuse; <see langword="null"/> means the default message for this exception type.</param>
    public UserError(string? message) : base(message) { }

    /// <summary>
    /// Creates a <see cref="UserError"/> with a message and an inner exception.
    /// </summary>
    /// <param name="message">The message describing the misuse; <see langword="null"/> means the default message for this exception type.</param>
    /// <param name="innerException">The exception that caused this failure; <see langword="null"/> when there is none.</param>
    public UserError(string? message, Exception? innerException) : base(message, innerException) { }
}
