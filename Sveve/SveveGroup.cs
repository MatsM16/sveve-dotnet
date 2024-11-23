using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Authentication;
using System.Threading;
using System.Threading.Tasks;
using Sveve.Commands;
using Sveve.Sending;

namespace Sveve;

/// <summary>
/// A group of SMS recipients.
/// </summary>
/// <remarks>
/// Obtained by calling <see cref="SveveClient.Group"/>.
/// </remarks>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class SveveGroup
{
    private readonly SveveClient _client;
    private readonly string _groupName;

    internal SveveGroup(SveveClient client, string groupName) => (_client, _groupName) = (client, groupName);

    /// <summary>
    /// Creates this group if it does not already exist. 
    /// </summary>
    /// <remarks>
    /// Does nothing if the group already exists.
    /// </remarks>
    /// <param name="cancellationToken"></param>
    /// <exception cref="InvalidCredentialException">The username/password combination is invalid.</exception>
    /// <exception cref="ObjectDisposedException">The client is disposed.</exception>
    public Task CreateAsync(CancellationToken cancellationToken = default) 
        => _client.GroupCommand("add_group").AddParameter("group", _groupName).InvokeAsync(cancellationToken);

    /// <summary>
    /// Moves all group members to another group <paramref name="destinationGroupName"/>.
    /// </summary>
    /// <remarks>
    /// Does nothing if this group does not exist. <br/>
    /// If <paramref name="destinationGroupName"/> does not exist, it will be created. <br/>
    /// This group will be empty afterwards.
    /// </remarks>
    /// <param name="destinationGroupName">All member will be moved into this group.</param>
    /// <param name="cancellationToken"></param>
    /// <exception cref="InvalidCredentialException">The username/password combination is invalid.</exception>
    /// <exception cref="ObjectDisposedException">The client is disposed.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="destinationGroupName"/> is <see langword="null"/>.</exception>
    public Task MoveToAsync(string destinationGroupName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(destinationGroupName))
            throw new ArgumentNullException(nameof(destinationGroupName));
        var command = _client.GroupCommand("move_group").AddParameter("group", _groupName).AddParameter("new_group", destinationGroupName);
        return SendWithRequiredGroup(command, destinationGroupName, cancellationToken);
    }

    /// <summary>
    /// Deletes this group.
    /// </summary>
    /// <remarks>
    /// Does nothing if the group does not exist.
    /// </remarks>
    /// <param name="cancellationToken"></param>
    /// <exception cref="InvalidCredentialException">The username/password combination is invalid.</exception>
    /// <exception cref="ObjectDisposedException">The client is disposed.</exception>
    public Task DeleteAsync(CancellationToken cancellationToken = default) 
        => _client.GroupCommand("delete_group").AddParameter("group", _groupName).InvokeAsync(cancellationToken);

    /// <summary>
    /// Lists all the members in this group.
    /// </summary>
    /// <remarks>
    /// Returns empty if the group does not exist.
    /// </remarks>
    /// <param name="cancellationToken"></param>
    /// <exception cref="InvalidCredentialException">The username/password combination is invalid.</exception>
    /// <exception cref="ObjectDisposedException">The client is disposed.</exception>
    public async Task<List<SveveGroupMember>> MembersAsync(CancellationToken cancellationToken = default)
    {
        var result = await _client.GroupCommand("list_recipients").AddParameter("group", _groupName).LinesAsync(cancellationToken).ConfigureAwait(false);
        if (result.Count > 0 && GroupDoesNotExist(result[0])) return [];
        return result.Select(x => x.Split(';')).Select(x => x.Length > 1 ? new SveveGroupMember(x[1], x[0]) : new SveveGroupMember(x[0], null)).ToList();
    }

    /// <summary>
    /// Adds a member to this group.
    /// </summary>
    /// <remarks>
    /// If this group does not exist, it will be created.
    /// </remarks>
    /// <param name="phoneNumber">Phone number of the recipient.</param>
    /// <param name="displayName">Display name for the recipient. This is optional.</param>
    /// <param name="cancellationToken"></param>
    /// <exception cref="InvalidCredentialException">The username/password combination is invalid.</exception>
    /// <exception cref="ObjectDisposedException">The client is disposed.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="phoneNumber"/> is <see langword="null"/>.</exception>
    public Task AddMemberAsync(string phoneNumber, string? displayName = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            throw new ArgumentNullException(nameof(phoneNumber));
        var command = _client.GroupCommand("add_recipient").AddParameter("group", _groupName).AddParameter("name", displayName ?? "").AddParameter("number", phoneNumber);
        return SendWithRequiredGroup(command, _groupName, cancellationToken);
    }

    /// <summary>
    /// Removes a member from this group.
    /// </summary>
    /// <remarks>
    /// Does nothing if the group or member does not exist.
    /// </remarks>
    /// <param name="phoneNumber">Phone number of the member.</param>
    /// <param name="cancellationToken"></param>
    /// <exception cref="InvalidCredentialException">The username/password combination is invalid.</exception>
    /// <exception cref="ObjectDisposedException">The client is disposed.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="phoneNumber"/> is <see langword="null"/>.</exception>
    public Task RemoveMemberAsync(string phoneNumber, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            throw new ArgumentNullException(nameof(phoneNumber));
        return _client.GroupCommand("delete_recipient").AddParameter("group", _groupName).AddParameter("number", phoneNumber).InvokeAsync(cancellationToken);
    }

    /// <summary>
    /// Moves a single member from this group to another.
    /// </summary>
    /// <remarks>
    /// Does nothing if this group or if <paramref name="phoneNumber"/> is not in this group. <br/>
    /// If <paramref name="destinationGroupName"/> does not exist, it will be created. <br/>
    /// <paramref name="phoneNumber"/> will no longer be in this group.
    /// </remarks>
    /// <param name="destinationGroupName"><paramref name="phoneNumber"/> will be moved into this group.</param>
    /// <param name="phoneNumber">Phone number of member.</param>
    /// <param name="cancellationToken"></param>
    /// <exception cref="InvalidCredentialException">The username/password combination is invalid.</exception>
    /// <exception cref="ObjectDisposedException">The client is disposed.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="destinationGroupName"/> or <paramref name="phoneNumber"/> is <see langword="null"/>.</exception>
    public Task MoveToAsync(string destinationGroupName, string phoneNumber, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(destinationGroupName))
            throw new ArgumentNullException(nameof(destinationGroupName));
        if (string.IsNullOrWhiteSpace(phoneNumber))
            throw new ArgumentNullException(nameof(phoneNumber));
        var command = _client.GroupCommand("move_recipient").AddParameter("group", _groupName).AddParameter("new_group", destinationGroupName).AddParameter("number", phoneNumber);
        return SendWithRequiredGroup(command, destinationGroupName, cancellationToken);
    }

    /// <summary>
    /// Returns <see langword="true"/> if the group exists, otherwise <see langword="false"/>.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <exception cref="InvalidCredentialException">The username/password combination is invalid.</exception>
    /// <exception cref="ObjectDisposedException">The client is disposed.</exception>
    public async Task<bool> ExistsAsync(CancellationToken cancellationToken = default)
    {
        var doesNotExist = Guid.NewGuid().ToString();
        var result = await _client.GroupCommand("delete_recipient").AddParameter("group", _groupName).AddParameter("number", doesNotExist).InvokeAsync(cancellationToken).ConfigureAwait(false);
        return GroupDoesNotExist(result) is false;
    }

    /// <summary>
    /// Returns <see langword="true"/> if this group has the member, otherwise <see langword="false"/>.
    /// </summary>
    /// <param name="phoneNumber">Phone number of the member.</param>
    /// <param name="cancellationToken"></param>
    /// <exception cref="InvalidCredentialException">The username/password combination is invalid.</exception>
    /// <exception cref="ObjectDisposedException">The client is disposed.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="phoneNumber"/> is <see langword="null"/>.</exception>
    public async Task<bool> HasMemberAsync(string phoneNumber, CancellationToken cancellationToken = default)
    {
        var normalized = new SmsRecipient(phoneNumber);
        var recipients = await MembersAsync(cancellationToken).ConfigureAwait(false);
        return recipients.Select(x => new SmsRecipient(x.PhoneNumber)).Contains(normalized);
    }

    private static bool GroupDoesNotExist(string response) => response.StartsWith($"Gruppen finnes ikke: ");

    /// <summary>
    /// Sends a command which requires a group to exist.
    /// </summary>
    /// <remarks>
    /// If the group does not exist, it will be created and the command will be sent again.
    /// </remarks>
    private async Task SendWithRequiredGroup(SveveCommand command, string requiredGroup, CancellationToken cancellationToken)
    {
        var result = await command.InvokeAsync(cancellationToken).ConfigureAwait(false);
        if (GroupDoesNotExist(result))
        {
            await _client.Group(requiredGroup).CreateAsync(cancellationToken).ConfigureAwait(false);
            await command.InvokeAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Returns the name of the group.
    /// </summary>
    public override string ToString()
    {
        return _groupName;
    }
}
