using StackExchange.Redis;

namespace Ethachat.Server.Hubs.MessageDispatcher.Handlers.ReliableMessageSender.ConcreteSenders.LongTermMessageStorage.Redis.RedisConnectionConfigurer;

public interface IRedisConnectionConfigurer
{
    Task<ConnectionMultiplexer> GetRedisConnection();
}