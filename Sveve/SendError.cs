namespace Sveve;

/// <summary>
/// Represents a SMS that was not sent.
/// </summary>
/// <param name="phoneNumber">The phone number that did not receive a SMS.</param>
/// <param name="reason">The reason why the SMS was not sent.</param>
public sealed class SendError(string phoneNumber, string reason)
{
    /// <summary>
    /// SMS to this phone number was not sent.
    /// </summary>
    public string PhoneNumber { get; } = phoneNumber;

    /// <summary>
    /// Reason why SMS was not sent.
    /// </summary>
    public string Reason { get; } = reason;
}
