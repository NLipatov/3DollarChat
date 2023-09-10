using System.Text.Json;
using Limp.Client.HubInteraction.Handlers.Helpers;
using Limp.Client.Services.HubServices.HubServices.Implementations.UsersService;
using Limp.Client.Services.LocalStorageService;
using Limp.Client.Services.NotificationService.Implementation.Types;
using LimpShared.Models.WebPushNotification;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Limp.Client.Services.NotificationService.Implementation
{
    public class WebPushService : IWebPushService
    {
        private readonly IJSRuntime _jSRuntime;
        private readonly IUsersService _usersService;
        private readonly ILocalStorageService _localStorageService;
        private readonly NavigationManager _navigationManager;

        public WebPushService
            (IJSRuntime jSRuntime, 
                IUsersService usersService, 
                ILocalStorageService localStorageService,
                NavigationManager navigationManager)
        {
            _jSRuntime = jSRuntime;
            _usersService = usersService;
            _localStorageService = localStorageService;
            _navigationManager = navigationManager;
        }

        public async Task RequestWebPushPermission()
        {
            string? accessToken = await JWTHelper.GetAccessTokenAsync(_jSRuntime);
            if (string.IsNullOrWhiteSpace(accessToken))
            {
                _navigationManager.NavigateTo("signin");
                return;
            }
            
            var userDecisionOnWebPushPermission = await _jSRuntime
                .InvokeAsync<string>("eval", "window.Notification.requestPermission();");
            
            Console.WriteLine(userDecisionOnWebPushPermission);

            if (userDecisionOnWebPushPermission == "granted")
            {
                var subscription =
                    await _jSRuntime
                        .InvokeAsync<NotificationSubscriptionDto?>("blazorPushNotifications.requestSubscription");
                
                if (subscription is null)
                    throw new ArgumentException($"{nameof(subscription)} was null.");
                
                subscription.AccessToken = accessToken;
                subscription.UserAgentId = await _localStorageService.GetUserAgentIdAsync();
                await _usersService.AddUserWebPushSubscription(subscription);
            }
            // return;
            //
            // var permissionType = await IsWebPushPermissionGranted();
            // if (permissionType != PushPermissionType.PROMPT)
            // {
            //     Console.WriteLine($"Push state - {permissionType}");
            //     return;
            // }
            // Console.WriteLine($"Push state - {permissionType}");
            //
            // string? accessToken = await JWTHelper.GetAccessTokenAsync(_jSRuntime);
            // if (string.IsNullOrWhiteSpace(accessToken))
            // {
            //     Console.WriteLine($"access token was null.");
            //     return;
            // }
            //
            // NotificationSubscriptionDto? subscription =
            //     await _jSRuntime
            //         .InvokeAsync<NotificationSubscriptionDto?>("blazorPushNotifications.requestSubscription");
            //
            // if (subscription != null)
            // {
            //     try
            //     {
            //         Console.WriteLine("adding subscription to db.");
            //         subscription.AccessToken = accessToken;
            //         subscription.UserAgentId = await _localStorageService.GetUserAgentIdAsync();
            //         await _usersService.AddUserWebPushSubscription(subscription);
            //         Console.WriteLine($"permission added.");
            //     }
            //     catch (Exception ex)
            //     {
            //         Console.WriteLine(ex.Message);
            //     }
            // }
        }

        private async Task<PushPermissionType> IsWebPushPermissionGranted()
        {
            string permissionTypeString = await _jSRuntime
                .InvokeAsync<string>("eval", "navigator.permissions.query({ name: 'push', userVisibleOnly: true }).then(result => result.state)");

            Console.WriteLine("permission:" + permissionTypeString);
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
