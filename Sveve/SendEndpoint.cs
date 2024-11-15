using Microsoft.Extensions.Logging;
using Sveve.Send;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Authentication;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Sveve;

internal sealed class SendEndpoint(SveveClient client)
{
    private static readonly JsonSerializerOptions _requestJsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    private static readonly JsonSerializerOptions _responseJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public Task<SendResponse> SendAsync(Sms sms, CancellationToken cancellationToken = default)
    {
        if (sms is null)
            throw new ArgumentNullException(nameof(sms));

        return SendAsync([sms!], cancellationToken);
    }

    public async Task<SendResponse> SendAsync(IEnumerable<Sms> bulk, CancellationToken cancellationToken = default)
    {
        if (bulk is null)
            throw new ArgumentNullException(nameof(bulk));

        var request = CreateRequest(bulk);

        var response = await PostAsync(request, cancellationToken);

        return CreateResponse(request, response);
    }

    private SendRequestDto CreateRequest(IEnumerable<Sms> bulk)
    {
        var dto = new SendRequestDto
        {
            User = client.Options.Username,
            Passwd = client.Options.Password,
            Test = client.Options.Test
        };

        bool? testMemory = null;
        foreach (var sms in bulk)
        {
            if (sms is null)
                throw new ArgumentNullException(nameof(sms), "One of the messages are null");

            EnsureAgreeOnTest(ref testMemory, sms.Test);

            var smsDto = new SmsDto
            {
                To = sms.To,
                Msg = sms.Text,
                Reply = (sms.ReplyAllowed || sms.ReplyTo.HasValue) ? true : null,
                From = sms.Sender ?? client.Options.Sender,
                ReplyTo = sms.ReplyTo,
                DateTime = sms.SendTime?.ToString("yyyyMMddHHmm"),
                Ref = sms.Reference
            };
            sms.Repeat?.AddProperties(smsDto);
            dto.Messages.Add(smsDto);
            dto.Test |= sms.Test;
        }

        if (dto.Messages.Count is 0)
            throw new ArgumentException("Bulk must contain at least one sms.", nameof(bulk));

        return dto;
    }

    private void EnsureAgreeOnTest(ref bool? memory, bool smsTest)
    {
        memory ??= smsTest;
        if (!Equals(memory.Value, smsTest))
            throw new ArgumentException("All messages in a bulk must agree on whether they are test messages or real messages.");
    }

    private async Task<SendResponseDto> PostAsync(SendRequestDto request, CancellationToken cancellationToken)
    {
        using var httpResponse = await client.HttpClient.PostAsJsonAsync("SMS/SendMessage", request, _requestJsonOptions, cancellationToken);

        if ((int)httpResponse.StatusCode == 429/*Too many requests*/)
            throw new SmsNotSentException("Too many concurrent requests. Sveve only allows 5 concurrent API-requests. Try again later. If the problem persists, try bulk-sending.");

        if (httpResponse.IsSuccessStatusCode is false)
            throw new SmsNotSentException($"Could not send SMS. Sveve responded {(int)httpResponse.StatusCode} {httpResponse.StatusCode}.");

        var response = await httpResponse.Content.ReadFromJsonAsync<ResponseWrapperDto<SendResponseDto>>(_responseJsonOptions, cancellationToken);
        if (response?.Response is null)
            throw new SmsNotSentException($"Sveve replied 200 OK, but no response body.");

        return response.Response;
    }

    private SendResponse CreateResponse(SendRequestDto request, SendResponseDto response)
    {
        if (response.FatalError is "Feil brukernavn/passord")
            throw new InvalidCredentialException("The configured username or password is incorrect.");

        if (!string.IsNullOrWhiteSpace(response.FatalError))
            throw new SmsNotSentException($"Could not send SMS. Sveve replied {response.FatalError}");

        var receivers = request.Messages
            .Select(sms => sms.To!)
            .SelectMany(SmsRecipient.Multiple)
            .ToList();

        var errors = (response.Errors ?? [])
            .ToDictionary(x => new SmsRecipient(x.Number), x => x.Message);

        var messageIds = new Dictionary<SmsRecipient, int>();
        if (!receivers.All(receiver => receiver.IsPhoneNumber))
        {
            // The list of receivers contains groups.
            // We do not yet nor currently plan to support getting message ids from messages sent to groups.
            // This would require looking up the group members in a separate API call.
            return new SendResponse(messageIds, errors, response.MsgOkCount, response.StdSMSCount);
        }

        var ids = response.Ids?.Where(x => x > 0).ToList();
        if (ids?.Count is not > 0)
        {
            // The response does not contain the ids of any sent messages.
            // This is still a successful response.
            client.Logger?.LogWarning("Sveve did not return any message IDs.");
            return new SendResponse(messageIds, errors, response.MsgOkCount, response.StdSMSCount);
        }

        // All receivers are phone numbers.
        // If we ignore the phone numbers with errors, the ids property contains the message ids to successfully send messages in the order the phone numbers appeared.
        var receiversWithSuccess = receivers.Where(receiver => !errors.ContainsKey(receiver)).ToList();
        if (receiversWithSuccess.Count != ids.Count)
        {
            // The number of successfully sent messages does not match the number of returned ids.
            return new SendResponse(messageIds, errors, response.MsgOkCount, response.StdSMSCount);
        }

        messageIds = receiversWithSuccess.Zip(ids, (receiver, messageId) => (receiver, messageId)).ToDictionary(x => x.receiver, x => x.messageId);
        return new SendResponse(messageIds, errors, response.MsgOkCount, response.StdSMSCount);
    }
}
