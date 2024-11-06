using System.Security.Authentication;
using System.Threading;
using System.Threading.Tasks;
using Sveve.Extensions;

namespace Sveve;

/// <summary>
/// Client for managing the Sveve account.
/// </summary>
public sealed class SveveAdminClient
{
    private readonly SveveClient _client;

    internal SveveAdminClient(SveveClient client) => _client = client;

    /// <inheritdoc cref="SveveClient(SveveClientOptions)" />
    public SveveAdminClient(SveveClientOptions options) : this(new SveveClient(options)) { }

    /// <summary>
    /// Orders a given number of SMS units.
    /// </summary>
    /// <remarks>
    /// Invoking this method will place a real order which costs real money. <br/>
    /// Buying larger quantities is cheaper per unit. <br/>
    /// </remarks>
    /// <exception cref="InvalidCredentialException">The username/password combination is invalid.</exception>
    public async Task OrderSmsAsync(SmsOrderSize order, CancellationToken cancellationToken = default)
    {
        await Command("order_sms").AddParameter("count", order.SmsCount.ToString()).SendAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Returns the number of SMS units remaining.
    /// </summary>
    /// <exception cref="InvalidCredentialException">The username/password combination is invalid.</exception>
    public async Task<int> RemainingSmsAsync(CancellationToken cancellationToken = default)
    {
        var response = await Command("sms_count").SendAsync(cancellationToken).ConfigureAwait(false);
        return int.Parse(response);
    }

    private SveveCommandBuilder Command(string action) => _client.Command("SMS/AccountAdm", action);
}
