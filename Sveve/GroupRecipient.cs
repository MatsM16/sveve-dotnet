using System;

namespace Sveve;

/// <summary>
/// A recipient on a group.
/// </summary>
public sealed class GroupRecipient
{
    internal GroupRecipient(string? name, string phoneNumber)
    {
        Name = name;
        PhoneNumber = phoneNumber;
    }

    /// <summary>
    /// Name of the recipient.
    /// </summary>
    public string? Name { get; }

    /// <summary>
    /// Phone number to the recipient.
    /// </summary>
    public string PhoneNumber { get; }

    internal static GroupRecipient Parse(string sveveFormattedRecipient)
    {
        if (sveveFormattedRecipient == null)
            throw new ArgumentNullException(nameof(sveveFormattedRecipient));

        var parts = sveveFormattedRecipient.Split(';');
        return parts.Length == 1 ? new GroupRecipient(null, parts[0]) : new GroupRecipient(parts[0], parts[1]);
    }
}
