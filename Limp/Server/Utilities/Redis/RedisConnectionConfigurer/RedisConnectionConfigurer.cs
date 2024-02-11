using StackExchange.Redis;

namespace Ethachat.Server.Utilities.Redis.RedisConnectionConfigurer;

public class RedisConnectionConfigurer : IRedisConnectionConfigurer
{
    private string ServiceAddress { get; init; }
    private string ServicePassword { get; init; }

    public RedisConnectionConfigurer(IConfiguration configuration)
    {
        ServiceAddress = configuration.GetValue<string>("Redis:Address") 
                         ?? throw new ArgumentException("Could not read an Redis address from application configuration");
    }

    public async Task<ConnectionMultiplexer> GetRedisConnection()
    {
        ConfigurationOptions options = new ConfigurationOptions
        {
            EndPoints = new EndPointCollection { ServiceAddress },
            Password = ServicePassword
        };
        
        return await ConnectionMultiplexer.ConnectAsync(options);
    }
}