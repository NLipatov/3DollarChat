using Limp.Client.Services.HubServices.CommonServices.SubscriptionService;
using Limp.Client.Services.HubServices.CommonServices.SubscriptionService.Types;
using System.Collections.Concurrent;

namespace Limp.Client.Services.HubServices.CommonServices.CallbackExecutor
{
    public class CallbackExecutor : ICallbackExecutor
    {
        private readonly IHubServiceSubscriptionManager _hubServiceSubscriptionManager;

        public CallbackExecutor(IHubServiceSubscriptionManager hubServiceSubscriptionManager)
        {
            _hubServiceSubscriptionManager = hubServiceSubscriptionManager;
        }
        public void ExecuteSubscriptionsByName<T>
        (T arg,
        string subscriptionName)
        {
            List<Subscription> targetSubscriptions = _hubServiceSubscriptionManager.GetSubscriptionsByName(subscriptionName);
            foreach (var subscription in targetSubscriptions)
            {
                object? callback = subscription?.Callback?.Delegate;
                Type? callbackType = callback?.GetType();
                if(callbackType == null)
                    throw new ArgumentNullException($"Could not resolve callback type.");

                if(callbackType.IsAssignableTo(typeof(Func<T, Task>)))
                {
                    Func<T, Task> methodToInvoke = CastCallback<Func<T, Task>>(callback);
                    methodToInvoke.Invoke(arg);
                    return;
                }
                else if(callbackType.IsAssignableTo(typeof(Action<T>)))
                {
                    Action<T> methodToInvoke = CastCallback<Action<T>>(callback);
                    methodToInvoke.Invoke(arg);
                    return;
                }

                throw new ArgumentException($"Could not handle an callback of type: {nameof(callbackType)}");
            }
        }

        private T CastCallback<T>(object? callback) where T : Delegate
        {
            if (callback == null)
                throw new ArgumentException("Callback is null");

            T? methodToInvoke = callback as T;
            if (methodToInvoke == null)
                throw new ArgumentException($"Could not convert an callback object to specified Delegate type");

            return methodToInvoke;
        }
        /// <summary>
        /// Executes each one callback in dictionary values without removing callback from dictionary
        /// </summary>
        public void ExecuteCallbackDictionary<T>
        (T arg,
        ConcurrentDictionary<Guid, Func<T, Task>> callbackCollection)
        {
            foreach (var callback in callbackCollection.Values)
            {
                callback(arg);
            }
        }

        public void ExecuteCallbackDictionary<T>
        (T arg,
        ConcurrentDictionary<Guid, Action<T>> callbackCollection)
        {
            foreach (var callback in callbackCollection.Values)
            {
                callback(arg);
            }
        }

        /// <summary>
        /// Executes each one callback in queue and removes callback from queue
        /// </summary>
        public void ExecuteCallbackQueue<T>
        (T arg,
        ConcurrentQueue<Func<T, Task>> callbackCollection)
        {
            foreach (var callback in callbackCollection)
            {
                callback(arg);
            }

            Func<T, Task>? callbackToDequeue;
            callbackCollection.TryDequeue(out callbackToDequeue);
        }
    }
}
