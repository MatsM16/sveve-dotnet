using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Sveve.Sending;

/// <summary>
/// Represents a recipient of an SMS message.
/// </summary>
[DebuggerDisplay("{_recipient}")]
internal sealed class SmsRecipient
{
    /// <summary>
    /// The character used to separate multiple recipients.
    /// </summary>
    public const char RecipientSeparator = ',';

    private readonly string _recipient;

    /// <summary>
    /// Creates a new SMS recipient.
    /// </summary>
    /// <param name="recipient">The recipient of the SMS.</param>
    /// <exception cref="ArgumentNullException">The recipient is null or empty.</exception>
    /// <exception cref="ArgumentException">The recipient contains multiple recipients.</exception>
    public SmsRecipient(string recipient)
    {
        if (string.IsNullOrWhiteSpace(recipient))
            throw new ArgumentNullException("Recipient cannot be null or empty", nameof(recipient));

        if (recipient.IndexOf(RecipientSeparator) > -1)
            throw new ArgumentException($"Recipient cannot contain multiple recipients. Use {nameof(SmsRecipient)}.{nameof(ToString)}", nameof(recipient));

        IsPhoneNumber = IsPhoneNumberInternal(recipient);
        _recipient = IsPhoneNumber ? NormalizedPhoneNumber(recipient) : NormalizedGroupName(recipient);
    }

    /// <summary>
    /// If <see langword="true"/>, the recipient is a phone number.
    /// </summary>
    public bool IsPhoneNumber { get; }

    /// <summary>
    /// Returns the recipient as a string.
    /// </summary>
    public override string ToString() => _recipient;

    /// <summary>
    /// Returns <see langword="true"/> if <paramref name="other"/> is equivalent to this recipient. 
    /// </summary>
    public override bool Equals(object? other) => other is SmsRecipient otherRecipient && Equals(otherRecipient);

    /// <summary>
    /// Returns <see langword="true"/> if <paramref name="other"/> is equivalent to this recipient. 
    /// </summary>
    public bool Equals(SmsRecipient? other) => _recipient == other?._recipient;

    /// <inheritdoc/>
    public override int GetHashCode() => _recipient.GetHashCode();

    /// <summary>
    /// Parses a comma-separated list of recipients.
    /// </summary>
    public static List<SmsRecipient> Multiple(string recipients) => recipients?.Split(RecipientSeparator).Select(r => new SmsRecipient(r)).ToList() ?? throw new ArgumentNullException(nameof(recipients));

    internal static bool IsPhoneNumberInternal(string value) => !string.IsNullOrWhiteSpace(value) && value.All(c => c is '+' or ' ' || char.IsDigit(c));

    private static string NormalizedPhoneNumber(string value)
    {
        value = value.Replace(" ", "").Replace("00", "+");

        if (value.StartsWith("+47"))
            value = value.Substring(3);

        return value;
    }

    private static string NormalizedGroupName(string value)
    {
        return value.Trim();
    }

    /// <summary>
    /// Compares two recipients for equality.
    /// </summary>
    public static bool operator ==(SmsRecipient? left, SmsRecipient? right) => left?.Equals(right) ?? right is null;

    /// <summary>
    /// Compares two recipients for inequality.
    /// </summary>
    public static bool operator !=(SmsRecipient? left, SmsRecipient? right) => !(left == right);
}
