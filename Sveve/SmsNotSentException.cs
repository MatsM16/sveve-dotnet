using System;

namespace Sveve;

/// <summary>
/// Exception thrown when a sms failed to send.
/// </summary>
/// <param name="message">The reason for failure.</param>
/// <param name="innerException">The inner exception that caused the failure.</param>
public sealed class SmsNotSentException(string? message, Exception? innerException = null) : Exception(message, innerException);
