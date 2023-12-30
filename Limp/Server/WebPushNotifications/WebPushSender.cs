using Limp.Server.Utilities.HttpMessaging;
using EthachatShared.Models.WebPushNotification;
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
        public async Task SendPush(string pushBodyText, string pushLink, string receiverUsername)
        {
            var subscriptions = await _serverHttpClient.GetUserWebPushSubscriptionsByAccessToken(receiverUsername);

            var tasks = new List<Task>();
            foreach (var subscription in subscriptions)
            {
                tasks.Add(SendNotificationAsync(pushBodyText, pushLink, subscription));
            }

            await Task.WhenAll(tasks);
        }
        private async Task SendNotificationAsync(string message, string pushLink, NotificationSubscriptionDto notificationSubscriptionDTO)
        {
#warning ToDo: implement key generation mechanism
            var publicKey = "BA6mK_HXP2I9vXg6e4r2t_3wFwkhCh6l2THvFPqrPb1ERENvFN82VDk4pKnoHMxsd6oKGrTccX_0aLCDDFmXH00";
            var privateKey = "HZTzZSJYLrZQCG4GwWIENMh_SEo7Bziahh6H_1mJ8Eo";

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
