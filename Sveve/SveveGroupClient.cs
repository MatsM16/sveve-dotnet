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

    internal SveveGroupClient(SveveClient client)
    {
        _client = client;
    }

    /// <summary>
    /// Creates a new group. 
    /// </summary>
    /// <remarks>
    /// Does nothing if the group already exists.
    /// </remarks>
    /// <param name="group">Name of the group.</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="InvalidCredentialException">The username/password combination is invalid.</exception>
    public Task CreateAsync(string group, CancellationToken cancellationToken = default) => SendAsync("add_group", new() { ["group"] = group }, cancellationToken);

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
    /// <returns></returns>
    /// <exception cref="InvalidCredentialException">The username/password combination is invalid.</exception>
    public async Task MoveRecipientsAsync(string fromGroup, string toGroup, CancellationToken cancellationToken = default)
    {
        var result = await MoveRecipientsCore(fromGroup, toGroup, cancellationToken).ConfigureAwait(false);
        if (GroupDoesNotExist(result, toGroup))
        {
            await CreateAsync(toGroup, cancellationToken).ConfigureAwait(false);
            await MoveRecipientsCore(fromGroup, toGroup, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Deletes a group.
    /// </summary>
    /// <remarks>
    /// Does nothing if the group does not exist.
    /// </remarks>
    /// <param name="group">Name of the group to delete.</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="InvalidCredentialException">The username/password combination is invalid.</exception>
    public Task DeleteAsync(string group, CancellationToken cancellationToken = default) => SendAsync("delete_group", new() { ["group"] = group }, cancellationToken);

    /// <summary>
    /// Lists the names of all groups.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<List<string>> ListAsync(CancellationToken cancellationToken = default)
    {
        var result = await SendAsync("list_groups", [], cancellationToken).ConfigureAwait(false);
        return result.Split('\n').Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
    }

    /// <summary>
    /// Lists all the recipients in a group.
    /// </summary>
    /// <param name="group"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="InvalidCredentialException">The username/password combination is invalid.</exception>
    public async Task<List<GroupRecipient>> ListRecipientsAsync(string group, CancellationToken cancellationToken = default)
    {
        var result = await SendAsync("list_recipients", new() { ["group"] = group }, cancellationToken).ConfigureAwait(false);
        var lines = GroupDoesNotExist(result, group) ? [] : result.Split('\n');
        return lines.Where(x => !string.IsNullOrWhiteSpace(x)).Select(GroupRecipient.Parse).ToList();
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
    /// <returns></returns>
    /// <exception cref="InvalidCredentialException">The username/password combination is invalid.</exception>
    public async Task AddRecipientAsync(string group, string recipientName, string phoneNumber, CancellationToken cancellationToken = default)
    {
        var result = await AddRecipientCore(group, recipientName, phoneNumber, cancellationToken).ConfigureAwait(false);
        if (GroupDoesNotExist(result, group))
        {
            await CreateAsync(group, cancellationToken);
            await AddRecipientCore(group, recipientName, phoneNumber, cancellationToken).ConfigureAwait(false);
        }
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
    /// <returns></returns>
    /// <exception cref="InvalidCredentialException">The username/password combination is invalid.</exception>
    public Task RemoveRecipientAsync(string group, string phoneNumber, CancellationToken cancellationToken = default) => SendAsync("delete_recipient", new() { ["group"] = group, ["number"] = phoneNumber }, cancellationToken);

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
    /// <returns></returns>
    /// <exception cref="InvalidCredentialException">The username/password combination is invalid.</exception>
    public async Task MoveRecipientAsync(string fromGroup, string toGroup, string phoneNumber, CancellationToken cancellationToken = default)
    {
        var result = await MoveRecipientCore(fromGroup, toGroup, phoneNumber, cancellationToken).ConfigureAwait(false);
        if (GroupDoesNotExist(result, toGroup))
        {
            await CreateAsync(toGroup, cancellationToken).ConfigureAwait(false);
            await MoveRecipientCore(fromGroup, toGroup, phoneNumber, cancellationToken).ConfigureAwait(false);
        }
    }

    private static bool GroupDoesNotExist(string response, string group) => response.StartsWith($"Gruppen finnes ikke: {group}");

    private Task<string> SendAsync(string command, Dictionary<string,string> parameters, CancellationToken cancellationToken) => _client.SendCommandAsync("SMS/RecipientAdm", command, parameters, cancellationToken);
    private Task<string> MoveRecipientsCore(string fromGroup, string toGroup, CancellationToken cancellationToken) => SendAsync("move_group", new() { ["group"] = fromGroup, ["new_group"] = toGroup }, cancellationToken);
    private Task<string> MoveRecipientCore(string fromGroup, string toGroup, string phoneNumber, CancellationToken cancellationToken) => SendAsync("move_recipient", new() { ["group"] = fromGroup, ["new_group"] = toGroup, ["number"] = phoneNumber }, cancellationToken);
    private Task<string> AddRecipientCore(string group, string recipientName, string phoneNumber, CancellationToken cancellationToken) => SendAsync("add_recipient", new() { ["group"] = group, ["name"] = recipientName, ["number"] = phoneNumber }, cancellationToken);
}
