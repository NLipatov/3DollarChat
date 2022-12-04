using Limp.Server.Utilities.HttpMessaging;
using Limp.Server.Utilities.Kafka;

namespace Limp.Server.Extensions
{
    public static class DIContainerExtension
    {
        public static IServiceCollection UseServerHttpClient(this IServiceCollection services) 
        {
            return services.AddScoped<IServerHttpClient, ServerHttpClient>();
        }

        public static IServiceCollection UseKafkaService(this IServiceCollection services)
        {
            services.AddHostedService<KafkaHelper>();
            return services.AddSingleton<IMessageBrokerService, KafkaHelper>();
        }
    }
}
