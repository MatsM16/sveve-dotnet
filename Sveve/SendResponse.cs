using System;
using System.Collections.Generic;
using System.Linq;

namespace Sveve;

/// <summary>
/// A response from a send SMS request.
/// </summary>
public sealed class SendResponse
{
    private readonly IReadOnlyDictionary<SmsRecipient, int>? _messageIds;
    private readonly IReadOnlyDictionary<SmsRecipient, string> _errors;
    private List<SendError>? _x;

    internal SendResponse(
        IReadOnlyDictionary<SmsRecipient, int>? messageIds, 
        IReadOnlyDictionary<SmsRecipient, string> errors,
        int sentCount, int smsUnitCost)
    {
        _messageIds = messageIds;
        _errors = errors;
        SentCount = sentCount;
        SmsUnitCost = smsUnitCost;
    }

    /// <summary>
    /// Number of sent messages.
    /// </summary>
    public int SentCount { get; }

    /// <summary>
    /// Cost of request in SMS units.
    /// </summary>
    public int SmsUnitCost { get; }

    /// <summary>
    /// Returns the message id of the SMS sent to <paramref name="phoneNumber"/>.
    /// </summary>
    /// <remarks>
    /// If the request contains any messages sent to groups, getting the message id is not supported and will throw <see cref="NotSupportedException"/>.
    /// </remarks>
    /// <param name="phoneNumber">Phone number to get message id for.</param>
    /// <exception cref="ArgumentException"><paramref name="phoneNumber"/> is not a single mobile phone number.</exception>
    /// <exception cref="NotSupportedException">The request contains messages sent groups.</exception>
    /// <exception cref="SmsNotSentException">The SMS was not sent for some known reason.</exception>
    public int MessageId(string phoneNumber)
    {
        var recipient = new SmsRecipient(phoneNumber);
        if (!recipient.IsPhoneNumber)
            throw new ArgumentException($"Parameter {nameof(phoneNumber)} must be a valid mobile phone number");

        if (_messageIds is null)
            throw new NotSupportedException("Getting message IDs of messages sent to group members are not supported.");

        if (_messageIds.TryGetValue(recipient, out var messageId))
            return messageId;

        if (_errors.TryGetValue(recipient, out var error))
            throw new SmsNotSentException($"SMS to {phoneNumber} was not sent. {error}");

        throw new SmsNotSentException($"SMS to {phoneNumber} was not sent. The number was not part of the request.");
    }

    /// <summary>
    /// All errors that prevented individual messages from being sent.
    /// </summary>
    public IReadOnlyList<SendError> Errors => _x ??= _errors.Select(x => new SendError(x.Key.ToString(), x.Value)).ToList();
}
