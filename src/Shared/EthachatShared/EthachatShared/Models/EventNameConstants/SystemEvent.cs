namespace EthachatShared.Models.EventNameConstants;

public record SystemEvent<T>
{
    public SystemEventType Type { get; set; }
    public T EventValue { get; set; }
}