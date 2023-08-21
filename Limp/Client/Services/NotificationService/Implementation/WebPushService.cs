using Limp.Client.HubInteraction.Handlers.Helpers;
using Limp.Client.Services.HubService.UsersService;
using Limp.Client.Services.LocalStorageService;
using Limp.Client.Services.NotificationService.Implementation.Types;
using LimpShared.Models.WebPushNotification;
using Microsoft.JSInterop;

namespace Limp.Client.Services.NotificationService.Implementation
{
    public class WebPushService : IWebPushService
    {
        private readonly IJSRuntime _jSRuntime;
        private readonly IUsersService _usersService;
        private readonly ILocalStorageService _localStorageService;

        public WebPushService(IJSRuntime jSRuntime, IUsersService usersService, ILocalStorageService localStorageService)
        {
            _jSRuntime = jSRuntime;
            _usersService = usersService;
            _localStorageService = localStorageService;
        }

        public async Task RequestWebPushPermission()
        {
            if (await IsWebPushPermissionGranted() != PushPermissionType.PROMPT)
                return;

            string? accessToken = await JWTHelper.GetAccessTokenAsync(_jSRuntime);
            if (string.IsNullOrWhiteSpace(accessToken))
                return;

            NotificationSubscriptionDTO? subscription =
                await _jSRuntime
                    .InvokeAsync<NotificationSubscriptionDTO?>("blazorPushNotifications.requestSubscription");

            if (subscription != null)
            {
                try
                {
                    subscription.AccessToken = accessToken;
                    subscription.UserAgentId = await _localStorageService.GetUserAgentIdAsync();
                    await _usersService.AddUserWebPushSubscription(subscription);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        private async Task<PushPermissionType> IsWebPushPermissionGranted()
        {
            string permissionTypeString = await _jSRuntime
                .InvokeAsync<string>("eval", "navigator.permissions.query({ name: 'push', userVisibleOnly: true }).then(result => result.state)");

            bool isTypeParsed = Enum.TryParse(permissionTypeString, true, out PushPermissionType parsedType);

            if (isTypeParsed)
                return parsedType;
            else
                return PushPermissionType.UNKNOWN;
        }

        public async Task ResetWebPushPermission()
        {
            await _jSRuntime.InvokeAsync<string>("eval", "Notification.permission = 'default'");
        }
    }
}
