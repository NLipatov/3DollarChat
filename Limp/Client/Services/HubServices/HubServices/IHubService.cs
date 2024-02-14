using Microsoft.AspNetCore.SignalR.Client;

namespace Ethachat.Client.Services.HubServices.HubServices
{
    public interface IHubService
    {
        Task<HubConnection> GetHubConnectionAsync();
    }
}
