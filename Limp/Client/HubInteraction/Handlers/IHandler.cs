using Microsoft.AspNetCore.SignalR.Client;

namespace Limp.Client.HubInteraction.Handlers
{
    public interface IHandler<T> : IDisposable
    {
        Task<HubConnection> ConnectAsync();
    }
}
