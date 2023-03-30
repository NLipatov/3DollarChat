using ClientServerCommon.Models;

namespace Limp.Client.Services.HubConnectionProvider
{
    public interface IHubConnectionProvider
    {
        /// <summary>
        /// Handles hub connections and server-side calls on client methods
        /// </summary>
        /// <param name="OnUserConnectionsUpdate">Delegate invoked when server-side sends to its a clients updated List<UserConnection></param>
        /// <param name="OnConnectionIdUpdate">Delegate invoked when server-side sends a usersHub connection Id to client</param>
        /// <param name="RerenderComponent">Delegate invoked when ui component must be rerendered</param>
        /// <returns></returns>
        Task ConnectToHubs
        (Func<List<UserConnection>, Task>? OnUserConnectionsUpdate, 
        Func<string, Task>? OnConnectionIdUpdate = null, 
        Action? RerenderComponent = null);

        /// <summary>
        /// Closes connection to all hubs
        /// </summary>
        ValueTask DisposeAsync();
    }
}