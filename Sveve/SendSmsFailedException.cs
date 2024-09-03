using System;

namespace Sveve;

public sealed class SendSmsFailedException(string? message, Exception? innerException = null) : Exception(message, innerException);
