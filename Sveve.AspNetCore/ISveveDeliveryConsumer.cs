﻿namespace Sveve.AspNetCore;

/// <summary>
/// Implementations are notified when a SMS delivery succeeds or fails.
/// </summary>
public interface ISveveDeliveryConsumer
{
    /// <summary>
    /// The delivery of <paramref name="deliveredSms"/> was successful.
    /// </summary>
    /// <param name="deliveredSms">The sent SMS that was delivered successfully.</param>
    /// <param name="cancellationToken"></param>
    Task SmsDelivered(OutgoingSms deliveredSms, CancellationToken cancellationToken);

    /// <summary>
    /// The delivery of <paramref name="failedSms"/> failed.
    /// </summary>
    /// <param name="failedSms">The sent SMS that was not delivered.</param>
    /// <param name="error">The reason for failing to deliver.</param>
    /// <param name="cancellationToken"></param>
    Task SmsFailed(OutgoingSms failedSms, SmsDeliveryError error, CancellationToken cancellationToken);
}
