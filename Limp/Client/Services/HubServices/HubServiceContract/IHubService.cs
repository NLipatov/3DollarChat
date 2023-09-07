using Microsoft.AspNetCore.SignalR.Client;

namespace Limp.Client.Services.HubServices.HubServiceContract
{
    public interface IHubService
    {
        Task<HubConnection> ConnectAsync();
        Task DisconnectAsync();
    }
}
