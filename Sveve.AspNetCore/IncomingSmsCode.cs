namespace Sveve.AspNetCore;

/// <summary>
/// An incoming message that was received by code word.
/// </summary>
/// <param name="CodeWord">The code word configured in Sveve.</param>
/// <param name="ReceiverPhoneNumber">The incoming SMS was sent to this phone number.</param>
/// <param name="SenderPhoneNumber">The senders phone number.</param>
/// <param name="Message">The content of the incoming message.</param>
/// <param name="SenderName">The senders full name or <see langword="null"/>.</param>
/// <param name="SenderAddress">The senders full address or <see langword="null"/>.</param>
public sealed record IncomingSmsCode(
    string CodeWord,
    string ReceiverPhoneNumber,
    string SenderPhoneNumber,
    string Message,
    string? SenderName,
    string? SenderAddress);
