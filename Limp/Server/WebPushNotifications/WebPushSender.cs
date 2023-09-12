using Limp.Server.Utilities.HttpMessaging;
using LimpShared.Models.WebPushNotification;
using System.Text.Json;
using WebPush;

namespace Limp.Server.WebPushNotifications
{
    public class WebPushSender : IWebPushSender
    {
        private readonly IServerHttpClient _serverHttpClient;
        public WebPushSender(IServerHttpClient serverHttpClient)
        {
            _serverHttpClient = serverHttpClient;
        }
        public async Task SendPush(string message, string pushLink, string username)
        {
            var subscriptions = await _serverHttpClient.GetUserWebPushSubscriptionsByAccessToken(username);

            var tasks = new List<Task>();
            foreach (var subscription in subscriptions)
            {
                tasks.Add(SendNotificationAsync(message, pushLink, subscription));
            }

            await Task.WhenAll(tasks);
        }
        private async Task SendNotificationAsync(string message, string pushLink, NotificationSubscriptionDto notificationSubscriptionDTO)
        {
#warning ToDo: implement key generation mechanism
            var publicKey = "BNCAFN3E0iLenjyBVNZ0Tlm87nhPCyFpfgdxPlURSy0FVds5mapFIeUC5f2XKn7guanHBsVvyh6GpcXH1JU-1pE";
            var privateKey = "XpxNewItAFg34Q0uOTm-jUVTgB5b47-sxiV3JxGAbTA";

            var pushSubscription = new PushSubscription
                (notificationSubscriptionDTO.Url, notificationSubscriptionDTO.P256dh, notificationSubscriptionDTO.Auth);

            var vapidDetails = new VapidDetails("mailto:admin@ethacore.com", publicKey, privateKey);

            var webPushClient = new WebPushClient();
            try
            {
                var payload = JsonSerializer.Serialize(new
                {
                    message,
                    //This will redirect user to specified url
                    url = $"{pushLink}",
                });
                await webPushClient.SendNotificationAsync(pushSubscription, payload, vapidDetails);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Error sending push notification: " + ex.Message);
            }
        }
    }
}
