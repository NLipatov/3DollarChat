using System.Net.Mime;
using Ethachat.Server.Services.Notifications.WebPush.PushDescriptionGeneration;
using Ethachat.Server.Services.Notifications.WebPush.PushDescriptionGeneration.Strategies.Strategies;
using Ethachat.Server.Utilities.HttpMessaging;
using EthachatShared.Models.Message;
using EthachatShared.Models.Message.ClientToClientTransferData;
using EthachatShared.Models.Message.Interfaces;
using EthachatShared.Models.WebPushNotification;
using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Message = FirebaseAdmin.Messaging.Message;

namespace Ethachat.Server.Services.Notifications.WebPush;

public class FirebasePushNotificationService : IWebPushNotificationService
{
    private string configuration { get; } = string.Empty;

    private readonly IServerHttpClient _serverHttpClient;
    private readonly IPushMessageFactory _pushMessageFactory;

    public FirebasePushNotificationService(IServerHttpClient serverHttpClient)
    {
        _serverHttpClient = serverHttpClient;
        
        var rawFcmCongifJson = Environment.GetEnvironmentVariable("FCM_KEY_JSON") ?? string.Empty;
        var fcmConfigJson = Uri.UnescapeDataString(rawFcmCongifJson);
        configuration = fcmConfigJson;

        _pushMessageFactory = new PushMessageFactory();
        _pushMessageFactory.RegisterStrategy<TextMessage>(new TextMessageStrategy());
    }

    public async Task SendAsync<T>(T itemToNotifyAbout) where T : ISourceResolvable, IDestinationResolvable
    {
        try
        {
            var pushMessageStrategy = _pushMessageFactory.GetItemStrategy((itemToNotifyAbout as EncryptedDataTransfer).DataType);
            var pushMessageCommand = pushMessageStrategy.Process(itemToNotifyAbout);
            if (!pushMessageCommand.IsSendRequired)
                return;
            
            var subscriptions = await _serverHttpClient
                .GetUserWebPushSubscriptionsByAccessToken(itemToNotifyAbout.Target);

            if (!subscriptions.Any())
                return;

            var sendPushesWorkload = CreateSendPushesWorkload(subscriptions, pushMessageCommand.PushMessage);
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
                        Credential = GoogleCredential.FromJson(configuration)
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