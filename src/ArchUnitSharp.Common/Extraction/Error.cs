namespace ArchUnitSharp.Common.Extraction;

/// <summary>
/// The base type of every failure that is not a rule outcome. A failing rule is a
/// <see cref="Violation"/> in a returned list; everything else — a project that cannot be located,
/// an environment the library cannot work in, or an API used incorrectly — is an
/// <see cref="Error"/>.
/// </summary>
/// <remarks>
/// <para>
/// The two concrete members split the family by who is at fault: the library or the environment
/// (<see cref="TechnicalError"/>), or the caller (<see cref="UserError"/>). Both are sealed, so the
/// family is closed by convention and a consumer can rely on these being the only two kinds.
/// </para>
/// <para>
/// This type derives from <see cref="Exception"/>, so every non-rule failure is catchable in the
/// usual way and a single <c>catch (Error)</c> handles them all. This type is safe for concurrent
/// use.
/// </para>
/// </remarks>
public abstract class Error : Exception
{
    /// <summary>
    /// Creates an <see cref="Error"/> with no message and no inner exception. Only derived types can
    /// call this.
    /// </summary>
    protected Error() { }

    /// <summary>
    /// Creates an <see cref="Error"/> with a message and no inner exception. Only derived types can
    /// call this.
    /// </summary>
    /// <param name="message">The message describing the failure; <see langword="null"/> means the default message for this exception type.</param>
    protected Error(string? message) : base(message) { }

    /// <summary>
    /// Creates an <see cref="Error"/> with a message and an inner exception. Only derived types can
    /// call this.
    /// </summary>
    /// <param name="message">The message describing the failure; <see langword="null"/> means the default message for this exception type.</param>
    /// <param name="innerException">The exception that caused this failure; <see langword="null"/> when there is none.</param>
    protected Error(string? message, Exception? innerException) : base(message, innerException) { }
}
