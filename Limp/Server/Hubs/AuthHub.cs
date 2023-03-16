using AuthAPI.DTOs.User;
using ClientServerCommon.Models.Login;
using Limp.Server.Utilities.HttpMessaging;
using LimpShared.Authentification;
using LimpShared.Encryption;
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
            AuthResult result = await _serverHttpClient.Register(userDto);

            await Clients.Caller.SendAsync("OnRegister", result);
        }
        public async Task LogIn(UserDTO userDto)
        {
            var result = await _serverHttpClient.GetJWTPairAsync(userDto);

            await Clients.Caller.SendAsync("OnLoggingIn", result);
        }

        public async Task RefreshTokens(RefreshToken refreshToken)
        {
            AuthResult result = await _serverHttpClient.ExplicitJWTPairRefresh(refreshToken);

            await Clients.Caller.SendAsync("OnTokensRefresh", result);
        }
    }
}
