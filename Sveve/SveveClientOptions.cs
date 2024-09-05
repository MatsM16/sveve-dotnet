using System;
using System.Net.Http;

namespace Sveve;

/// <summary>
/// Options to configure a <see cref="SveveClient"/>.
/// </summary>
public sealed class SveveClientOptions
{
    /// <summary>
    /// API username.
    /// </summary>
    public string Username { get; set; } = null!;
    /// <summary>
    /// API password. You find this on the page API -> API-nøkkel.
    /// </summary>
    public string Password { get; set; } = null!;

    /// <summary>
    /// If <see langword="true"/>, the all messages automatically have <see cref="SmsOptions.IsTest"/> set to <see langword="true"/>.
    /// </summary>
    public bool IsTest { get; set; }

    /// <summary>
    /// Optional sender name. Can be overridden by <see cref="SmsOptions.Sender"/>.
    /// </summary>
    public string? Sender { get; set; }

    /// <summary>
    /// Optional <see cref="HttpClient"/> factory.
    /// </summary>
    public Func<HttpClient>? HttpClientFactory { get; set; }
}
