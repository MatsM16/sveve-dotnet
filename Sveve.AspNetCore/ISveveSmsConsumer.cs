namespace Sveve.AspNetCore;

/// <summary>
/// Implementations are notified when a SMS is received.
/// </summary>
public interface ISveveSmsConsumer
{
    /// <summary>
    /// Received a SMS as a reply to a previous SMS sent by you.
    /// </summary>
    /// <param name="sms">The received SMS.</param>
    /// <param name="cancellationToken"></param>
    Task SmsReceived(IncomingSmsReply sms, CancellationToken cancellationToken);

    /// <summary>
    /// Received a SMS sent to a configured code word.
    /// </summary>
    /// <param name="sms">The received SMS.</param>
    /// <param name="cancellationToken"></param>
    Task SmsReceived(IncomingSmsCode sms, CancellationToken cancellationToken);

    /// <summary>
    /// Received a SMS to a dedicated phone number.
    /// </summary>
    /// <param name="sms">The received SMS.</param>
    /// <param name="cancellationToken"></param>
    Task SmsReceived(IncomingSms sms, CancellationToken cancellationToken);
}
