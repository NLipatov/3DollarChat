using EthachatShared.Models.Message;

namespace Ethachat.Server.Hubs.MessageDispatcher.Handlers.ReliableMessageSender;

public interface IReliableMessageSender
{
    void Enqueue(Message message);
    void OnAckReceived(Guid messageId, string targetGroup);
}