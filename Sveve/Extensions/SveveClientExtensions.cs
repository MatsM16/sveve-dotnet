using System.Collections.Generic;
using System.Security.Authentication;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;

namespace Sveve.Extensions;

/// <summary>
/// Extensions for the SveveClient.
/// </summary>
internal static class SveveClientExtensions
{
    internal static async Task<string> SendCommandAsync(this SveveClient client, string endpoint, string command, Dictionary<string,string> parameters, CancellationToken cancellationToken)
    {
        var commandBuilder = new StringBuilder().Append(endpoint).Append('?');

        parameters.Add("user", client.Options.Username);
        parameters.Add("passwd", client.Options.Password);
        parameters.Add("cmd", command);
        foreach (var pair in parameters)
            commandBuilder.Append(pair.Key).Append('=').Append(UrlEncoder.Default.Encode(pair.Value)).Append('&');

        var response = await client.HttpClient.GetAsync(commandBuilder.ToString(), cancellationToken).ConfigureAwait(false);
        var responseText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        if (responseText.StartsWith("Feil brukernavn/passord"))
            throw new InvalidCredentialException();

        return responseText;
    }
}
