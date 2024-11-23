using System.Diagnostics;
using System.Security.Authentication;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;

namespace Sveve.Commands;

[DebuggerDisplay($"{{{nameof(_builder)}}}")]
internal class SveveCommand
{
    private readonly SveveClient _client;
    private readonly StringBuilder _builder = new();

    public SveveCommand(SveveClient client, string endpoint, string command)
    {
        _client = client;
        _builder.Append(endpoint).Append('?');
        AddParameter("user", client.Options.Username);
        AddParameter("passwd", client.Options.Password);
        AddParameter("cmd", command);
    }

    public SveveCommand AddParameter(string key, string value)
    {
        if (value is not null)
            _builder.Append(key).Append('=').Append(UrlEncoder.Default.Encode(value)).Append('&');
        return this;
    }

    public async Task<string> InvokeAsync(CancellationToken cancellationToken)
    {
        var response = await _client.HttpClient.GetAsync(_builder.ToString(), cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var responseText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        if (responseText.StartsWith("Feil brukernavn/passord"))
            throw new InvalidCredentialException();

        return responseText ?? "";
    }
}