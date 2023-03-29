using ClientServerCommon.Models;
using ClientServerCommon.Models.Login;
using Limp.Client.HubConnectionManagement.HubObservers.Implementations.MessageHub.EventTypes;
using Limp.Client.HubInteraction.EventExecution;
using Limp.Client.HubInteraction.HubObservers;
using Limp.Client.HubInteraction.HubObservers.Implementations.UsersHubObserver.EventTypes;
using Limp.Client.Services.ConcurrentCollectionManager;
using System.Collections.Concurrent;

namespace Limp.Client.HubConnectionManagement.HubObservers.Implementations.MessageHub
{
    public class MessageHubObserver : IHubObserver<MessageHubEvent>
    {
        private readonly IEventCallbackExecutor _eventCallbackExecutor;
        private readonly IConcurrentCollectionManager _concurrentCollectionManager;

        public MessageHubObserver
        (IEventCallbackExecutor eventCallbackExecutor, 
        IConcurrentCollectionManager concurrentCollectionManager)
        {
            _eventCallbackExecutor = eventCallbackExecutor;
            _concurrentCollectionManager = concurrentCollectionManager;
        }
        private ConcurrentDictionary<Guid, Func<Task>> OnAESAccept { get; set; } = new();
        public Guid AddHandler<T>(MessageHubEvent eventType, T callback)
        {
            switch (eventType)
            {
                case MessageHubEvent.AESAccept:
                    return _concurrentCollectionManager.TryAddSubscription<Func<Task>>(OnAESAccept, callback as Func<Task>);
                default:
                    throw new ApplicationException($"Unsupported {nameof(UsersHubEvent)} '{eventType}' passed in.");
            }
        }

        public Task CallHandler<T>(MessageHubEvent eventType, T parameter)
        {
            throw new NotImplementedException();
        }

        public void RemoveHandlers(List<Guid> handlerIds)
        {
            throw new NotImplementedException();
        }

        public void UnsubscriveAll()
        {
            throw new NotImplementedException();
        }
    }
}
