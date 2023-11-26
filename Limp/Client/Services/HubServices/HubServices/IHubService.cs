using Microsoft.AspNetCore.SignalR.Client;

namespace Limp.Client.Services.HubServices.HubServices
{
    public interface IHubService
    {
        Task<HubConnection> GetHubConnectionAsync();

        bool IsConnected();
    }
}
