using Sveve.Commands;
using Sveve.Sending;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Security.Authentication;
using System.Threading;
using System.Threading.Tasks;

namespace Sveve;

/// <summary>
/// A managed client for communicating with the Sveve API.
/// </summary>
[DebuggerDisplay($"{{{nameof(HttpClient)}.{nameof(HttpClient.BaseAddress)}}}")]
public class SveveClient : IDisposable
{
    private readonly SendEndpoint _sendEndpoint;

    /// <summary>
    /// Validates the <paramref name="options"/> and configures a new client for the Sveve API.
    /// </summary>
    /// <exception cref="ArgumentNullException">A required option is missing.</exception>
    public SveveClient(SveveClientOptions options)
    {
        if (options is null)
            throw new ArgumentNullException(nameof(options));

        if (string.IsNullOrWhiteSpace(options.Username))
            throw new ArgumentNullException(nameof(options), $"{nameof(options)}.{nameof(SveveClientOptions.Username)} is required.");

        if (string.IsNullOrWhiteSpace(options.Password))
            throw new ArgumentNullException(nameof(options), $"{nameof(options)}.{nameof(SveveClientOptions.Password)} is required.");

        Options = options;
        HttpClient = options.HttpClientFactory?.Invoke() ?? DefaultHttpClientFactory();
        _sendEndpoint = new SendEndpoint(this);
    }

    /// <summary>
    /// Configures a new client for the Sveve API
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="username"/> or <paramref name="password"/> is <see langword="null"/>.</exception>
    public SveveClient(string username, string password) : this(new SveveClientOptions { Username = username, Password = password }) { }

    /// <summary>
    /// Validated options for the Sveve client.
    /// </summary>
    internal SveveClientOptions Options { get; }

    /// <summary>
    /// HttpClient used to communicate with the Sveve API.
    /// </summary>
    internal HttpClient HttpClient { get; }

    /// <inheritdoc />
    public void Dispose()
    {
        HttpClient.Dispose();
    }

    private static HttpClient DefaultHttpClientFactory() => new()
    {
        BaseAddress = new Uri("https://sveve.no")
    };

    /// <summary>
    /// Buys additional SMS units.
    /// </summary>
    /// <remarks>
    /// Invoking this method will place a real order which costs real money. <br/>
    /// Buying larger quantities is cheaper per unit. <br/>
    /// </remarks>
    /// <exception cref="InvalidCredentialException">The username/password combination is invalid.</exception>
    /// <exception cref="ObjectDisposedException">The client is disposed.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="order"/> is null.</exception>
    public async Task PurchaseSmsUnitsAsync(SmsUnitOrder order, CancellationToken cancellationToken = default)
    {
        if (order is null) throw new ArgumentNullException(nameof(order));
        await this.AdminCommand("order_sms").AddParameter("count", order.SmsUnits.ToString()).InvokeAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Returns the number of SMS units remaining on the current Sveve account.
    /// </summary>
    /// <exception cref="InvalidCredentialException">The username/password combination is invalid.</exception>
    /// <exception cref="ObjectDisposedException">The client is disposed.</exception>
    public async Task<int> RemainingSmsUnitsAsync(CancellationToken cancellationToken = default) 
        => int.Parse(await this.AdminCommand("sms_count").InvokeAsync(cancellationToken).ConfigureAwait(false));

    /// <summary>
    /// Returns the names of all the sms groups.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <exception cref="InvalidCredentialException">The username/password combination is invalid.</exception>
    /// <exception cref="ObjectDisposedException">The client is disposed.</exception>
    public Task<List<string>> GroupsAsync(CancellationToken cancellationToken = default) => this.GroupCommand("list_groups").LinesAsync(cancellationToken);

    /// <summary>
    /// Returns a client for the given group.
    /// </summary>
    /// <param name="groupName">Name of the group.</param>
    /// <exception cref="ArgumentNullException"><paramref name="groupName"/> is <see langword="null"/>.</exception>
    public SveveGroup Group(string groupName)
    {
        if (string.IsNullOrWhiteSpace(groupName))
            throw new ArgumentNullException(nameof(groupName));
        return new(this, groupName);
    }

    /// <summary>
    /// Sends a sms request to Sveve.
    /// </summary>
    /// <exception cref="InvalidCredentialException">The username/password combination is invalid.</exception>
    /// <exception cref="SmsNotSentException">Failed to send the SMS request.</exception>
    /// <exception cref="ObjectDisposedException">The client is disposed.</exception>
    public Task<SendResponse> SendAsync(Sms sms, CancellationToken cancellationToken = default) => _sendEndpoint.SendAsync(sms, cancellationToken);

    /// <summary>
    /// Bulks and sends a sms request to Sveve.
    /// </summary>
    /// <remarks>
    /// The bulk is sent as a single request. This can be useful if the limit of 5 concurrent API requests becomes an issue.
    /// </remarks>
    /// <exception cref="InvalidCredentialException">The username/password combination is invalid.</exception>
    /// <exception cref="SmsNotSentException">Failed to send the SMS request.</exception>
    /// <exception cref="ObjectDisposedException">The client is disposed.</exception>
    public Task<SendResponse> SendAsync(IEnumerable<Sms> bulk, CancellationToken cancellationToken = default) => _sendEndpoint.SendAsync(bulk, cancellationToken);
}