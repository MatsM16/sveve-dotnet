using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Sveve.AspNetCore;

/// <summary>
/// Endpoint for consuming notifications from Sveve.
/// </summary>
public static class SveveEndpoint
{
    private static readonly ActivitySource ActivitySource = new ("Sveve");

    /// <summary>
    /// Endpoint handler for consuming Sveve notifications.
    /// </summary>
    internal static async Task<IResult> Endpoint(
        [FromServices] IServiceProvider services,
        [FromQuery] string? number = null,
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

            var sms = new OutgoingSms(number, id.Value, refParam);

            if (status.Value)
            {
                using var activity = ActivitySource.StartActivity($"Sveve.DeliveryReport", ActivityKind.Server);
                activity?.SetTag("sveve.delivery_status", "success");
                activity?.SetTag("sveve.reference", sms.Reference);
                return await services.ConsumeEvent<ISveveDeliveryConsumer>("delivery_report", consumer 
                    => consumer.SmsDelivered(sms, cancellationToken));
            }
            else
            {
                var error = new SmsDeliveryError(errorCode ?? "", errorDesc ?? "");
                using var activity = ActivitySource.StartActivity($"Sveve.DeliveryReport", ActivityKind.Server);
                activity?.SetTag("sveve.delivery_status", "failed");
                activity?.SetTag("sveve.reference", sms.Reference);
                activity?.SetTag("sveve.delivery_error", error.Code);
                activity?.SetTag("sveve.delivery_error_description", error.Description);
                return await services.ConsumeEvent<ISveveDeliveryConsumer>("delivery_report", consumer
                    => consumer.SmsFailed(sms, error, cancellationToken));
            }
        }

        if (string.IsNullOrWhiteSpace(msg))
            return Results.BadRequest("Missing required parameter: msg");

        if (id.HasValue)
        {
            var sms = new IncomingSmsReply(number, id.Value, msg);
            using var activity = ActivitySource.StartActivity($"Sveve.IncomingSms", ActivityKind.Server);
            activity?.AddTag("sveve.incoming_sms_type", "reply");
            activity?.AddTag("sveve.message_id", sms.MessageId);
            return await services.ConsumeEvent<ISveveSmsConsumer>("incoming_sms", consumer
                => consumer.SmsReceived(sms, cancellationToken));
        }

        if (string.IsNullOrWhiteSpace(shortnumber))
            return Results.BadRequest("The request is ambiguous. One or more required parameters are missing.");

        if (!string.IsNullOrWhiteSpace(prefix))
        {
            var sms = new IncomingSmsCode(prefix, shortnumber, number, msg, name, address);
            using var activity = ActivitySource.StartActivity($"Sveve.IncomingSms", ActivityKind.Server);
            activity?.AddTag("sveve.incoming_sms_type", "code_word");
            activity?.AddTag("sveve.code_word", sms.CodeWord);
            activity?.AddTag("sveve.receiver_phone_number", sms.ReceiverPhoneNumber);
            return await services.ConsumeEvent<ISveveSmsConsumer>("incoming_sms", consumer
                => consumer.SmsReceived(sms, cancellationToken));
        }
        else
        {
            var sms = new IncomingSms(shortnumber, number, msg, name, address);
            using var activity = ActivitySource.StartActivity($"Sveve.IncomingSms", ActivityKind.Server);
            activity?.AddTag("sveve.incoming_sms_type", "dedicated_phone_number");
            activity?.AddTag("sveve.receiver_phone_number", sms.DedicatedPhoneNumber);
            return await services.ConsumeEvent<ISveveSmsConsumer>("incoming_sms", consumer
                => consumer.SmsReceived(sms, cancellationToken));
        }
    }

    private static async Task<IResult> ConsumeEvent<TConsumer>(this IServiceProvider services, string eventType, Func<TConsumer, Task> consume)
    {
        var consumers = services.GetServices<TConsumer>().ToList();
        if (consumers.Count == 0)
        {
            // If the user has configured a callback at Sveve, but has not
            // registered any consumers, this is probably a mistake.
            // We return 500 Internal Server Error so Sveve tries again later.
            // At least the user will be notified that the event was not consumed.
            services.GetService<ILogger>()?.LogError("No consumers for Sveve {sveve.event_type} are registered. Register at least one {sveve.consumer_type}", eventType, typeof(TConsumer).Name);
            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }
        foreach (var consumer in consumers)
        {
            try
            {
                var task = consume(consumer);
                if (task is not null)
                    await task;
            }
            catch (Exception exception)
            {
                // We stop the entire consumption if any consumer fails to process.
                // This way, Sveve will try again later and at least the developers are notified.
                services.GetService<ILogger>()?.LogError(exception, "Consumer {sveve.consumer_type} failed to process {sveve.event_type}", consumer?.GetType().Name, eventType);

                // IMPORTANT: Do not leak any exception details to Sveve.
                return Results.StatusCode(StatusCodes.Status500InternalServerError);
            }
        }
        services.GetService<ILogger>()?.LogInformation("Consumed {sveve.event_type} from Sveve", eventType);
        return Results.Ok("Accepted " + eventType);
    }
}
