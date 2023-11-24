using LimpShared.Models.Authentication.Models.Credentials;

namespace Limp.Client.Services.NotificationService
{
    public interface IWebPushService
    {
        Task RequestWebPushPermission(ICredentials credentials);
        Task ResetWebPushPermission();
    }
}
