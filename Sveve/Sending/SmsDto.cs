namespace Sveve.Sending;

internal sealed class SmsDto
{
    public string To { get; set; } = "";
    public string Msg { get; set; } = "";
    public string? From { get; set; }
    public bool? Reply { get; set; }
    public int? ReplyTo { get; set; }
    public string? DateTime { get; set; }
    public string? Ref { get; set; }
    public string? Reoccurrence { get; set; }
    public string? ReoccurrenceEnds { get; set; }
    public int? EndsAfter { get; set; }
    public string? EndsOn { get; set; }
}
