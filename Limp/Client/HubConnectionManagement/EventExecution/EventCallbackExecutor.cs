using System.Collections.Concurrent;

namespace Limp.Client.HubInteraction.EventExecution;

public class EventCallbackExecutor : IEventCallbackExecutor
{
    public async Task ExecuteAllAsync<T>(T eventArgument, ConcurrentDictionary<Guid, Func<T, Task>> callbackDictionary)
    {
        await Task.WhenAll(callbackDictionary.Values.Select(callback => callback(eventArgument)));
    }
}
