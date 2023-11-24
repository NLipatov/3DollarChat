using System.Text.Json;
using LimpShared.Models.Message;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;

namespace Limp.Server.Utilities.Redis;

public class UnsentMessagesRedisService : IUnsentMessagesRedisService
{
    private string ServiceAddress { get; init; }
    private string ServicePassword { get; init; }

    public UnsentMessagesRedisService(IConfiguration configuration)
    {
        ServiceAddress = configuration.GetValue<string>("Redis:Address") 
                         ?? throw new ArgumentException("Could not read an Redis address from application configuration");
    }
    
    public async Task Save(Message message)
    {
        try
        {
            using (var redis = await GetRedisConnection())
            {
                IDatabase db = redis.GetDatabase();

                var redisValue = new RedisValue(JsonSerializer.Serialize(message));

                await db.ListRightPushAsync(message.TargetGroup, redisValue);
            }
        }
        catch (Exception e)
        {
            throw new ApplicationException($"Exception point - {nameof(UnsentMessagesRedisService)}.{nameof(Save)}:" +
                                           $"Could not save message in redis: {e.Message}");
        }
    }

    public async Task<Message[]> GetSaved(string username)
    {
        try
        {
            using (var redis = await GetRedisConnection())
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

    private async Task<ConnectionMultiplexer> GetRedisConnection()
    {
        ConfigurationOptions options = new ConfigurationOptions
        {
            EndPoints = new EndPointCollection { ServiceAddress },
            Password = ServicePassword
        };
        
        return await ConnectionMultiplexer.ConnectAsync(options);
    }
}