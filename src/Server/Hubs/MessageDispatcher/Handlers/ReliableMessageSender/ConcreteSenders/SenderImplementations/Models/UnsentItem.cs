namespace Ethachat.Server.Hubs.MessageDispatcher.Handlers.ReliableMessageSender.ConcreteSenders.SenderImplementations.
    Models;

public record UnsentItem<T>
{
    public required T Item { get; set; }
    public TimeSpan Backoff { get; set; }
    public DateTime SendAfter { get; set; } = DateTime.UtcNow - TimeSpan.FromMinutes(1);
}