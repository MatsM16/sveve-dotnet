namespace Sveve.Api;

/// <summary>
/// An incoming message that was received by code word.
/// </summary>
/// <param name="Code">The code word configured in Sveve.</param>
/// <param name="SenderPhoneNumber">The senders phone number.</param>
/// <param name="Message">The content of the incoming message.</param>
/// <param name="SenderName">The senders full name or <see langword="null"/>.</param>
/// <param name="SenderAddress">The senders full address or <see langword="null"/>.</param>
public sealed record CodeSms(
    string Code,
    string SenderPhoneNumber,
    string Message,
    string? SenderName,
    string? SenderAddress);
