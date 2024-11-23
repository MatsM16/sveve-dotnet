namespace Sveve.Sending;

internal sealed class ResponseWrapperDto<T>
{
    public T? Response { get; set; }
}