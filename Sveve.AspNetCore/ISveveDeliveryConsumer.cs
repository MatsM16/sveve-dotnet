namespace Sveve.AspNetCore;

/// <summary>
/// Implementations are notified when a SMS delivery succeeds or fails.
/// </summary>
public interface ISveveDeliveryConsumer
{
    /// <summary>
    /// The delivery of <paramref name="sms"/> was successful.
    /// </summary>
    /// <param name="sms">The sent SMS that was delivered successfully.</param>
    /// <param name="cancellationToken"></param>
    Task SmsDelivered(OutgoingSms sms, CancellationToken cancellationToken);

    /// <summary>
    /// The delivery of <paramref name="sms"/> failed.
    /// </summary>
    /// <param name="sms">The sent SMS that was not delivered.</param>
    /// <param name="error">The reason for failing to deliver.</param>
    /// <param name="cancellationToken"></param>
    Task SmsFailed(OutgoingSms sms, SmsDeliveryError error, CancellationToken cancellationToken);
}
