using ClientServerCommon.Models.Login;
using Limp.Client.HubInteraction.EventExecution;
using Limp.Client.HubInteraction.HubObservers.Implementations.AuthHub.EventTypes;
using System.Collections.Concurrent;

namespace Limp.Client.HubInteraction.HubObservers.Implementations.AuthHub
{
    public class AuthHubObserver : IHubObserver<AuthHubEvent>
    {
        public AuthHubObserver(IEventCallbackExecutor eventCallbackExecutor)
        {
            _eventCallbackExecutor = eventCallbackExecutor;
        }
        private readonly IEventCallbackExecutor _eventCallbackExecutor;
        private ConcurrentDictionary<Guid, Func<AuthResult, Task>> OnJWTPairRefresh { get; set; } = new();
        private Guid TryAddSubscription<T>(ConcurrentDictionary<Guid, Func<T, Task>> dictionary, Func<T, Task> callback)
        {
            Guid handlerId = Guid.NewGuid();
            Func<T, Task> callbackFunc = callback;
            bool isAdded = dictionary.TryAdd(handlerId, callbackFunc);
            if (!isAdded)
                TryAddSubscription(dictionary, callback);

            return handlerId;
        }
        private void TryRemoveSubscription<T>(ConcurrentDictionary<Guid, Func<T, Task>> dictionary, Guid handlerId)
        {
            Func<T, Task>? target = dictionary.GetValueOrDefault(handlerId);
            if (target != null)
            {
                bool isRemoved = dictionary.TryRemove(handlerId, out target);
                if (!isRemoved)
                    TryRemoveSubscription(dictionary, handlerId);
            }
        }

        public Guid AddHandler<T>(AuthHubEvent eventType, Func<T, Task> callback)
        {
            return TryAddSubscription(OnJWTPairRefresh, callback as Func<AuthResult, Task>);
        }

        public void RemoveHandlers(List<Guid> ids)
        {
            foreach (Guid id in ids)
                if (OnJWTPairRefresh.GetValueOrDefault(id) != null)
                {
                    TryRemoveSubscription(OnJWTPairRefresh, id);
                }
        }
        public async Task CallHandler<T>(AuthHubEvent eventType, T parameter)
        {
            await _eventCallbackExecutor.ExecuteAllAsync(parameter as AuthResult, OnJWTPairRefresh);
        }
        public void UnsubscriveAll()
        {
            OnJWTPairRefresh.Clear();
        }
    }
}
