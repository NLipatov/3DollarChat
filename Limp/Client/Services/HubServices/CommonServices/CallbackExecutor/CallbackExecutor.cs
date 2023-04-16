using System.Collections.Concurrent;

namespace Limp.Client.Services.HubServices.CommonServices.CallbackExecutor
{
    public class CallbackExecutor : ICallbackExecutor
    {
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
