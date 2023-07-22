using Limp.Client.HubInteraction.Handlers.Helpers;
using Limp.Client.Services.HubService.UsersService;
using LimpShared.Models.WebPushNotification;
using Microsoft.JSInterop;

namespace Limp.Client.Services.NotificationService.Implementation
{
    public class WebPushService : IWebPushService
    {
        private readonly IJSRuntime _jSRuntime;
        private readonly IUsersService _usersService;

        public WebPushService(IJSRuntime jSRuntime, IUsersService usersService)
        {
            _jSRuntime = jSRuntime;
            _usersService = usersService;
        }

        public async Task RequestWebPushPermission()
        {
            if (await IsWebPushPermissionGranted())
                return;

            string? accessToken = await JWTHelper.GetAccessTokenAsync(_jSRuntime);
            if (string.IsNullOrWhiteSpace(accessToken))
                throw new ApplicationException
                ("Could not subscribe to web push notifications because access token was empty string. Relogin needed.");

            NotificationSubscriptionDTO? subscription =
                await _jSRuntime.InvokeAsync<NotificationSubscriptionDTO>("blazorPushNotifications.requestSubscription");

            if (subscription != null)
            {
                try
                {
                    subscription.AccessToken = accessToken;
                    await _usersService.AddUserWebPushSubscription(subscription);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        private async Task<bool> IsWebPushPermissionGranted()
            => await _jSRuntime.InvokeAsync<string>("eval", "Notification.permission") == "granted";

        public async Task ResetWebPushPermission()
        {
            await _jSRuntime.InvokeAsync<string>("eval", "Notification.permission = 'default'");
        }
    }
}
