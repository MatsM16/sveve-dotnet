using System.Collections.Generic;
using System.Linq;

namespace Sveve;

internal class SmsReceiver
{
    private readonly string _receiver;

    private SmsReceiver(string receiver)
    {
        _receiver = receiver.Replace(" ", "");
        IsPhoneNumber = !_receiver.Any(char.IsLetter);
    }

    public bool IsPhoneNumber { get; }

    public bool IsReceiver(string receiver) => receiver.Contains(_receiver);

    public SendSmsResult CreateOkSendResponse(int messageId, bool isTest) => SendSmsResult.Ok(_receiver, messageId, isTest);

    public static List<SmsReceiver> From(string? receivers) => receivers?.Split(',').Select(receiver => new SmsReceiver(receiver)).ToList() ?? [];
}
