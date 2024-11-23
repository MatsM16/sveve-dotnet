using System;
using System.Diagnostics;

namespace Sveve;

/// <summary>
/// A single sms.
/// </summary>
/// <param name="to">Comma-separated list of phone numbers and group names that will receive this sms.</param>
/// <param name="text">The text in the sms.</param>
[DebuggerDisplay($"{{{nameof(To)},nq}}: {{{nameof(Text)},nq}}")]
public sealed class Sms(string to, string text)
{
    /// <summary>
    /// A comma-separated list of receivers for this sms.
    /// </summary>
    /// <remarks>
    /// A receiver can be a phone number or a group name.
    /// </remarks>
    public string To { get; } = !string.IsNullOrWhiteSpace(to) ? to : throw new ArgumentNullException(nameof(to));

    /// <summary>
    /// The actual text message.
    /// </summary>
    public string Text { get; } = !string.IsNullOrWhiteSpace(text) ? text : throw new ArgumentNullException(nameof(text));

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
    public bool ReplyAllowed { get; set; }

    /// <summary>
    /// ID of an incoming message or an outgoing message sent with <see cref="ReplyAllowed"/>=<see langword="true"/>.
    /// </summary>
    /// <remarks>
    /// This keeps the message in the same thread as the referenced message on the receivers phone.
    /// </remarks>
    public int? ReplyTo { get; set; }

    /// <inheritdoc cref="SendRepeat" />
    public SendRepeat? Repeat { get; set; } = SendRepeat.Never;

    /// <summary>
    /// The message will be schedule for sending at the given date and time.
    /// </summary>
    public DateTimeOffset? SendTime { get; set; }

    /// <summary>
    /// Your own reference. This will be included in delivery reports.
    /// </summary>
    public string? Reference { get; set; }

    /// <summary>
    /// If <see langword="true"/>, the message is not actually sent to the receiver.
    /// </summary>
    public bool Test { get; set; }
}
