namespace ArchUnitSharp.Common.Extraction;

/// <summary>
/// An <see cref="Error"/> caused by the library itself or the environment it runs in, not by the
/// caller: a project that cannot be located, a graph that cannot be read, an environment the library
/// cannot work in.
/// </summary>
/// <remarks>
/// <para>
/// A consumer catches this kind to distinguish "the library or its environment failed" from
/// <see cref="UserError"/>, which means the API was used incorrectly. The message describes the
/// failure; an optional <see cref="Exception.InnerException"/> carries the underlying cause.
/// </para>
/// <para>
/// This type is sealed and safe for concurrent use.
/// </para>
/// </remarks>
public sealed class TechnicalError : Error
{
    /// <summary>
    /// Creates a <see cref="TechnicalError"/> with no message and no inner exception.
    /// </summary>
    public TechnicalError() { }

    /// <summary>
    /// Creates a <see cref="TechnicalError"/> with a message and no inner exception.
    /// </summary>
    /// <param name="message">The message describing the failure; <see langword="null"/> means the default message for this exception type.</param>
    public TechnicalError(string? message) : base(message) { }

    /// <summary>
    /// Creates a <see cref="TechnicalError"/> with a message and an inner exception.
    /// </summary>
    /// <param name="message">The message describing the failure; <see langword="null"/> means the default message for this exception type.</param>
    /// <param name="innerException">The exception that caused this failure; <see langword="null"/> when there is none.</param>
    public TechnicalError(string? message, Exception? innerException) : base(message, innerException) { }
}
