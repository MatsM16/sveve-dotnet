using System.Diagnostics;

namespace Sveve;

/// <summary>
/// An error that occurred when trying to send a sms to <see cref="PhoneNumber"/>.
/// </summary>
/// <param name="phoneNumber">The phone number that did not receive a SMS.</param>
/// <param name="reason">The reason why the SMS was not sent.</param>
[DebuggerDisplay($"{{{nameof(PhoneNumber)},nq}}: {{{nameof(Reason)},nq}}")]
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
