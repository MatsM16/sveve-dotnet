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
    private static readonly JsonSerializerOptions _jsonOptions = new()
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
    /// <param name="mobilePhoneNUmber"></param>
    /// <param name="content"></param>
    /// <param name="options"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>The ID of the sent message.</returns>
    /// <exception cref="ArgumentNullException">Request is null.</exception>
    /// <exception cref="ArgumentException">Receiver is not a single phone number.</exception>
    /// <exception cref="SmsNotSentException">The SMS failed to send.</exception>
    public async Task<int> SendSingleAsync(string mobilePhoneNUmber, string content, SmsOptions? options = null, CancellationToken cancellationToken = default)
    {
        if (SmsReceiver.IsSinglePhoneNumber(mobilePhoneNUmber) is false)
            throw new ArgumentException($"{nameof(mobilePhoneNUmber)} must be a single mobile phone number.", nameof(mobilePhoneNUmber));

        var results = await SendAsync(mobilePhoneNUmber, content, options, cancellationToken).ConfigureAwait(false);
        var result = results.FirstOrDefault() ?? SmsResult.Failed(mobilePhoneNUmber, "Could not get a response");

        return result.MessageId;
    }

    /// <summary>
    /// Sends a single SMS to one or more receivers.
    /// </summary>
    /// <param name="receivers"></param>
    /// <param name="content"></param>
    /// <param name="options"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>One result per sent SMS.</returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="SmsNotSentException">The sending as a whole failed.</exception>
    public async Task<List<SmsResult>> SendAsync(string receivers, string content, SmsOptions? options = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(receivers))
            throw new ArgumentNullException($"{nameof(receivers)} cannot be null or empty.", nameof(receivers));

        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentNullException($"{nameof(content)} cannot be null or empty.", nameof(content));

        var properties = CreateProperties(options?.IsTest);
        AddMessageProperties(properties, receivers, content);
        AddOptionsProperties(properties, options);

        return await SendAsync(properties, receivers, cancellationToken).ConfigureAwait(false);
    }

    private async Task<List<SmsResult>> SendAsync(Dictionary<string, object> properties, string receiversAsString, CancellationToken cancellationToken = default)
    {
        var httpResponse = await _client.HttpClient.PostAsync("SMS/SendMessage", CreateContent(properties), cancellationToken).ConfigureAwait(false);
        if (httpResponse.IsSuccessStatusCode is false)
            throw new SmsNotSentException($"Failed to send SMS. Status code: {httpResponse.StatusCode}");

        var response = await ReadResponseAsync(httpResponse).ConfigureAwait(false);

        if (response is null)
            return [];

        if (string.IsNullOrWhiteSpace(response.FatalError) is false)
            throw new SmsNotSentException(response.FatalError);

        var receivers = SmsReceiver.From(receiversAsString);

        return [
            ..AddFailedResults(receivers, response), 
            ..AddSuccessfulResults(receivers, response)];
    }

    private static List<SmsResult> AddFailedResults(List<SmsReceiver> receivers, ResponseContent response)
    {
        var results = new List<SmsResult>();
        foreach (var error in response.Errors ?? [])
        {
            var number = error.Number ?? "not a number";
            receivers.RemoveAll(x => x.IsReceiver(number));
            results.Add(SmsResult.Failed(number, error.Message));
        }
        return results;
    }

    private static List<SmsResult> AddSuccessfulResults(List<SmsReceiver> receivers, ResponseContent response)
    {
        if (receivers.Any(x => x.IsPhoneNumber is false))
            // When receivers contains groups, we are unable
            // to match message ids to phone numbers since
            // we don't have the actual phone numbers.
            return [];

        return receivers.Zip(response.Ids ?? [], (receiver, messageId) => receiver.CreateOkResult(messageId, response.Test)).ToList();
    }

    private static void AddMessageProperties(Dictionary<string, object> properties, string receiver, string sms)
    {
        properties["to"] = receiver;
        properties["msg"] = sms;
    }

    private static void AddOptionsProperties(Dictionary<string, object> properties, SmsOptions? options)
    {
        if (options is null)
            return;

        if (options.Sender is not null)
            properties.Add("from", options.Sender);

        if (options.ReplyToMessageId is not null)
            properties.Add("reply_id", options.ReplyToMessageId);

        if (options.IsReplyAllowed)
            properties.Add("reply", true);

        if (options.Reference is not null)
            properties.Add("ref", options.Reference);

        if (options.IsTest)
            properties.Add("test", true);

        options.Repeat?.AddProperties(properties);
    }

    private static HttpContent CreateContent(Dictionary<string, object> properties)
    {
        var jsonContent = JsonSerializer.Serialize(properties);
        return new StringContent(jsonContent, Encoding.UTF8, "application/json");
    }

    private async Task<ResponseContent?> ReadResponseAsync(HttpResponseMessage httpResponse)
    {
        using var httpContentStream = await httpResponse.Content.ReadAsStreamAsync().ConfigureAwait(false);
        var apiResponse = await JsonSerializer.DeserializeAsync<SendMessageResponse>(httpContentStream, _jsonOptions).ConfigureAwait(false);
        return apiResponse?.Response;
    }

    private Dictionary<string, object> CreateProperties(bool? optionsIsTest) => new()
    {
        ["user"] = _client.Options.Username,
        ["passwd"] = _client.Options.Password,
        ["test"] = (optionsIsTest ?? false) || _client.Options.IsTest,
        ["f"] = "json",
    };

    private class SendMessageResponse
    {
        public ResponseContent? Response { get; set; }
    }

    private class ResponseContent
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
