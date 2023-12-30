using EthachatShared.Models.Message;

namespace Limp.Server.Utilities.Redis;

public interface IUnsentMessagesRedisService
{
    Task Save(Message message);
    Task<Message[]> GetSaved(string username);
}