namespace Ethachat.Server.WebPushNotifications
{
    public interface IWebPushSender
    {
        Task SendPush(string pushBodyText, string pushLink, string receiverUsername);
    }
}