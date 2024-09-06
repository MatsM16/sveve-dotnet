using System;
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
    /// <paramref name="count"/> must be between <c>500</c> and <c>100 000</c> (inclusive).
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is less than 500 or greater than 100000.</exception>
    /// <exception cref="InvalidCredentialException">The username/password combination is invalid.</exception>
    public async Task OrderSmsAsync(int count, CancellationToken cancellationToken = default)
    {
        if (count < ORDER_MIN_COUNT || count > ORDER_MAX_COUNT)
            throw new ArgumentOutOfRangeException(nameof(count), count, $"{nameof(count)} must be between {ORDER_MIN_COUNT} and {ORDER_MAX_COUNT} (both inclusive)");

        await _client.SendCommandAsync("SMS/AccountAdm", "order_sms", new() { ["count"] = count.ToString() }, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Returns the number of SMS units remaining.
    /// </summary>
    /// <exception cref="InvalidCredentialException">The username/password combination is invalid.</exception>
    public async Task<int> RemainingSmsAsync(CancellationToken cancellationToken = default)
    {
        var response = await _client.SendCommandAsync("SMS/AccountAdm", "sms_count", [], cancellationToken).ConfigureAwait(false);
        return int.Parse(response);
    }
}