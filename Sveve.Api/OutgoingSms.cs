namespace Sveve.Api;

/// <summary>
/// A SMS sent by you.
/// </summary>
/// <param name="ReceiverPhoneNumber">The receivers phone number.</param>
/// <param name="MessageId">The ID of the sent message.</param>
/// <param name="Reference">The reference on the sent message.</param>
public sealed record OutgoingSms(string ReceiverPhoneNumber, int MessageId, string? Reference);
