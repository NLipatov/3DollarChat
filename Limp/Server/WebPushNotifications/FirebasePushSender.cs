using Ethachat.Server.Utilities.HttpMessaging;
using EthachatShared.Models.WebPushNotification;
using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;

namespace Ethachat.Server.WebPushNotifications;

public class FirebasePushSender : IWebPushSender
{
    private readonly string _configurationPath = Path
        .Combine(AppDomain.CurrentDomain.BaseDirectory, "FirebaseCloudMessageConfiguration.json");

    private readonly IServerHttpClient _serverHttpClient;

    public FirebasePushSender(IServerHttpClient serverHttpClient)
    {
        _serverHttpClient = serverHttpClient;
        
        var rawFcmCongifJson = Environment.GetEnvironmentVariable("FCM_KEY_JSON") ?? string.Empty;
        var fcmConfigJson = Uri.UnescapeDataString(rawFcmCongifJson);
        if (!Path.Exists(_configurationPath) || File.ReadAllText(_configurationPath) != fcmConfigJson)
        {
            File.WriteAllLines(_configurationPath, new[] { fcmConfigJson });
        }
    }

    public async Task SendPush(string pushBodyText, string pushLink, string receiverUsername)
    {
        try
        {
            var subscriptions = await _serverHttpClient
                .GetUserWebPushSubscriptionsByAccessToken(receiverUsername);

            if (!subscriptions.Any())
                return;

            var sendPushesWorkload = CreateSendPushesWorkload(subscriptions, pushBodyText);
            await Task.WhenAll(sendPushesWorkload);
        }
        catch (Exception e)
        {
            throw new ApplicationException($"Cannot send a push {e.Message}.");
        }
    }

    private Task[] CreateSendPushesWorkload(NotificationSubscriptionDto[] subscriptions, string pushBodyText)
    {
        Task[] workload = new Task[subscriptions.Length];
        for (int i = 0; i < subscriptions.Length; i++)
        {
            int index = i;

            workload[index] = Task.Run(async () =>
            {
                var notificationMessage = new Message()
                {
                    Notification = new Notification()
                    {
                        Title = "η Chat",
                        Body = pushBodyText
                    },
                    Token = subscriptions[index].FirebaseRegistrationToken
                };

                if (FirebaseApp.DefaultInstance == null)
                {
                    var options = new AppOptions()
                    {
                        Credential = GoogleCredential.FromFile(_configurationPath)
                    };

                    FirebaseApp.Create(options);
                }

                var messaging = FirebaseMessaging.DefaultInstance;
                await messaging.SendAsync(notificationMessage);
            });
        }

        return workload;
    }
}