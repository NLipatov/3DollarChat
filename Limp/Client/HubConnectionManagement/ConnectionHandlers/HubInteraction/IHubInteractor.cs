using Microsoft.AspNetCore.SignalR.Client;

namespace Limp.Client.HubConnectionManagement.ConnectionHandlers.HubInteraction;

public interface IHubInteractor<T> : IAsyncDisposable
{
    /// <summary>
    /// Connects to a hub
    /// </summary>
    Task<HubConnection> ConnectAsync();
}
