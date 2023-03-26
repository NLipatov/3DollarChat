using System.Collections.Concurrent;

namespace Limp.Client.HubInteraction.EventExecution
{
    public interface IEventCallbackExecutor
    {
        /// <summary>
        /// Executes all callbacks from ConcurrentDictionary
        /// </summary>
        Task ExecuteAllAsync<T>(T eventArgument, ConcurrentDictionary<Guid, Func<T, Task>> callbackDictionary);
    }
}