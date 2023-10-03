using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Limp.Server.Utilities.HttpMessaging;
using LimpShared.Models.WebPushNotification;

namespace Limp.Server.WebPushNotifications;

public class FirebasePushSender : IWebPushSender
{
    private readonly string _configurationPath = Path
        .Combine(AppDomain.CurrentDomain.BaseDirectory, "FCMConfiguration.json");
    
    private readonly IServerHttpClient _serverHttpClient;

    public FirebasePushSender(IServerHttpClient serverHttpClient)
    {
        _serverHttpClient = serverHttpClient;

        if (!File.Exists(_configurationPath))
            throw new ArgumentException($"FCMConfiguration.json is required and could not be found " +
                                        $"in specified path: '{_configurationPath}'.");
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
            int index = i; // Сделать копию значения i

            workload[index] = Task.Run(async () =>
            {
                var notificationMessage = new Message()
                {
                    Notification = new Notification()
                    {
                        Title = "Etha Chat",
                        Body = pushBodyText
                    },
                    Token = subscriptions[index].FirebaseRegistrationToken // Использовать скопированный index
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