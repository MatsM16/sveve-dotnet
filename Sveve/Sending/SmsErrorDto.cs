namespace Sveve.Sending;

internal sealed class SmsErrorDto
{
    public string Number { get; set; } = "";
    public string Message { get; set; } = "";
}
