using EthachatShared.Models.Message;

namespace Ethachat.Server.Hubs.MessageDispatcher.Handlers.ReliableMessageSender.ConcreteSenders.LongTermMessageStorage;

public interface ILongTermMessageStorageService
{
    Task SaveAsync(Message message);
    Task<Message[]> GetSaved(string username);
}