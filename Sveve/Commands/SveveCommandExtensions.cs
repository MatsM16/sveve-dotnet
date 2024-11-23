using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sveve.Commands;

/// <summary>
/// Extensions for the SveveClient.
/// </summary>
internal static class SveveCommandExtensions
{
    public static SveveCommand Command(this SveveClient client, string endpoint, string command) => new (client, endpoint, command);

    public static SveveCommand AdminCommand(this SveveClient client, string command) => client.Command("SMS/AccountAdm", command);

    public static SveveCommand GroupCommand(this SveveClient client, string command) => client.Command("SMS/RecipientAdm", command);

    public static async Task<List<string>> LinesAsync(this SveveCommand command, CancellationToken cancellationToken = default)
    {
        var responseText = await command.InvokeAsync(cancellationToken);
        return responseText.Split('\n').Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
    }
}
