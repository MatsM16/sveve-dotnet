using System.Collections.Generic;

namespace Sveve.Send;

internal sealed class SendResponseDto
{
    public int MsgOkCount { get; set; }
    public int StdSMSCount { get; set; }
    public string? FatalError { get; set; }
    public List<int>? Ids { get; set; }
    public List<SmsErrorDto>? Errors { get; set; }
}
