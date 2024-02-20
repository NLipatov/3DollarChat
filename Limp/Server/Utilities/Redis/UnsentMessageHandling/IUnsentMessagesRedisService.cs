using EthachatShared.Models.Message;

namespace Ethachat.Server.Utilities.Redis.UnsentMessageHandling;

public interface IUnsentMessagesRedisService
{
    Task SaveAsync(Message message);
    Task<Message[]> GetSaved(string username);
}