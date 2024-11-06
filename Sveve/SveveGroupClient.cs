using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Authentication;
using System.Threading;
using System.Threading.Tasks;
using Sveve.Extensions;

namespace Sveve;

/// <summary>
/// Client for managing groups of recipients.
/// </summary>
public sealed class SveveGroupClient
{
    private readonly SveveClient _client;

    internal SveveGroupClient(SveveClient client) => _client = client;

    /// <inheritdoc cref="SveveClient(SveveClientOptions)"/>
    public SveveGroupClient(SveveClientOptions options) : this(new SveveClient(options)) { }

    /// <summary>
    /// Creates a new group. 
    /// </summary>
    /// <remarks>
    /// Does nothing if the group already exists.
    /// </remarks>
    /// <param name="group">Name of the group.</param>
    /// <param name="cancellationToken"></param>
    /// <exception cref="InvalidCredentialException">The username/password combination is invalid.</exception>
    public Task CreateAsync(string group, CancellationToken cancellationToken = default) => Command("add_group").AddParameter("group", group).SendAsync(cancellationToken);

    /// <summary>
    /// Moves all recipients from one <paramref name="fromGroup"/> to <paramref name="toGroup"/>.
    /// </summary>
    /// <remarks>
    /// Does nothing if <paramref name="fromGroup"/> does not exist. <br/>
    /// If <paramref name="toGroup"/> does not exist, it will be created.
    /// </remarks>
    /// <param name="fromGroup">Name of group to take recipients from.</param>
    /// <param name="toGroup">Name of group to receive recipients.</param>
    /// <param name="cancellationToken"></param>
    /// <exception cref="InvalidCredentialException">The username/password combination is invalid.</exception>
    public Task MoveRecipientsAsync(string fromGroup, string toGroup, CancellationToken cancellationToken = default)
    {
        var command = Command("move_group").AddParameter("group", fromGroup).AddParameter("new_group", toGroup);
        return SendWithRequiredGroup(command, toGroup, cancellationToken);
    }

    /// <summary>
    /// Deletes a group.
    /// </summary>
    /// <remarks>
    /// Does nothing if the group does not exist.
    /// </remarks>
    /// <param name="group">Name of the group to delete.</param>
    /// <param name="cancellationToken"></param>
    /// <exception cref="InvalidCredentialException">The username/password combination is invalid.</exception>
    public Task DeleteAsync(string group, CancellationToken cancellationToken = default) => Command("delete_group").AddParameter("group", group).SendAsync(cancellationToken);

    /// <summary>
    /// Lists the names of all groups.
    /// </summary>
    /// <param name="cancellationToken"></param>
    public async Task<List<string>> ListAsync(CancellationToken cancellationToken = default)
    {
        var result = await Command("list_groups").SendAsync(cancellationToken).ConfigureAwait(false);
        return result.Split('\n').Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
    }

    /// <summary>
    /// Lists all the recipients in a group.
    /// </summary>
    /// <param name="group"></param>
    /// <param name="cancellationToken"></param>
    /// <exception cref="InvalidCredentialException">The username/password combination is invalid.</exception>
    public async Task<List<RecipientGroupMember>> ListRecipientsAsync(string group, CancellationToken cancellationToken = default)
    {
        var result = await Command("list_recipients").AddParameter("group", group).SendAsync(cancellationToken).ConfigureAwait(false);
        var lines = GroupDoesNotExist(result, group) ? [] : result.Split('\n');
        return lines.Where(x => !string.IsNullOrWhiteSpace(x)).Select(RecipientGroupMember.Parse).ToList();
    }

    /// <summary>
    /// Adds a recipient to a group.
    /// </summary>
    /// <remarks>
    /// If <paramref name="group"/> does not exist, it will be created.
    /// </remarks>
    /// <param name="group">Name of the group.</param>
    /// <param name="recipientName">Display name for the recipient.</param>
    /// <param name="phoneNumber">Phone number of the recipient.</param>
    /// <param name="cancellationToken"></param>
    /// <exception cref="InvalidCredentialException">The username/password combination is invalid.</exception>
    public Task AddRecipientAsync(string group, string recipientName, string phoneNumber, CancellationToken cancellationToken = default)
    {
        var command = Command("add_recipient").AddParameter("group", group).AddParameter("name", recipientName).AddParameter("number", phoneNumber);
        return SendWithRequiredGroup(command, group, cancellationToken);
    }

    /// <summary>
    /// Removes a recipient from a group.
    /// </summary>
    /// <remarks>
    /// Does nothing if the group or recipient does not exist.
    /// </remarks>
    /// <param name="group">Name of the group.</param>
    /// <param name="phoneNumber">Phone number of the recipient.</param>
    /// <param name="cancellationToken"></param>
    /// <exception cref="InvalidCredentialException">The username/password combination is invalid.</exception>
    public Task RemoveRecipientAsync(string group, string phoneNumber, CancellationToken cancellationToken = default) => Command("delete_recipient").AddParameter("group", group).AddParameter("number", phoneNumber).SendAsync(cancellationToken);

    /// <summary>
    /// Moves a single recipient from one group to another.
    /// </summary>
    /// <remarks>
    /// Does nothing if the <paramref name="fromGroup"/> or recipient does not exist. <br/>
    /// If <paramref name="toGroup"/> does not exist, it will be created.
    /// </remarks>
    /// <param name="fromGroup">Group to take recipient from.</param>
    /// <param name="toGroup">Group to add recipient to.</param>
    /// <param name="phoneNumber">Phone number of recipient.</param>
    /// <param name="cancellationToken"></param>
    /// <exception cref="InvalidCredentialException">The username/password combination is invalid.</exception>
    public Task MoveRecipientAsync(string fromGroup, string toGroup, string phoneNumber, CancellationToken cancellationToken = default)
    {
        var command = Command("move_recipient").AddParameter("group", fromGroup).AddParameter("new_group", toGroup).AddParameter("number", phoneNumber);
        return SendWithRequiredGroup(command, toGroup, cancellationToken);
    }

    /// <summary>
    /// Returns <see langword="true"/> if the group exists, otherwise <see langword="false"/>.
    /// </summary>
    /// <param name="group">Name of the group.</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="InvalidCredentialException">The username/password combination is invalid.</exception>
    public async Task<bool> ExistsAsync(string group, CancellationToken cancellationToken = default)
    {
        var doesNotExist = Guid.NewGuid().ToString();
        var result = await Command("delete_recipient").AddParameter("group", group).AddParameter("number", doesNotExist).SendAsync(cancellationToken).ConfigureAwait(false);
        return GroupDoesNotExist(result, group) is false;
    }

    /// <summary>
    /// Returns <see langword="true"/> if the group has the recipient, otherwise <see langword="false"/>.
    /// </summary>
    /// <param name="group">Name of the group.</param>
    /// <param name="phoneNumber">Phone number of the recipient.</param>
    /// <param name="cancellationToken"></param>
    /// <exception cref="InvalidCredentialException">The username/password combination is invalid.</exception>
    public async Task<bool> HasRecipientAsync(string group, string phoneNumber, CancellationToken cancellationToken = default)
    {
        var normalized = new SmsRecipient(phoneNumber);
        var recipients = await ListRecipientsAsync(group, cancellationToken).ConfigureAwait(false);
        return recipients.Select(x => new SmsRecipient(x.PhoneNumber)).Contains(normalized);
    }

    private static bool GroupDoesNotExist(string response, string group) => response.StartsWith($"Gruppen finnes ikke: {group}");

    /// <summary>
    /// Sends a command which requires a group to exist.
    /// </summary>
    /// <remarks>
    /// If the group does not exist, it will be created and the command will be sent again.
    /// </remarks>
    private async Task SendWithRequiredGroup(SveveCommandBuilder command, string requiredGroup, CancellationToken cancellationToken)
    {
        var result = await command.SendAsync(cancellationToken).ConfigureAwait(false);
        if (GroupDoesNotExist(result, requiredGroup))
        {
            await CreateAsync(requiredGroup, cancellationToken).ConfigureAwait(false);
            await command.SendAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private SveveCommandBuilder Command(string command) => _client.Command("SMS/RecipientAdm", command);
}
