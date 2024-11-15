namespace Sveve.Send;

internal sealed class ResponseWrapperDto<T>
{
    public T? Response { get; set; }
}