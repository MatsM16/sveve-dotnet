using System.Diagnostics;

namespace Sveve;

/// <summary>
/// A member of a sms group.
/// </summary>
/// <param name="phoneNumber"></param>
/// <param name="name"></param>
[DebuggerDisplay("{ToString(),nq}")]
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
    
    /// <inheritdoc />
    public override string ToString()
    {
        return Name is null ? PhoneNumber : $"{Name}: {PhoneNumber}";
    }
}
