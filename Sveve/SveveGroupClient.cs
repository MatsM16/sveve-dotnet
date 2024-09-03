using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;

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

    public Task CreateAsync(string group, CancellationToken cancellationToken = default) => SendAsync("add_group", new() { ["group"] = group }, cancellationToken);

    public async Task MoveRecipientsAsync(string fromGroup, string toGroup, CancellationToken cancellationToken = default)
    {
        var result = await MoveRecipientsCore(fromGroup, toGroup, cancellationToken).ConfigureAwait(false);

        if (result.StartsWith($"Gruppen finnes ikke: {toGroup}"))
        {
            await CreateAsync(toGroup, cancellationToken).ConfigureAwait(false);
            await MoveRecipientsCore(fromGroup, toGroup, cancellationToken).ConfigureAwait(false);
        }
    }

    public Task DeleteAsync(string group, CancellationToken cancellationToken = default) => SendAsync("delete_group", new() { ["group"] = group }, cancellationToken);

    public async Task<List<string>> ListAsync(CancellationToken cancellationToken = default)
    {
        var listAsString = await SendAsync("list_groups", new() { }, cancellationToken).ConfigureAwait(false);
        return listAsString.Split('\n').Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
    }

    public async Task<List<GroupRecipient>> ListRecipientsAsync(string group, CancellationToken cancellationToken = default)
    {
        var listAsString = await SendAsync("list_recipients", new() { ["group"] = group }, cancellationToken).ConfigureAwait(false);
        return listAsString.Split('\n').Where(x => !string.IsNullOrWhiteSpace(x)).Select(GroupRecipient.Parse).ToList();
    }

    public Task AddRecipientAsync(string group, string recipientName, string phoneNumber, CancellationToken cancellationToken = default) => SendAsync("add_recipient", new() { ["group"] = group, ["name"] = recipientName, ["number"] = phoneNumber }, cancellationToken);

    public Task RemoveRecipientAsync(string group, string phoneNumber, CancellationToken cancellationToken = default) => SendAsync("delete_recipient", new() { ["group"] = group, ["number"] = phoneNumber }, cancellationToken);

    public async Task MoveRecipientAsync(string fromGroup, string toGroup, string phoneNumber, CancellationToken cancellationToken = default)
    {
        var result = await MoveRecipientCore(fromGroup, toGroup, phoneNumber, cancellationToken).ConfigureAwait(false);

        if (result.StartsWith($"Gruppen finnes ikke: {toGroup}"))
        {
            await CreateAsync(toGroup, cancellationToken).ConfigureAwait(false);
            await MoveRecipientCore(fromGroup, toGroup, phoneNumber, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task<string> SendAsync(string command, Dictionary<string,string> parameters, CancellationToken cancellationToken)
    {
        var commandBuilder = new StringBuilder().Append("SMS/RecipientAdm?");

        parameters.Add("user", _client.Options.Username);
        parameters.Add("passwd", _client.Options.Password);
        parameters.Add("cmd", command);
        foreach (var pair in parameters)
            commandBuilder.Append(pair.Key).Append('=').Append(UrlEncoder.Default.Encode(pair.Value)).Append('&');

        var response = await _client.HttpClient.GetAsync(commandBuilder.ToString(), cancellationToken).ConfigureAwait(false);
        var responseText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return responseText;
    }

    private Task<string> MoveRecipientsCore(string fromGroup, string toGroup, CancellationToken cancellationToken) => SendAsync("move_group", new() { ["group"] = fromGroup, ["new_group"] = toGroup }, cancellationToken);
    private Task<string> MoveRecipientCore(string fromGroup, string toGroup, string phoneNumber, CancellationToken cancellationToken) => SendAsync("move_recipient", new() { ["group"] = fromGroup, ["new_group"] = toGroup, ["number"] = phoneNumber }, cancellationToken);
}
