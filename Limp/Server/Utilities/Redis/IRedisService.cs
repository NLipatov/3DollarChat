using LimpShared.Models.Message;

namespace Limp.Server.Utilities.Redis;

public interface IRedisService
{
    Task Save(Message message);
    Task<Message[]> GetSaved(string username);
}