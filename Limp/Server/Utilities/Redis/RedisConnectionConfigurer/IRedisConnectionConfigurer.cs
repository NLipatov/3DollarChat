using StackExchange.Redis;

namespace Ethachat.Server.Utilities.Redis;

public interface IRedisConnectionConfigurer
{
    Task<ConnectionMultiplexer> GetRedisConnection();
}