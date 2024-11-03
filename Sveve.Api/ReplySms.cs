namespace Sveve.Api;

/// <summary>
/// An incoming message that was a reply to an outgoing message.
/// </summary>
/// <param name="SenderPhoneNumber">The senders phone number.</param>
/// <param name="MessageId">The ID of the incoming message. Use to reply.</param>
/// <param name="Message">The content of the incoming message.</param>
public sealed record ReplySms(
    string SenderPhoneNumber,
    int MessageId,
    string Message);
