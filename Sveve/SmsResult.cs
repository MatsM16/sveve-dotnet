using System;

namespace Sveve;

/// <summary>
/// Response status for a single sent sms.
/// </summary>
public sealed class SmsResult
{
    private readonly int? _messageId;
    private readonly bool _isTest;

    private SmsResult(string phoneNumber, int? messageId, string? error, bool isTest)
    {
        ReceiverPhoneNumber = phoneNumber;
        _messageId = messageId;
        _isTest = isTest;
        Error = error;
    }

    /// <summary>
    /// Phone number the sms was sent to.
    /// </summary>
    public string ReceiverPhoneNumber { get; }

    /// <summary>
    /// Sveve ID for the successfully sent message.
    /// </summary>
    /// <remarks>
    /// Throws <see cref="SmsNotSentException"/> if <see cref="IsSentSuccessfully"/> is <see langword="false"/>.
    /// </remarks>
    /// <exception cref="SmsNotSentException">The response is not successful.</exception>
    public int MessageId => _messageId ?? throw new SmsNotSentException(Error ?? "Message was not sent successfully.");

    /// <summary>
    /// Error explaining why message was not sent.
    /// </summary>
    public string? Error { get; }

    /// <summary>
    /// If <see langword="true"/>, the message was sent successfully and <see cref="MessageId"/> is available.
    /// </summary>
    /// <remarks>
    /// Trying to read <see cref="MessageId"/> when this is <see langword="false"/> will throw an exception.
    /// </remarks>
    public bool IsSentSuccessfully => _messageId is not null;

    /// <summary>
    /// If <see langword="true"/>, this is the result of a test message.
    /// </summary>
    /// <remarks>
    /// Test messages are not actually sent, even if <see cref="IsSentSuccessfully"/> is <see langword="true"/>.
    /// </remarks>
    public bool IsTest => _isTest;

    /// <summary>
    /// Creates a new successful message result.
    /// </summary>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static SmsResult Ok(string phoneNumber, int messageId, bool isTest = false)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            throw new ArgumentNullException(nameof(phoneNumber));

        if (SmsReceiver.IsSinglePhoneNumber(phoneNumber) is false)
            throw new ArgumentException($"{nameof(SmsResult)} can only be created for exactly one phone number.");

        if (messageId <= 0)
            throw new ArgumentOutOfRangeException(nameof(messageId));

        return new SmsResult(phoneNumber, messageId, null, isTest);
    }

    /// <summary>
    /// Creates a new failed message response.
    /// </summary>
    /// <exception cref="ArgumentNullException"></exception>
    public static SmsResult Failed(string phoneNumber, string? error, bool isTest = false)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            throw new ArgumentNullException(nameof(phoneNumber));

        if (SmsReceiver.IsSinglePhoneNumber(phoneNumber) is false)
            throw new ArgumentException($"{nameof(SmsResult)} can only be created for exactly one phone number.");

        return new SmsResult(phoneNumber, null, error, isTest);
    }
}