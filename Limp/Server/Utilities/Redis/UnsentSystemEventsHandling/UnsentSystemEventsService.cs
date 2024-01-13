using System.Text.Json;
using EthachatShared.Models.SystemEvents;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;

namespace Ethachat.Server.Utilities.Redis.UnsentSystemEventsHandling;

public class UnsentSystemEventsService : IUnsentSystemEventsService
{
    private readonly IRedisConnectionConfigurer _redisConnectionConfigurer;
    private const string SystemEventsCommonKey = "SystemEvents";

    public UnsentSystemEventsService(IRedisConnectionConfigurer redisConnectionConfigurer)
    {
        _redisConnectionConfigurer = redisConnectionConfigurer;
    }
    public async Task Save<T>(SystemEvent<T> systemEvent, string username)
    {
        try
        {
            using (var redis = await _redisConnectionConfigurer.GetRedisConnection())
            {
                IDatabase db = redis.GetDatabase();

                var redisValue = new RedisValue(JsonSerializer.Serialize(systemEvent));

                await db.ListRightPushAsync(SystemEventsCommonKey + username, redisValue);
            }
        }
        catch (Exception e)
        {
            throw new ApplicationException($"Could not save {nameof(systemEvent)} in redis: {e.Message}");
        }
    }

    public async Task<SystemEvent<T>[]> GetSaved<T>(string username)
    {
        try
        {
            using (var redis = await _redisConnectionConfigurer.GetRedisConnection())
            {
                IDatabase db = redis.GetDatabase();

                var values = db.ListRange(SystemEventsCommonKey + username);

                if (values.IsNullOrEmpty())
                    return Array.Empty<SystemEvent<T>>();
            
                await db.KeyDeleteAsync(username);

                SystemEvent<T>[] messages = values
                    .Where(x => !x.IsNull)
                    .Select(x => JsonSerializer.Deserialize<SystemEvent<T>>(x.ToString()))
                    .ToArray()!;
            
                return messages;
            }
        }
        catch (Exception e)
        {
            throw new ApplicationException($"Could read systemEvents from redis: {e.Message}");
        }
    }
}