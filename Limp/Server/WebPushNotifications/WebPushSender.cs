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
        private async Task SendNotificationAsync(string message, string pushLink, NotificationSubscriptionDTO notificationSubscriptionDTO)
        {
#warning ToDo: implement key generation mechanism
            // For a real application, generate your own
            var publicKey = "BLC8GOevpcpjQiLkO7JmVClQjycvTCYWm6Cq_a7wJZlstGTVZvwGFFHMYfXt6Njyvgx_GlXJeo5cSiZ1y4JOx1o";
            var privateKey = "OrubzSz3yWACscZXjFQrrtDwCKg-TGFuWhluQ2wLXDo";

            var pushSubscription = new PushSubscription
                (notificationSubscriptionDTO.Url, notificationSubscriptionDTO.P256dh, notificationSubscriptionDTO.Auth);

            var vapidDetails = new VapidDetails("mailto:<someone@example.com>", publicKey, privateKey);

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
