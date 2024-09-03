using System;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;

namespace Sveve;

/// <summary>
/// Client for managing the Sveve account.
/// </summary>
public sealed class SveveAdminClient
{
    private readonly SveveClient _client;
    private const int ORDER_MIN_COUNT = 500;
    private const int ORDER_MAX_COUNT = 100_000;

    internal SveveAdminClient(SveveClient client)
    {
        _client = client;
    }

    /// <summary>
    /// Orders <paramref name="count"/> SMS units.
    /// </summary>
    /// <remarks>
    /// <paramref name="count"/> must be between <c>500</c> and <c>100000</c> (inclusive).
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is less than 500 or greater than 100000.</exception>
    public async Task OrderSmsAsync(int count, CancellationToken cancellationToken = default)
    {
        if (count < ORDER_MIN_COUNT || count > ORDER_MAX_COUNT)
            throw new ArgumentOutOfRangeException(nameof(count), count, $"{nameof(count)} must be between {ORDER_MIN_COUNT} and {ORDER_MAX_COUNT} (both inclusive)");

        var response = await _client.HttpClient.GetAsync($"SMS/AccountAdm?cmd=order_sms&count={count}&user={UrlEncoder.Default.Encode(_client.Options.Username)}&passwd={UrlEncoder.Default.Encode(_client.Options.Password)}", cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// Returns the number of SMS units remaining.
    /// </summary>
    public async Task<int> RemainingSmsAsync(CancellationToken cancellationToken = default)
    {
        var response = await _client.HttpClient.GetAsync($"SMS/AccountAdm?cmd=sms_count&user={UrlEncoder.Default.Encode(_client.Options.Username)}&passwd={UrlEncoder.Default.Encode(_client.Options.Password)}", cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var responseAsString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        return int.Parse(responseAsString);
    }
}