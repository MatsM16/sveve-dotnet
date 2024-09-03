using System;

namespace Sveve;

/// <summary>
/// Response status for a single sent sms.
/// </summary>
public sealed class SendSmsResult
{
    private readonly int? _messageId;
    private readonly bool _isTest;

    private SendSmsResult(string phoneNumber, int? messageId, string? error, bool isTest)
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
    /// Throws <see cref="InvalidOperationException"/> if <see cref="IsSuccess"/> is <see langword="false"/>.
    /// </remarks>
    /// <exception cref="InvalidOperationException">The response is not successful.</exception>
    public int MessageId => _messageId ?? throw new InvalidOperationException($"Can only read {nameof(MessageId)} from successfully sent messages");

    /// <summary>
    /// Error explaining why message was not sent.
    /// </summary>
    public string? Error { get; }

    /// <summary>
    /// If <see langword="true"/>, this is a successful response and <see cref="MessageId"/> is safe to read.
    /// </summary>
    public bool IsSuccess => _messageId is not null;

    /// <summary>
    /// If <see langword="true"/>, this is a response to a test message.
    /// </summary>
    public bool IsTest => _isTest;

    /// <summary>
    /// Creates a new successful message response.
    /// </summary>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static SendSmsResult Ok(string phoneNumber, int messageId, bool isTest = false)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            throw new ArgumentNullException(nameof(phoneNumber));

        if (messageId <= 0)
            throw new ArgumentOutOfRangeException(nameof(messageId));

        return new SendSmsResult(phoneNumber, messageId, null, isTest);
    }

    /// <summary>
    /// Creates a new failed message response.
    /// </summary>
    /// <exception cref="ArgumentNullException"></exception>
    public static SendSmsResult Failed(string phoneNumber, string? error, bool isTest = false)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            throw new ArgumentNullException(nameof(phoneNumber));

        if (string.IsNullOrWhiteSpace(error))
            throw new ArgumentNullException(nameof(error));

        return new SendSmsResult(phoneNumber, null, error, isTest);
    }
}