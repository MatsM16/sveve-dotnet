using System.Security.Authentication;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;

namespace Sveve;

internal class SveveCommandBuilder
{
    private readonly SveveClient _client;
    private readonly StringBuilder _builder = new();

    public SveveCommandBuilder(SveveClient client, string endpoint, string command)
    {
        _client = client;
        _builder.Append(endpoint).Append('?');
        AddParameter("user", client.Options.Username);
        AddParameter("passwd", client.Options.Password);
        AddParameter("cmd", command);
    }

    public SveveCommandBuilder AddParameter(string key, string value)
    {
        _builder.Append(key).Append('=').Append(UrlEncoder.Default.Encode(value)).Append('&');
        return this;
    }

    public async Task<string> SendAsync(CancellationToken cancellationToken)
    {
        var response = await _client.HttpClient.GetAsync(_builder.ToString(), cancellationToken).ConfigureAwait(false);
        var responseText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

        response.EnsureSuccessStatusCode();

        if (responseText.StartsWith("Feil brukernavn/passord"))
            throw new InvalidCredentialException();

        return responseText;
    }
}