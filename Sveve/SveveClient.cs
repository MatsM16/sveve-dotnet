using System;
using System.Net.Http;

namespace Sveve;

/// <summary>
/// A managed client for communicating with the Sveve API.
/// </summary>
public class SveveClient : IDisposable
{
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

        Groups = new SveveGroupClient(this);
        Admin = new SveveAdminClient(this);
        Sms = new SveveSmsClient(this);
    }

    /// <summary>
    /// Validated options for the Sveve client.
    /// </summary>
    internal SveveClientOptions Options { get; }

    /// <summary>
    /// HttpClient used to communicate with the Sveve API.
    /// </summary>
    internal HttpClient HttpClient { get; }

    /// <inheritdoc cref="SveveGroupClient"/>
    public SveveGroupClient Groups { get; }

    /// <inheritdoc cref="SveveAdminClient"/>
    public SveveAdminClient Admin { get; }

    /// <inheritdoc cref="SveveSmsClient"/>
    public SveveSmsClient Sms { get; }

    /// <inheritdoc />
    public void Dispose()
    {
        HttpClient.Dispose();
    }

    private static HttpClient DefaultHttpClientFactory() => new()
    {
        BaseAddress = new Uri("https://sveve.no/SMS")
    };
}