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

        public async Task GetTokenRefreshHistory(string accessToken)
        {
            var history = await _serverHttpClient.GetTokenRefreshHistory(accessToken);

            await Clients.Caller.SendAsync("OnRefreshTokenHistoryResponse", history);
        }

        public async Task IsTokenValid(string accessToken)
        {
            bool isTokenValid = await _serverHttpClient.IsAccessTokenValid(accessToken);

            await Clients.Caller.SendAsync("OnAuthenticationCredentialsValidated", isTokenValid);
        }
        
        public async Task IsWebAuthnCredentialsAreValid(string credentialId, uint counter)
        {
            AuthResult result = await _serverHttpClient.RefreshCredentialId(credentialId, counter);
            await Clients.Caller.SendAsync("OnAuthenticationCredentialsValidated", result.Result == AuthResultType.Success);
        }

        public async Task RefreshTokens(RefreshTokenDto refreshToken)
        {
            AuthResult result = await _serverHttpClient.ExplicitJWTPairRefresh(refreshToken);

            await Clients.Caller.SendAsync("OnTokensRefresh", result);
        }

        public async Task RefreshCredentialId(string credentialId, uint counter)
        {
            AuthResult result = await _serverHttpClient.RefreshCredentialId(credentialId, counter);
            Guid eventId = Guid.NewGuid();
            await Clients.Caller.SendAsync("OnCredentialIdRefresh", result, eventId);
        }

        public async Task GetAuthorisationServerAddress()
        {
            var address = await _serverHttpClient.GetServerAddress();
            await Clients.Caller.SendAsync("AuthorisationServerAddressResolved", address);
        }
    }
}
