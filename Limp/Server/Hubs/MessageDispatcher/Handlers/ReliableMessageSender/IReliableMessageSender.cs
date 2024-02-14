using EthachatShared.Models.Message;

namespace Ethachat.Server.Hubs.MessageDispatcher.Handlers.ReliableMessageSender;

public interface IReliableMessageSender
{
    Task Enqueue(Message message);
    void OnAckReceived(Message syncMessage);
}