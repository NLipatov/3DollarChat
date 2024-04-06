using EthachatShared.Models.Message;

namespace Ethachat.Server.Hubs.MessageDispatcher.Handlers.ReliableMessageSender;

public interface IReliableMessageSender
{
    Task EnqueueAsync(Message message);
    void OnAck(Message syncMessage);
}