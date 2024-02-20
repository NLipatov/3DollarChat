using System.Text.Json;
using Ethachat.Server.Utilities.Redis.RedisConnectionConfigurer;
using EthachatShared.Models.Message;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;

namespace Ethachat.Server.Utilities.Redis.UnsentMessageHandling;

public class UnsentMessagesRedisService : IUnsentMessagesRedisService
{
    private readonly IRedisConnectionConfigurer _redisConnectionConfigurer;

    public UnsentMessagesRedisService(IRedisConnectionConfigurer redisConnectionConfigurer)
    {
        _redisConnectionConfigurer = redisConnectionConfigurer;
    }
    
    public async Task SaveAsync(Message message)
    {
        try
        {
            using (var redis = await _redisConnectionConfigurer.GetRedisConnection())
            {
                IDatabase db = redis.GetDatabase();

                var redisValue = new RedisValue(JsonSerializer.Serialize(message));

                await db.ListRightPushAsync(message.TargetGroup, redisValue);
            }
        }
        catch (Exception e)
        {
            throw new ApplicationException($"Exception point - {nameof(UnsentMessagesRedisService)}.{nameof(SaveAsync)}:" +
                                           $"Could not save message in redis: {e.Message}");
        }
    }

    public async Task<Message[]> GetSaved(string username)
    {
        try
        {
            using (var redis = await _redisConnectionConfigurer.GetRedisConnection())
            {
                IDatabase db = redis.GetDatabase();

                var values = db.ListRange(username);

                if (values.IsNullOrEmpty())
                    return Array.Empty<Message>();
            
                await db.KeyDeleteAsync(username);

                Message[] messages = values
                    .Where(x => !x.IsNull)
                    .Select(x => JsonSerializer.Deserialize<Message>(x.ToString()))
                    .ToArray()!;
            
                return messages;
            }
        }
        catch (Exception e)
        {
            throw new ApplicationException($"Could read messages from redis: {e.Message}");
        }
    }
}