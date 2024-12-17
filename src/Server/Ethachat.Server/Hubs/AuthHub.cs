using Ethachat.Client.Extensions;
using Ethachat.Server.Utilities.HttpMessaging;
using EthachatShared.Models.Authentication.Models;
using EthachatShared.Models.Authentication.Models.Credentials.CredentialsDTO;
using EthachatShared.Models.Authentication.Models.UserAuthentication;
using EthachatShared.Models.Message;
using Microsoft.AspNetCore.SignalR;

namespace Ethachat.Server.Hubs
{
    public class AuthHub : Hub
    {
        private readonly IServerHttpClient _serverHttpClient;
        public AuthHub(IServerHttpClient serverHttpClient)
        {
            _serverHttpClient = serverHttpClient;
        }

        public async Task Register(ClientToServerData data)
        {
            await Clients.All.SendAsync("OnClientToServerDataAck", data.Id);
            var userDto = await data.Data.DeserializeAsync<UserAuthentication>();
            AuthResult result = await _serverHttpClient.Register(userDto);

            await Clients.Caller.SendAsync("OnRegister", result);
        }
        public async Task LogIn(ClientToServerData data)
        {
            await Clients.All.SendAsync("OnClientToServerDataAck", data.Id);
            var userDto = await data.Data.DeserializeAsync<UserAuthentication>();
            var result = await _serverHttpClient.GetJWTPairAsync(userDto);

            await Clients.Caller.SendAsync("OnLoggingIn", result);
        }

        public async Task GetTokenRefreshHistory(ClientToServerData data)
        {
            await Clients.All.SendAsync("OnClientToServerDataAck", data.Id);
            var accessToken = await data.Data.DeserializeAsync<string>();
            var history = await _serverHttpClient.GetTokenRefreshHistory(accessToken);

            await Clients.Caller.SendAsync("OnRefreshTokenHistoryResponse", history);
        }

        public async Task ValidateCredentials(ClientToServerData data)
        {
            await Clients.All.SendAsync("OnClientToServerDataAck", data.Id);
            var dto = await data.Data.DeserializeAsync<CredentialsDTO>();
            AuthResult result = await _serverHttpClient.ValidateCredentials(dto);

            await Clients.Caller.SendAsync("OnValidateCredentials", result);
        }

        public async Task RefreshCredentials(ClientToServerData data)
        {
            await Clients.All.SendAsync("OnClientToServerDataAck", data.Id);
            var dto = await data.Data.DeserializeAsync<CredentialsDTO>();
            AuthResult result = await _serverHttpClient.RefreshCredentials(dto);
            
            await Clients.Caller.SendAsync("OnRefreshCredentials", result);
        }
    }
}
