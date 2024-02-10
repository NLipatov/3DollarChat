using StackExchange.Redis;

namespace Ethachat.Server.Utilities.Redis.RedisConnectionConfigurer;

public interface IRedisConnectionConfigurer
{
    Task<ConnectionMultiplexer> GetRedisConnection();
}