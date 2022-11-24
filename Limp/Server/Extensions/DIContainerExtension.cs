using Limp.Server.Utilities.HttpMessaging;

namespace Limp.Server.Extensions
{
    public static class DIContainerExtension
    {
        public static IServiceCollection UseServerHttpClient(this IServiceCollection services) 
        {
            return services.AddScoped<IServerHttpClient, ServerHttpClient>();
        }
    }
}
