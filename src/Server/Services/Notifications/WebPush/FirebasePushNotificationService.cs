using Client.Transfer.Domain.Entities.Messages;
using Ethachat.Server.Services.Notifications.WebPush.PushDescriptionGeneration;
using Ethachat.Server.Services.Notifications.WebPush.PushDescriptionGeneration.Strategies.Implemetations;
using Ethachat.Server.Utilities.HttpMessaging;
using EthachatShared.Models.Message.ClientToClientTransferData;
using EthachatShared.Models.Message.DataTransfer;
using EthachatShared.Models.Message.Interfaces;
using EthachatShared.Models.WebPushNotification;
using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;

namespace Ethachat.Server.Services.Notifications.WebPush;

public class FirebasePushNotificationService : IWebPushNotificationService
{
    private string Configuration { get; }

    private readonly IServerHttpClient _serverHttpClient;
    private readonly IPushMessageFactory _pushMessageFactory;

    public FirebasePushNotificationService(IServerHttpClient serverHttpClient)
    {
        _serverHttpClient = serverHttpClient;
        
        var rawFcmConfigurationJson = Environment.GetEnvironmentVariable("FCM_KEY_JSON") ?? string.Empty;
        var fcmConfigJson = Uri.UnescapeDataString(rawFcmConfigurationJson);
        Configuration = fcmConfigJson;

        _pushMessageFactory = new PushMessageFactory();
        _pushMessageFactory.RegisterStrategy<TextMessage>(new TextStrategy());
        _pushMessageFactory.RegisterStrategy<HlsPlaylistMessage>(new HlsStrategy());
        _pushMessageFactory.RegisterStrategy<Package>(new PackageStrategy());
    }

    public async Task SendAsync<T>(T itemToNotifyAbout) where T : IHasInnerDataType, ISourceResolvable, IDestinationResolvable
    {
        if (string.IsNullOrWhiteSpace(Configuration))
            return;
        
        try
        {
            var pushMessageStrategy = _pushMessageFactory.GetItemStrategy(itemToNotifyAbout.DataType);
            var pushMessageCommand = pushMessageStrategy.CreateCommand(itemToNotifyAbout);
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
                    var options = new AppOptions
                    {
                        Credential = GoogleCredential.FromJson(Configuration)
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