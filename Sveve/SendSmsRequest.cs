using System.Collections.Generic;

namespace Sveve;

/// <summary>
/// Defines a single sms.
/// </summary>
/// <param name="receiver">Phone number, name of group or comma separated list of both.</param>
/// <param name="text">Content of sms.</param>
public sealed class SendSmsRequest(string receiver, string text)
{
    /// <summary>
    /// Phone number receiving the sms.
    /// </summary>
    public string Receiver { get; } = receiver;

    /// <summary>
    /// Content of the sms.
    /// </summary>
    public string Text { get; } = text;

    /// <summary>
    /// Display name for sender of the sms.
    /// </summary>
    public string? Sender { get; set; }

    /// <summary>
    /// The receiver can reply to this message.
    /// </summary>
    public bool IsReplyAllowed { get; set; }

    /// <summary>
    /// This message is a reply to the message with the given id.
    /// </summary>
    public string? ReplyToMessageId { get; set; }

    /// <inheritdoc cref="Sveve.SendRepetition" />
    public SendRepetition Repeat { get; set; } = SendRepetition.Never;

    /// <summary>
    /// Your own reference. This will be included in delivery reports.
    /// </summary>
    public string? Reference { get; set; }

    /// <summary>
    /// If <see langword="true"/>, the message is not actually sent to the receiver.
    /// </summary>
    public bool IsTest { get; set; }

    internal void AddProperties(IDictionary<string, object> properties)
    {
        properties.Add("to", Receiver);
        properties.Add("msg", Text);

        if (Sender is not null)
            properties.Add("from", Sender);

        if (ReplyToMessageId is not null)
            properties.Add("reply_id", ReplyToMessageId);

        if (IsReplyAllowed)
            properties.Add("reply", true);

        if (Reference is not null)
            properties.Add("ref", Reference);

        Repeat?.AddProperties(properties);
    }
}
