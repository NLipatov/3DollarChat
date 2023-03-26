using Microsoft.AspNetCore.SignalR.Client;

namespace Limp.Client.HubInteraction.Handlers
{
    public interface IHandler<T> : IAsyncDisposable
    {
        Task<HubConnection> ConnectAsync();
    }
}
