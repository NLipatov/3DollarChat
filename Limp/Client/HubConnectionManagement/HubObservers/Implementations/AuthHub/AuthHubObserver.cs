using ClientServerCommon.Models.Login;
using Limp.Client.HubInteraction.EventExecution;
using Limp.Client.HubInteraction.HubObservers.Implementations.AuthHub.EventTypes;
using Limp.Client.Services.ConcurrentCollectionManager;
using System.Collections.Concurrent;

namespace Limp.Client.HubInteraction.HubObservers.Implementations.AuthHub
{
    public class AuthHubObserver : IHubObserver<AuthHubEvent>
    {
        public AuthHubObserver
        (IEventCallbackExecutor eventCallbackExecutor, 
        IConcurrentCollectionManager concurrentCollectionManager)
        {
            _eventCallbackExecutor = eventCallbackExecutor;
            _concurrentCollectionManager = concurrentCollectionManager;
        }
        private readonly IEventCallbackExecutor _eventCallbackExecutor;
        private readonly IConcurrentCollectionManager _concurrentCollectionManager;

        private ConcurrentDictionary<Guid, Func<AuthResult, Task>> OnJWTPairRefresh { get; set; } = new();

        public Guid AddHandler<T>(AuthHubEvent eventType, T callback)
        {
            return _concurrentCollectionManager.TryAddSubscription(OnJWTPairRefresh, callback as Func<AuthResult, Task>);
        }

        public void RemoveHandlers(List<Guid> ids)
        {
            foreach (Guid id in ids)
                if (OnJWTPairRefresh.GetValueOrDefault(id) != null)
                {
                    _concurrentCollectionManager.TryRemoveSubscription(OnJWTPairRefresh, id);
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
