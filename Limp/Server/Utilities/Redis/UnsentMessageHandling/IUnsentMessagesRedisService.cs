using EthachatShared.Models.Message;

namespace Ethachat.Server.Utilities.Redis.UnsentMessageHandling;

public interface IUnsentMessagesRedisService
{
    Task Save(Message message);
    Task<Message[]> GetSaved(string username);
}