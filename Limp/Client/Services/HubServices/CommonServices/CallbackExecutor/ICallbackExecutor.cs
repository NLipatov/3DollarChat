using System.Collections.Concurrent;

namespace Limp.Client.Services.HubServices.CommonServices.CallbackExecutor
{
    public interface ICallbackExecutor
    {
        void ExecuteSubscriptionsByName(string subscriptionName);
        void ExecuteSubscriptionsByName<T>(T arg, string subscriptionName);
        void ExecuteCallbackDictionary<T>(T arg, ConcurrentDictionary<Guid, Action<T>> callbackCollection);
        void ExecuteCallbackDictionary<T>(T arg, ConcurrentDictionary<Guid, Func<T, Task>> callbackCollection);
        void ExecuteCallbackQueue<T>(T arg, ConcurrentQueue<Func<T, Task>> callbackCollection);
    }
}