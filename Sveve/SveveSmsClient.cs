using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Sveve;

/// <summary>
/// Client for sending sms messages.
/// </summary>
public sealed class SveveSmsClient
{
    private readonly SveveClient _client;
    private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    };

    internal SveveSmsClient(SveveClient client)
    {
        _client = client;
    }

    /// <summary>
    /// Sends a single SMS message to a single receiver.
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>The result for the single sent SMS.</returns>
    /// <exception cref="ArgumentNullException">Request is null.</exception>
    /// <exception cref="ArgumentException">Receiver is not a single phone number.</exception>
    /// <exception cref="SendSmsFailedException">The sending as a whole failed.</exception>
    public async Task<SendSmsResult> SendSingleAsync(SendSmsRequest request, CancellationToken cancellationToken = default)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        var receivers = SmsReceiver.From(request.Receiver);
        if (receivers.Count is 0 or > 1)
            throw new ArgumentException($"{nameof(SendSingleAsync)} can only send to exactly one phone number.");

        var results = await SendAsync(request, cancellationToken).ConfigureAwait(false);
        return results.FirstOrDefault() ?? SendSmsResult.Failed(request.Receiver, "Could not get a response");
    }

    /// <summary>
    /// Sends a single SMS to one or more receivers.
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>One result per sent SMS.</returns>
    /// <exception cref="ArgumentNullException">Request is null.</exception>
    /// <exception cref="SendSmsFailedException">The sending as a whole failed.</exception>
    public async Task<List<SendSmsResult>> SendAsync(SendSmsRequest request, CancellationToken cancellationToken = default)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        return await SendBulkAsync([request], cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends one or more SMS messages.
    /// </summary>
    /// <param name="requests">One or more send requests.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>A result for each sent SMS.</returns>
    /// <exception cref="ArgumentNullException">Request list is null.</exception>
    /// <exception cref="ArgumentException">Requests contains test and real messages.</exception>
    /// <exception cref="SendSmsFailedException">The sending as a whole failed.</exception>
    public async Task<List<SendSmsResult>> SendBulkAsync(IEnumerable<SendSmsRequest> requests,  CancellationToken cancellationToken = default)
    {
        if (requests is null)
            throw new ArgumentNullException(nameof(requests));

        var isTest = requests.FirstOrDefault()?.IsTest;
        if (isTest is null)
            return [];

        if (requests.Any(x => x.IsTest != isTest))
            throw new ArgumentException("Cannot send a mix of real and test messages.", nameof(requests));

        try
        {
            var body = new Dictionary<string, object>
            {
                ["user"] = _client.Options.Username,
                ["passwd"] = _client.Options.Password,
                ["test"] = isTest,
                ["f"] = "json",
                ["messages"] = requests.Select(request =>
                {
                    var messageProperties = new Dictionary<string, object>();
                    request.AddProperties(messageProperties);
                    return messageProperties;
                })
            };

            var jsonContent = JsonSerializer.Serialize(body);

            var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var httpResponse = await _client.HttpClient.PostAsync("SMS/SendMessage", httpContent, cancellationToken).ConfigureAwait(false);

            httpResponse.EnsureSuccessStatusCode();

            using var httpContentStream = await httpResponse.Content.ReadAsStreamAsync().ConfigureAwait(false);
            var response = await JsonSerializer.DeserializeAsync<SmsResponse>(httpContentStream, _jsonOptions, cancellationToken).ConfigureAwait(false);

            if (response?.Response is null)
                return [];

            var receivers = requests.SelectMany(r => SmsReceiver.From(r.Receiver)).ToList();

            var results = new List<SendSmsResult>();

            foreach (var error in response.Response.Errors ?? [])
            {
                var number = error.Number ?? "not a number";
                receivers.RemoveAll(x => x.IsReceiver(number));
                results.Add(SendSmsResult.Failed(number, error.Message));
            }

            if (receivers.Any(x => x.IsPhoneNumber is false))
                // When receivers contains groups, we are unable
                // to match message ids to phone numbers since
                // we don't have the actual phone numbers.
                return results;

            var successResults = receivers.Zip(response.Response.Ids ?? [], (receiver, messageId) => receiver.CreateOkSendResponse(messageId, response.Response.Test));
            results.AddRange(successResults);
            return results;
        }
        catch (Exception exception)
        {
            throw new SendSmsFailedException($"Failed to send", exception);
        }
    }

    private class SmsResponse
    {
        public SmsResponseContentDto? Response { get; set; }
    }

    private class SmsResponseContentDto
    {
        public int MsgOkCount { get; set; }
        public int StdSmsCount { get; set; }
        public string? FatalError { get; set; }
        public List<int>? Ids { get; set; }
        public List<SmsErrorDto>? Errors { get; set; }
        public bool Test {get; set; }
    }

    private class SmsErrorDto
    {
        public string? Number { get; set; }
    
        public string? Message { get; set; }
    }
}
