using AuthAPI.DTOs.User;
using Limp.Server.Utilities.HttpMessaging;
using Limp.Shared.Models.Login;
using LimpShared.Authentification;
using Microsoft.AspNetCore.SignalR;

namespace Limp.Server.Hubs
{
    public class AuthHub : Hub
    {
        private readonly IServerHttpClient _serverHttpClient;
        public AuthHub(IServerHttpClient serverHttpClient)
        {
            _serverHttpClient = serverHttpClient;
        }

        public async Task Register(UserDTO userDto)
        {
            LogInResult result = await _serverHttpClient.Register(userDto);

            await Clients.Caller.SendAsync("OnRegister", result);
        }
        public async Task LogIn(UserDTO userDto)
        {
            var result = await _serverHttpClient.GetJWTPairAsync(userDto);

            await Clients.Caller.SendAsync("OnLoggingIn", result);
        }

        public async Task RefreshTokens(RefreshToken refreshToken)
        {
            LogInResult result = await _serverHttpClient.ExplicitJWTPairRefresh(refreshToken);

            await Clients.Caller.SendAsync("OnTokensRefresh", result);
        }
    }
}
