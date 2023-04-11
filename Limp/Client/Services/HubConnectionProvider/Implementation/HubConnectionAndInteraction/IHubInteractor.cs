using Microsoft.AspNetCore.SignalR.Client;

namespace Limp.Client.Services.HubConnectionProvider.Implementation.HubInteraction;

public interface IHubInteractor<T> : IAsyncDisposable
{
    /// <summary>
    /// Connects to a hub
    /// </summary>
    Task<HubConnection> ConnectAsync();
}
