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
            var publicKey = "BLo2h1C7fbDB8e0DFxzqB1JcEqTF6js2UbvOQNFS0cFe6TDjpVPTqHm4qYVGtWIGnVGLabR-EjXLbLkHQIpV3eQ";
            var privateKey = "XqDHebLY7jfmHS0l4ZhXdpodDH2X-s719FOMFR81Irg";

            var pushSubscription = new PushSubscription
                (notificationSubscriptionDTO.Url, notificationSubscriptionDTO.P256dh, notificationSubscriptionDTO.Auth);

            var vapidDetails = new VapidDetails("mailto:admin@ethacore.com", publicKey, privateKey);

            var webPushClient = new WebPushClient();
            try
            {
                var payload = JsonSerializer.Serialize(new
                {
                    body = message,
                    //This will redirect user to specified url
                    url = $"{pushLink}",
                    tag = Guid.NewGuid().ToString()
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
