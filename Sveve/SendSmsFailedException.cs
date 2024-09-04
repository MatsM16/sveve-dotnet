using System;

namespace Sveve;

/// <summary>
/// Exception thrown when sending an sms failed.
/// </summary>
/// <param name="message">The reason for failure.</param>
/// <param name="innerException">The inner exception that caused the failure.</param>
public sealed class SendSmsFailedException(string? message, Exception? innerException = null) : Exception(message, innerException);
