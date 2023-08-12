namespace Limp.Client.Services.NotificationService
{
    public interface IWebPushService
    {
        Task RequestWebPushPermission();
        Task ResetWebPushPermission();
    }
}
