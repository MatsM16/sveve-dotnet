using System.Collections.Generic;

namespace Sveve.Sending;

internal sealed class SendRequestDto
{
    public string User { get; set; } = "";
    public string Passwd { get; set; } = "";
    public string? F { get; } = "json";
    public bool Test { get; set; }
    public List<SmsDto> Messages { get; } = [];
}
