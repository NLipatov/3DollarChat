using Ethachat.Client.Services.HubServices.HubServices.Implementations.UsersService;
using Ethachat.Client.Services.LocalStorageService;
using EthachatShared.Models.Authentication.Models.Credentials;
using EthachatShared.Models.Authentication.Models.Credentials.Implementation;
using EthachatShared.Models.WebPushNotification;
using Microsoft.JSInterop;

namespace Ethachat.Client.Services.NotificationService.Implementation
{
    public class WebPushService : IWebPushService
    {
        private readonly IJSRuntime _jSRuntime;
        private readonly IUsersService _usersService;
        private readonly ILocalStorageService _localStorageService;

        public WebPushService
            (IJSRuntime jSRuntime, 
                IUsersService usersService, 
                ILocalStorageService localStorageService)
        {
            _jSRuntime = jSRuntime;
            _usersService = usersService;
            _localStorageService = localStorageService;
        }

        public async Task RequestWebPushPermission(ICredentials credentials)
        {
            var fcmToken = await _jSRuntime
                .InvokeAsync<string>("getFCMToken");

            if (string.IsNullOrWhiteSpace(fcmToken))
                throw new ArgumentException($"Could not get an FCM token to subsribe to notifications");
            
            var subscription = new NotificationSubscriptionDto
            {
                FirebaseRegistrationToken = fcmToken,
                UserAgentId = await _localStorageService.GetUserAgentIdAsync(),
                JwtPair = credentials as JwtPair,
                WebAuthnPair = credentials as WebAuthnPair
            };
            
            await _usersService.AddUserWebPushSubscription(subscription);
        }

        public async Task ResetWebPushPermission()
        {
            await _jSRuntime.InvokeAsync<string>("eval", "Notification.permission = 'default'");
        }
    }
}
