using System;

namespace Sveve;

/// <summary>
/// A member of a sms group.
/// </summary>
/// <param name="phoneNumber"></param>
/// <param name="name"></param>
public sealed class SveveGroupMember(string phoneNumber, string? name)
{
    /// <summary>
    /// Name of the member.
    /// </summary>
    public string? Name { get; } = name;

    /// <summary>
    /// Phone number of the member.
    /// </summary>
    public string PhoneNumber { get; } = phoneNumber;
}
