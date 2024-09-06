using System;
using System.Linq;
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
    private static readonly int[] AllowedOrderSizes = [500, 2_000, 5_000, 10_000, 25_000, 50_000, 100_000];
    private static readonly string AllowedOrderSizesString = string.Join(", ", AllowedOrderSizes);

    private readonly SveveClient _client;

    internal SveveAdminClient(SveveClient client)
    {
        _client = client;
    }

    /// <summary>
    /// Orders <paramref name="count"/> SMS units.
    /// </summary>
    /// <remarks>
    /// <paramref name="count"/> must be one of <c>500</c>, <c>2 000</c>, <c>5 000</c>, <c>10 000</c>, <c>25 000</c>, <c>50 000</c>, or <c>100 000</c>. <br/>
    /// Invoking this method will place a real order which costs real money. <br/>
    /// Buying larger quantities is cheaper per unit. <br/>
    /// For prices, see the <a href="https://sveve.no/tjenester">Sveve services and prices</a>.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is less than 500 or greater than 100000.</exception>
    /// <exception cref="InvalidCredentialException">The username/password combination is invalid.</exception>
    public async Task OrderSmsAsync(int count, CancellationToken cancellationToken = default)
    {
        if (AllowedOrderSizes.Contains(count) is false)
            throw new ArgumentOutOfRangeException(nameof(count), count, $"{nameof(count)} must be one of {AllowedOrderSizesString}.");

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