namespace Ethachat.Server.Hubs.MessageDispatcher.Handlers.ReliableMessageSender;

public interface IReliableMessageSender<T>
{
    Task EnqueueAsync(T message);
    void OnAck(T data);
}