namespace Limp.Client.HubInteraction.EventExecution
{
    public class EventCallbackExecutor : IEventCallbackExecutor
    {
        public async Task ExecuteAllAsync<T>(T eventArgument, List<Func<T, Task>> callbacks)
        {
            await Task.WhenAll(callbacks.Select(callback => callback(eventArgument)));
        }
    }
}
