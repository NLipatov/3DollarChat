using System.Collections.Concurrent;

namespace Limp.Client.HubInteraction.EventExecution
{
    public interface IEventCallbackExecutor
    {
        Task ExecuteAllAsync<T>(T eventArgument, ConcurrentDictionary<Guid, Func<T, Task>> callbackDictionary);
    }
}