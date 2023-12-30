using LimpShared.Models.Authentication.Models.Credentials;

namespace Ethachat.Client.Services.NotificationService
{
    public interface IWebPushService
    {
        Task RequestWebPushPermission(ICredentials credentials);
        Task ResetWebPushPermission();
    }
}
