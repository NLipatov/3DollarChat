namespace Limp.Server.WebPushNotifications
{
    public interface IWebPushSender
    {
        Task SendPush(string message, string pushLink, string username);
    }
}