using System.Collections.Generic;
using System.Linq;

namespace Sveve;

internal class SmsReceiver
{
    private readonly string _receiver;

    internal SmsReceiver(string receiver)
    {
        _receiver = receiver.Replace(" ", "").Replace("+47", "");
        IsPhoneNumber = !_receiver.Any(char.IsLetter);
    }

    public bool IsPhoneNumber { get; }

    public string Receiver => _receiver;

    public bool IsReceiver(string receiver) => receiver.Contains(_receiver);

    public SmsResult CreateOkResult(int messageId, bool isTest) => SmsResult.Ok(_receiver, messageId, isTest);

    public static List<SmsReceiver> From(string? receivers) => receivers?.Split(',').Select(receiver => new SmsReceiver(receiver)).ToList() ?? [];

    public static bool IsSinglePhoneNumber(string? receivers)
    {
        var parsed = From(receivers);
        return parsed.Count is 1 && parsed[0].IsPhoneNumber;
    }
}