namespace Limp.Client.HubInteraction.EventExecution
{
    public interface IEventCallbackExecutor
    {
        Task ExecuteAllAsync<T>(T eventArgument, List<Func<T, Task>> callbacks);
    }
}