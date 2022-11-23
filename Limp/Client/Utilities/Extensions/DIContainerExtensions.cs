namespace Limp.Client.Utilities.Extensions
{
    public static class DIContainerExtensions
    {
        public static IServiceCollection UseLimpHttpClient(this IServiceCollection services)
        {
            return services.AddScoped<ILimpHttpClient, LimpHttpClient>();
        }
    }
}
