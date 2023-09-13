using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Limp.Server.Utilities.HttpMessaging;

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
        var subscriptions = await _serverHttpClient
            .GetUserWebPushSubscriptionsByAccessToken(receiverUsername);
        
        if (!subscriptions.Any())
            return;
        
        var notificationMessage = new Message()
        {
            Notification = new Notification()
            {
                Title = "Etha Chat",
                Body = pushBodyText
            },
            Token = subscriptions.First().FirebaseRegistrationToken
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
    }
}