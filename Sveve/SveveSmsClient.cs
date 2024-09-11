using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Security.Authentication;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Sveve.Extensions;

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
    /// <param name="mobilePhoneNumber"></param>
    /// <param name="message"></param>
    /// <param name="options"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>The ID of the sent message.</returns>
    /// <exception cref="ArgumentNullException">Request is null.</exception>
    /// <exception cref="ArgumentException">Receiver is not a single phone number.</exception>
    /// <exception cref="SmsNotSentException">The SMS failed to send.</exception>
    /// <exception cref="InvalidCredentialException">The username/password combination is invalid.</exception>
    public async Task<int> SendSingleAsync(string mobilePhoneNumber, string message, SmsOptions? options = null, CancellationToken cancellationToken = default)
    {
        if (SmsRecipient.IsSinglePhoneNumber(mobilePhoneNumber) is false)
            throw new ArgumentException($"{nameof(mobilePhoneNumber)} must be a single mobile phone number.", nameof(mobilePhoneNumber));

        var results = await SendAsync(mobilePhoneNumber, message, options, cancellationToken).ConfigureAwait(false);
        var result = results.FirstOrDefault() ?? SmsResult.Failed(mobilePhoneNumber, "Could not get a response");

        return result.MessageId;
    }

    /// <summary>
    /// Sends a single SMS to one or more receivers.
    /// </summary>
    /// <param name="receivers"></param>
    /// <param name="message"></param>
    /// <param name="options"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>One result per sent SMS.</returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="SmsNotSentException">The sending as a whole failed.</exception>
    /// <exception cref="InvalidCredentialException">The username/password combination is invalid.</exception>
    public async Task<List<SmsResult>> SendAsync(string receivers, string message, SmsOptions? options = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(receivers))
            throw new ArgumentNullException($"{nameof(receivers)} cannot be null or empty.", nameof(receivers));

        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentNullException($"{nameof(message)} cannot be null or empty.", nameof(message));

        var properties = CreateProperties(options?.IsTest);
        AddMessageProperties(properties, receivers, message);
        AddOptionsProperties(properties, options);

        return await SendAsync(properties, receivers, options?.LookupGroupMembers ?? false, cancellationToken).ConfigureAwait(false);
    }

    private async Task<List<SmsResult>> SendAsync(Dictionary<string, object> properties, string receiversAsString, bool lookupGroupMembers, CancellationToken cancellationToken = default)
    {
        var httpResponse = await _client.HttpClient.PostAsync("SMS/SendMessage", CreateContent(properties), cancellationToken).ConfigureAwait(false);
        if (httpResponse.IsSuccessStatusCode is false)
            throw new SmsNotSentException($"Failed to send SMS. Status code: {httpResponse.StatusCode}");

        var response = await ReadResponseAsync(httpResponse).ConfigureAwait(false);

        if (response is null)
            return [];

        if (string.Equals(response.FatalError, "Feil brukernavn/passord", StringComparison.OrdinalIgnoreCase))
            throw new InvalidCredentialException();

        if (string.IsNullOrWhiteSpace(response.FatalError) is false)
            throw new SmsNotSentException(response.FatalError);

        var receivers = SmsRecipient.Multiple(receiversAsString);

        if (lookupGroupMembers)
        {
            await LookupGroupMembersAsync(receivers, cancellationToken).ConfigureAwait(false);
        }

        return [
            ..AddFailedResults(receivers, response), 
            ..AddSuccessfulResults(receivers, response)];
    }

    private static List<SmsResult> AddFailedResults(List<SmsRecipient> receivers, ResponseContent response)
    {
        var results = new List<SmsResult>();
        foreach (var error in response.Errors ?? [])
        {
            var receipient = new SmsRecipient(error.Number ?? "not a number");
            receivers.RemoveAll(receipient.Equals);
            results.Add(SmsResult.Failed(receipient.ToString(), error.Message));
        }
        return results;
    }

    private static List<SmsResult> AddSuccessfulResults(List<SmsRecipient> receivers, ResponseContent response)
    {
        if (receivers.Any(x => x.IsPhoneNumber is false))
            // When receivers contains groups, we are unable
            // to match message ids to phone numbers since
            // we don't have the actual phone numbers.
            return [];

        return receivers.Zip(response.Ids ?? [], (receiver, messageId) => SmsResult.Ok(receiver.ToString(), messageId, response.Test)).ToList();
    }

    private async Task LookupGroupMembersAsync(List<SmsRecipient> receivers, CancellationToken cancellationToken)
    {
        var receiversWithGroupMembers = new List<SmsRecipient>();
        foreach (var receiver in receivers)
        {
            if (receiver.IsPhoneNumber)
            {
                receiversWithGroupMembers.Add(receiver);
                continue;
            }

            var groupMembers = await _client.Group.ListRecipientsAsync(receiver.ToString(), cancellationToken).ConfigureAwait(false);
            receiversWithGroupMembers.AddRange(groupMembers.Select(x => new SmsRecipient(x.PhoneNumber)));
        }

        receivers.Clear();
        receivers.AddRange(receiversWithGroupMembers);
    }

    private static void AddMessageProperties(Dictionary<string, object> properties, string receiver, string sms)
    {
        properties["to"] = receiver;
        properties["msg"] = sms;
    }

    private void AddOptionsProperties(Dictionary<string, object> properties, SmsOptions? options)
    {
        if (options is null)
            return;

        if (options.Sender is not null)
            properties.Add("from", options.Sender);

        else if (_client.Options.Sender is not null)
            properties.Add("from", _client.Options.Sender);

        if (options.ReplyToMessageId is not null)
            properties.Add("reply_id", options.ReplyToMessageId);

        if (options.IsReplyAllowed)
            properties.Add("reply", true);

        if (options.Reference is not null)
            properties.Add("ref", options.Reference);
            
        if (options.ScheduledSendTime.HasValue && options.ScheduledSendTime.Value > DateTimeOffset.UtcNow)
        {
            var norwegianScheduledSendTime = options.ScheduledSendTime.Value.ToNorwegianLocalTime();
            properties.Add("time", norwegianScheduledSendTime.ToString("yyyyMMddHHmm"));
        }

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
