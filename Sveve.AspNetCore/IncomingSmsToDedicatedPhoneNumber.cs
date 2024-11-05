namespace Sveve.AspNetCore;

/// <summary>
/// An incoming message that was sent to a dedicated phone number.
/// </summary>
/// <param name="DedicatedPhoneNumber">The dedicated phone number configured in Sveve.</param>
/// <param name="SenderPhoneNumber">The senders phone number.</param>
/// <param name="Message">The content of the incoming message.</param>
/// <param name="SenderName">The senders full name or <see langword="null"/>.</param>
/// <param name="SenderAddress">The senders full address or <see langword="null"/>.</param>
public sealed record IncomingSmsToDedicatedPhoneNumber(
    string DedicatedPhoneNumber,
    string SenderPhoneNumber,
    string Message,
    string? SenderName,
    string? SenderAddress);
