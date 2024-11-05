using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Sveve.AspNetCore;

/// <summary>
/// Consumer for notifications from Sveve.
/// </summary>
public static class SveveConsumer
{
    /// <summary>
    /// All activities created by <see cref="SveveConsumer"/> are prefixed <c>"Sveve"</c>. It is also the name of the activity source.
    /// </summary>
    public const string ActivityNamePrefix = "Sveve";
    private static readonly ActivitySource ActivitySource = new (ActivityNamePrefix);

    /// <summary>
    /// Endpoint handler for consuming Sveve notifications.
    /// </summary>
    public static async Task<IResult> Endpoint(
        [FromServices] IServiceProvider services,
        [FromQuery] string number,
        [FromQuery] bool? status = null,
        [FromQuery] int? id = null,
        [FromQuery] string? msg = null,
        [FromQuery(Name = "ref")] string? refParam = null,
        [FromQuery] string? errorCode = null,
        [FromQuery] string? errorDesc = null,
        [FromQuery] string? prefix = null,
        [FromQuery] string? shortnumber = null,
        [FromQuery] string? name = null,
        [FromQuery] string? address = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(number))
            return Results.BadRequest("Missing required parameter: number");

        if (status.HasValue)
        {
            // This is a delivery report.
            // Only the delivery reports does not carry the actual message
            if (id is null)
                return Results.BadRequest("Missing required parameter: id");

            if (!services.GetRequiredServices<ISveveDeliveryConsumer>(out var deliveryConsumers))
            {
                services.GetService<ILogger>()?.LogCritical("Received SMS from Sveve, but no consumers are registered!");
                return Results.StatusCode(StatusCodes.Status500InternalServerError);
            }

            var outgoingSms = new OutgoingSms(number, id.Value, refParam);
            var error = new SmsDeliveryError(errorCode ?? "", errorDesc ?? "");

            if (status.Value)
            {
                using var source = ActivitySource.StartActivity($"{ActivityNamePrefix}.DeliveryReport.Success", ActivityKind.Server);
                using var scope = services.GetService<ILogger>()?.BeginScope("Received successful delivery report from Sveve");
                foreach (var consumer in deliveryConsumers)
                    await consumer.SmsDelivered(outgoingSms, cancellationToken);
            }
            else
            {
                using var source = ActivitySource.StartActivity($"{ActivityNamePrefix}.DeliveryReport.Failure", ActivityKind.Server);
                using var scope = services.GetService<ILogger>()?.BeginScope("Received failed delivery report from Sveve");
                foreach (var consumer in deliveryConsumers)
                    await consumer.SmsFailed(outgoingSms, error, cancellationToken);
            }

            return Results.Ok("Delivery report accepted");
        }

        if (string.IsNullOrWhiteSpace(msg))
            return Results.BadRequest("Missing required parameter: msg");

        if (!services.GetRequiredServices<ISveveSmsConsumer>(out var smsConsumers))
        {
            services.GetService<ILogger>()?.LogCritical("Received SMS from Sveve, but no consumers are registered!");
            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }

        if (!string.IsNullOrWhiteSpace(shortnumber))
        {
            using var source = ActivitySource.StartActivity($"{ActivityNamePrefix}.IncomingSms.DedicatedPhoneNumber", ActivityKind.Server);
            using var scope = services.GetService<ILogger>()?.BeginScope("Received incoming sms to dedicated phone number");
            var sms = new IncomingSmsToDedicatedPhoneNumber(shortnumber, number, msg, name, address);
            foreach (var consumer in smsConsumers)
                await consumer.SmsReceived(sms, cancellationToken);
            return Results.Ok("SMS to dedicated phone number accepted");
        }

        if (!string.IsNullOrWhiteSpace(prefix))
        {
            using var source = ActivitySource.StartActivity($"{ActivityNamePrefix}.IncomingSms.CodeWord", ActivityKind.Server);
            using var scope = services.GetService<ILogger>()?.BeginScope("Received incoming sms to code word");
            var sms = new IncomingSmsToCode(prefix, number, msg, name, address);
            foreach (var consumer in smsConsumers)
                await consumer.SmsReceived(sms, cancellationToken);
            return Results.Ok("SMS to code word accepted");
        }

        if (id.HasValue)
        {
            using var source = ActivitySource.StartActivity($"{ActivityNamePrefix}.IncomingSms.Reply", ActivityKind.Server);
            using var scope = services.GetService<ILogger>()?.BeginScope("Received incoming sms to reply");
            var sms = new IncomingSmsReply(number, id.Value, msg);
            foreach (var consumer in smsConsumers)
                await consumer.SmsReceived(sms, cancellationToken);
            return Results.Ok("Reply sms accepted");
        }

        return Results.BadRequest("Request is ambiguous. One of \"shortnumber\", \"prefix\", \"id\" or \"status\" is required to identify the notification type.");
    }

    private static bool GetRequiredServices<T>(this IServiceProvider services, out IEnumerable<T> result)
    {
        result = services.GetServices<T>().ToList();
        return result.Any();
    }
}
