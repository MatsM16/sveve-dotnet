using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Authentication;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Sveve;

internal sealed class SendEndpoint(SveveClient client)
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
    };

    public Task<SendResponse> SendAsync(Sms sms, CancellationToken cancellationToken = default)
    {
        if (sms is null)
            throw new ArgumentNullException(nameof(sms));

        ValidateSms(sms, sms.Test);
        return SendAsync([sms!], cancellationToken);
    }

    public async Task<SendResponse> SendAsync(IEnumerable<Sms> bulk, CancellationToken cancellationToken = default)
    {
        if (bulk is null)
            throw new ArgumentNullException(nameof(bulk));

        if (bulk is not ICollection<Sms>)
            bulk = bulk.ToList();

        if (!bulk.Any())
            throw new ArgumentException($"{nameof(bulk)} must contain at least one SMS.");

        var isTest = bulk.FirstOrDefault()?.Test ?? true;
        var request = CreateRequestProperties(isTest);
        request["messages"] = bulk.Select(sms => CreateSmsProperties(sms, isTest)).ToList();

        var json = await PostSendMessageAsync(request, cancellationToken).ConfigureAwait(false);

        return ParseSendResponse(json, bulk);
    }

    private void ValidateSms(Sms sms, bool isTest)
    {
        if (sms is null)
            throw new ArgumentNullException(nameof(sms));

        if (sms.Test != isTest)
            throw new ArgumentException($"Every sms in the bulk must agree on the {nameof(Sms)}.{nameof(Sms.Test)} property. At least two messages had different values for {nameof(Sms.Test)}.");

        if (string.IsNullOrEmpty(sms.To))
            throw new ArgumentNullException($"{nameof(sms)}.{nameof(Sms.To)}");

        if (string.IsNullOrEmpty(sms.Text))
            throw new ArgumentNullException($"{nameof(sms)}.{nameof(Sms.Text)}");
    }

    private Dictionary<string, object?> CreateSmsProperties(Sms sms, bool isTest)
    {
        ValidateSms(sms, isTest);
        var properties = new Dictionary<string, object?>
        {
            ["to"] = sms.To,
            ["msg"] = sms.Text,
        };

        if (sms.ReplyAllowed || !string.IsNullOrWhiteSpace(sms.ReplyTo))
            properties["reply"] = true;

        if ((sms.From ?? client.Options.Sender) is string from)
            properties["from"] = from;

        if (sms.ReplyTo is not null)
            properties["reply_id"] = sms.ReplyTo;

        if (sms.SendTime.HasValue)
            properties["date_time"] = sms.SendTime.Value.ToString("yyyyMMddHHmm");

        if (sms.Reference is not null)
            properties["ref"] = sms.Reference;

        sms.Repeat?.AddProperties(properties);
        return properties;
    }

    private Dictionary<string, object?> CreateRequestProperties(bool isTest)
    {
        return new Dictionary<string, object?>()
        {
            ["f"] = "json",
            ["user"] = client.Options.Username,
            ["passwd"] = client.Options.Password,
            ["test"] = client.Options.Test || isTest,
        };
    }

    private async Task<JsonElement> PostSendMessageAsync(IReadOnlyDictionary<string,object?> requestBody, CancellationToken cancellationToken)
    {
        var requestJson = JsonSerializer.Serialize(requestBody, _jsonOptions);

        client.Logger?.LogDebug("Sending request {request_body}", requestJson);

        var requestContent = new StringContent(requestJson, Encoding.UTF8, "application/json");
        using var apiResponse = await client.HttpClient.PostAsync("SMS/SendMessage", requestContent, cancellationToken).ConfigureAwait(false);

        if ((int)apiResponse.StatusCode == 429/*Too many requests*/)
            throw new SmsNotSentException("Too many concurrent requests. Sveve only allows 5 concurrent API-requests. Try again later. If the problem persists, try bulk-sending.");

        if (apiResponse.IsSuccessStatusCode is false)
            throw new SmsNotSentException($"Could not send SMS. Sveve responded {(int)apiResponse.StatusCode} {apiResponse.StatusCode}.");

        var jsonString = await apiResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
        return JsonSerializer.Deserialize<JsonElement>(jsonString, _jsonOptions);
    }

    private SendResponse ParseSendResponse(JsonElement json, IEnumerable<Sms> request)
    {
        if (!json.TryGetProperty("response", out var response))
            throw new SmsNotSentException("The Sveve API responded 200 OK, but the response is invalid.");

        if (response.TryGetProperty("fatalError", out var fatalErrorProperty) && fatalErrorProperty.GetString() is string fatalError)
        {
            if (fatalError == "Feil brukernavn/passord")
                throw new InvalidCredentialException("The configured username or password is incorrect.");

            throw new SmsNotSentException($"Could not send SMS. Sveve replied {fatalError}");
        }

        var msgOkCount = 0;
        if (response.TryGetProperty("msgOkCount", out var msgOkCountProperty))
            msgOkCount = msgOkCountProperty.GetInt32();

        var stdSmsCount = 0;
        if (response.TryGetProperty("stdSmsCount", out var stdSmsCountProperty))
            stdSmsCount = stdSmsCountProperty.GetInt32();

        var receivers = request
            .Select(sms => sms.To)
            .SelectMany(SmsRecipient.Multiple)
            .ToList();

        var messageIds = new Dictionary<SmsRecipient, int>();
        var errors = new Dictionary<SmsRecipient, string>();
        foreach (var error in response.TryGetProperty("errors", out var errorsProperty) ? errorsProperty.EnumerateArray() : [])
        {
            var number = error.GetProperty("number").GetString() ?? "";
            var message = error.GetProperty("message").GetString() ?? "";
            errors.Add(new SmsRecipient(number), message);
        }

        if (!receivers.All(receiver => receiver.IsPhoneNumber))
        {
            // The list of receivers contains groups.
            // We do not yet nor currently plan to support getting message ids from messages sent to groups.
            // This would require looking up the group members in a separate API call.
            return new SendResponse(messageIds, errors, msgOkCount, stdSmsCount);
        }

        if (!response.TryGetProperty("ids", out var idsProperty))
        {
            // The response does not contain the ids of any sent messages.
            // This is still a successful response.
            client.Logger?.LogWarning("Sveve did not return any message IDs.");
            return new SendResponse(messageIds, errors, msgOkCount, stdSmsCount);
        }

        var ids = idsProperty.EnumerateArray().Select(id => id.GetInt32()).Where(id => id > 0).ToList();

        // All receivers are phone numbers.
        // If we ignore the phone numbers with errors, the ids property contains the message ids to successfully send messages in the order the phone numbers appeared.
        var receiversWithSuccess = receivers.Where(receiver => !errors.ContainsKey(receiver)).ToList();
        if (receiversWithSuccess.Count != ids.Count)
        {
            // The number of successfully sent messages does not match the number of returned ids.
            return new SendResponse(messageIds, errors, msgOkCount, stdSmsCount);
        }

        messageIds = receiversWithSuccess.Zip(ids, (receiver, messageId) => (receiver, messageId)).ToDictionary(x => x.receiver, x => x.messageId);
        return new SendResponse(messageIds, errors, msgOkCount, stdSmsCount);
    }
}
