using Ethachat.Server.Utilities.HttpMessaging;
using EthachatShared.Models.Authentication.Models;
using EthachatShared.Models.Authentication.Models.Credentials.CredentialsDTO;
using EthachatShared.Models.Authentication.Models.UserAuthentication;
using EthachatShared.Models.Message;
using Microsoft.AspNetCore.SignalR;
using SharedServices;

namespace Ethachat.Server.Hubs
{
    public class AuthHub : Hub
    {
        private readonly IServerHttpClient _serverHttpClient;
        private readonly ISerializerService _serializerService;

        public AuthHub(IServerHttpClient serverHttpClient, ISerializerService serializerService)
        {
            _serverHttpClient = serverHttpClient;
            _serializerService = serializerService;
        }

        public async Task Register(ClientToServerData data)
        {
            await Clients.All.SendAsync("OnClientToServerDataAck", data.Id);
            var userDto = await _serializerService.DeserializeAsync<UserAuthentication>(data.Data);
            AuthResult result = await _serverHttpClient.Register(userDto);

            await Clients.Caller.SendAsync("OnRegister", result);
        }
        public async Task LogIn(ClientToServerData data)
        {
            await Clients.All.SendAsync("OnClientToServerDataAck", data.Id);
            var userDto = await _serializerService.DeserializeAsync<UserAuthentication>(data.Data);
            var result = await _serverHttpClient.GetJWTPairAsync(userDto);

            await Clients.Caller.SendAsync("OnLoggingIn", result);
        }

        public async Task GetTokenRefreshHistory(ClientToServerData data)
        {
            await Clients.All.SendAsync("OnClientToServerDataAck", data.Id);
            var accessToken = await _serializerService.DeserializeAsync<string>(data.Data);
            var history = await _serverHttpClient.GetTokenRefreshHistory(accessToken);

            await Clients.Caller.SendAsync("OnRefreshTokenHistoryResponse", history);
        }

        public async Task ValidateCredentials(ClientToServerData data)
        {
            await Clients.All.SendAsync("OnClientToServerDataAck", data.Id);
            var dto = await _serializerService.DeserializeAsync<CredentialsDTO>(data.Data);
            AuthResult result = await _serverHttpClient.ValidateCredentials(dto);

            await Clients.Caller.SendAsync("OnValidateCredentials", result);
        }

        public async Task RefreshCredentials(ClientToServerData data)
        {
            await Clients.All.SendAsync("OnClientToServerDataAck", data.Id);
            var dto = await _serializerService.DeserializeAsync<CredentialsDTO>(data.Data);
            AuthResult result = await _serverHttpClient.RefreshCredentials(dto);
            
            await Clients.Caller.SendAsync("OnRefreshCredentials", result);
        }
    }
}
