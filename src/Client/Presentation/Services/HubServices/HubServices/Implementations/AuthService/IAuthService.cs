﻿using System.Collections.Concurrent;
using EthachatShared.Models.Authentication.Models;
using EthachatShared.Models.Authentication.Models.UserAuthentication;

namespace Ethachat.Client.Services.HubServices.HubServices.Implementations.AuthService
{
    public interface IAuthService : IHubService
    {
        ConcurrentQueue<Func<AuthResult, Task>> IsTokenValidCallbackQueue { get; set; }
        Task ValidateAccessTokenAsync(Func<AuthResult, Task> isTokenAccessValidCallback);
        Task Register(UserAuthentication newUserDto);
        Task LogIn(UserAuthentication userAuthentication);
        Task GetRefreshTokenHistory();
        void PreventReconnecting();
        Task ReconnectAsync();
    }
}
