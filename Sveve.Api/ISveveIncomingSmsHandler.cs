namespace Sveve.Api;

/// <summary>
/// Implementations are notified when a SMS is received.
/// </summary>
public interface ISveveIncomingSmsHandler
{
    /// <summary>
    /// Received a SMS as a reply to a previous SMS sent by you.
    /// </summary>
    /// <param name="reply">The received SMS.</param>
    /// <param name="cancellationToken"></param>
    Task OnReply(ReplySms reply, CancellationToken cancellationToken);

    /// <summary>
    /// Received a SMS sent to a configured code word.
    /// </summary>
    /// <param name="sms">The received SMS.</param>
    /// <param name="cancellationToken"></param>
    Task OnSmsToCode(CodeSms sms, CancellationToken cancellationToken);

    /// <summary>
    /// Received a SMS to a dedicated phone number.
    /// </summary>
    /// <param name="sms">The received SMS.</param>
    /// <param name="cancellationToken"></param>
    Task OnSmsToDedicatedPhoneNumber(DedicatedPhoneNumberSms sms, CancellationToken cancellationToken);
}
