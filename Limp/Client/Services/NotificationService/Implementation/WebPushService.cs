﻿using Limp.Client.Services.AuthenticationService.Handlers;
using Limp.Client.Services.HubServices.HubServices.Implementations.UsersService;
using Limp.Client.Services.LocalStorageService;
using Limp.Client.Services.NotificationService.Implementation.Types;
using LimpShared.Models.Authentication.Models.Credentials;
using LimpShared.Models.Authentication.Models.Credentials.Implementation;
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
        private readonly IAuthenticationHandler _authenticationHandler;

        public WebPushService
            (IJSRuntime jSRuntime, 
                IUsersService usersService, 
                ILocalStorageService localStorageService,
                NavigationManager navigationManager,
                IAuthenticationHandler authenticationHandler)
        {
            _jSRuntime = jSRuntime;
            _usersService = usersService;
            _localStorageService = localStorageService;
            _navigationManager = navigationManager;
            _authenticationHandler = authenticationHandler;
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
