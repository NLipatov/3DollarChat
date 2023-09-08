using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;

namespace Limp.Client.Services.HubServices.HubServices
{
    public interface IHubService
    {
        public NavigationManager NavigationManager { get; set; }
        Task<HubConnection> GetHubConnectionAsync();
        Task DisconnectAsync();
    }
}
