using EthachatShared.Models.Message.Interfaces;

namespace Ethachat.Server.Hubs.MessageDispatcher.Handlers.ReliableMessageSender.ConcreteSenders.LongTermMessageStorage;

public interface ILongTermStorageService<T>
{
    Task SaveAsync(T data);
    Task<T[]> GetSaved(string username);
}