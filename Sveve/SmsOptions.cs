using System;

namespace Sveve;

/// <summary>
/// Options for sending an sms.
/// </summary>
public class SmsOptions
{
    /// <summary>
    /// Display name for sender of the sms.
    /// </summary>
    public string? Sender { get; set; }

    /// <summary>
    /// The receiver can reply to this message.
    /// </summary>
    /// <remarks>
    /// If <see langword="true"/>, <see cref="Sender"/> is ignored and Sveve generates a random 14-digit phone number.
    /// </remarks>
    public bool IsReplyAllowed { get; set; }

    /// <summary>
    /// ID of a message sent with <see cref="IsReplyAllowed"/>=<see langword="true"/>.
    /// </summary>
    /// <remarks>
    /// This keeps the message in the same thread as the referenced message on the receivers phone.
    /// </remarks>
    public string? ConversationMessageId { get; set; }

    /// <inheritdoc cref="SendRepetition" />
    public SendRepetition? Repeat { get; set; } = SendRepetition.Never;

    /// <summary>
    /// The message will be schedule for sending at the given date and time.
    /// </summary>
    public DateTimeOffset? ScheduledSendTime { get; set; }

    /// <summary>
    /// Your own reference. This will be included in delivery reports.
    /// </summary>
    public string? Reference { get; set; }

    /// <summary>
    /// If <see langword="true"/>, the message is not actually sent to the receiver.
    /// </summary>
    public bool IsTest { get; set; }

    /// <summary>
    /// If <see langword="true"/>, the client will look up members and include successful <see cref="SmsResult"/>s for messages sent to groups.
    /// </summary>
    /// <remarks>
    /// When enabled, one additional API-request is made for each group in a receiver list. <br/>
    /// Disabled by default to avoid unnecessary API-requests. <br/>
    /// Only enable when you intend to use the <see cref="SmsResult"/>s for the group members.
    /// </remarks>
    public bool LookupGroupMembers { get; set; }
}
