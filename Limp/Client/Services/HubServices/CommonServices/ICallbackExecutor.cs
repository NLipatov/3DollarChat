using Limp.Client.Services.HubServices.CommonServices.SubscriptionService.Types;
using System.Collections.Concurrent;

namespace Limp.Client.Services.HubService.CommonServices
{
    public interface ICallbackExecutor
    {
        void ExecuteCallbackDictionary<T>(T arg, ConcurrentDictionary<Guid, Action<T>> callbackCollection);
        void ExecuteCallbackDictionary<T>(T arg, ConcurrentDictionary<Guid, Func<T, Task>> callbackCollection);
        void ExecuteCallbackQueue<T>(T arg, ConcurrentQueue<Func<T, Task>> callbackCollection);
        void ExecuteSubscriptionsByName<T>(T arg, string subscriptionName);
    }
}