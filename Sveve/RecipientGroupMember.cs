using System;

namespace Sveve;

/// <summary>
/// A member of a recipient group.
/// </summary>
public sealed class RecipientGroupMember
{
    private RecipientGroupMember(string phoneNumber, string? name)
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

    internal static RecipientGroupMember Parse(string sveveFormattedRecipient)
    {
        if (sveveFormattedRecipient == null)
            throw new ArgumentNullException(nameof(sveveFormattedRecipient));

        var parts = sveveFormattedRecipient.Split(';');
        return parts.Length == 1 ? new RecipientGroupMember(parts[0], null) : new RecipientGroupMember(parts[1], parts[0]);
    }
}
