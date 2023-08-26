using Limp.Server.Utilities.HttpMessaging;
using LimpShared.Encryption;
using LimpShared.Models.Authentication.Models;
using LimpShared.Models.Authentication.Models.UserAuthentication;
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

        public async Task Register(UserAuthentication userDto)
        {
            AuthResult result = await _serverHttpClient.Register(userDto);

            await Clients.Caller.SendAsync("OnRegister", result);
        }
        public async Task LogIn(UserAuthentication userDto)
        {
            var result = await _serverHttpClient.GetJWTPairAsync(userDto);

            await Clients.Caller.SendAsync("OnLoggingIn", result);
        }

        public async Task IsTokenValid(string accessToken)
        {
            bool isTokenValid = await _serverHttpClient.IsAccessTokenValid(accessToken);

            await Clients.Caller.SendAsync("OnTokenValidation", isTokenValid);
        }

        public async Task RefreshTokens(RefreshTokenDto refreshToken)
        {
            AuthResult result = await _serverHttpClient.ExplicitJWTPairRefresh(refreshToken);

            await Clients.Caller.SendAsync("OnTokensRefresh", result);
        }
    }
}
