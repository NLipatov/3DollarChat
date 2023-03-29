using ClientServerCommon.Models;
using ClientServerCommon.Models.Message;
using Limp.Client.HubConnectionManagement.HubObservers.Implementations.MessageHub.EventTypes;
using Limp.Client.HubInteraction.EventExecution;
using Limp.Client.HubInteraction.HubObservers;
using Limp.Client.HubInteraction.HubObservers.Implementations.UsersHubObserver.EventTypes;
using Limp.Client.Services.ConcurrentCollectionManager;
using LimpShared.Encryption;
using System.Collections.Concurrent;

namespace Limp.Client.HubConnectionManagement.HubObservers.Implementations.MessageHub;

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
    private ConcurrentDictionary<Guid, Func<Message, Task>> OnAESAccept { get; set; } = new();
    private ConcurrentDictionary<Guid, Func<Message, Task>> OnMessageReceived { get; set; } = new();
    private ConcurrentDictionary<Guid, Func<List<UserConnection>, Task>> OnOnlineUsersReceived { get; set; } = new();
    private ConcurrentDictionary<Guid, Func<string, Task>> OnMessageReceivedByRecepient { get; set; } = new();
    private ConcurrentDictionary<Guid, Func<Key, Task>> OnPublicKeyReceived { get; set; } = new();
    private ConcurrentDictionary<Guid, Func<string, Task>> OnMyUsernameResolved { get; set; } = new();
    public Guid AddHandler<T>(MessageHubEvent eventType, T callback)
    {
        switch (eventType)
        {
            case MessageHubEvent.AESAccept:
                return _concurrentCollectionManager
                    .TryAddSubscription<Func<Message, Task>>
                    (OnAESAccept, callback as Func<Message, Task>);
            case MessageHubEvent.MessageReceived:
                return _concurrentCollectionManager
                    .TryAddSubscription<Func<Message, Task>>
                    (OnMessageReceived, callback as Func<Message, Task>);
            case MessageHubEvent.OnlineUsersReceived:
                return _concurrentCollectionManager
                    .TryAddSubscription<Func<List<UserConnection>, Task>>
                    (OnOnlineUsersReceived, callback as Func<List<UserConnection>, Task>);
            case MessageHubEvent.MessageReceivedByRecepient:
                return _concurrentCollectionManager
                    .TryAddSubscription(OnMessageReceivedByRecepient, callback as Func<string, Task>);
            case MessageHubEvent.PublicKeyReceived:
                return _concurrentCollectionManager
                    .TryAddSubscription(OnPublicKeyReceived, callback as Func<Key, Task>);
            case MessageHubEvent.MyUsernameResolved:
                return _concurrentCollectionManager
                    .TryAddSubscription(OnMyUsernameResolved, callback as Func<string, Task>);
            default:
                throw new ApplicationException($"Unsupported {nameof(UsersHubEvent)} '{eventType}' passed in.");
        }
    }

    public async Task CallHandler<T>(MessageHubEvent eventType, T parameter)
    {
        switch (eventType)
        {
            case MessageHubEvent.AESAccept:
                await _eventCallbackExecutor.ExecuteAllAsync(parameter as Message, OnAESAccept);
                break;
            case MessageHubEvent.MessageReceived:
                await _eventCallbackExecutor.ExecuteAllAsync(parameter as Message, OnMessageReceived);
                break;
            case MessageHubEvent.OnlineUsersReceived:
                await _eventCallbackExecutor.ExecuteAllAsync(parameter as List<UserConnection>, OnOnlineUsersReceived);
                break;
            case MessageHubEvent.MessageReceivedByRecepient:
                await _eventCallbackExecutor.ExecuteAllAsync(parameter as string, OnMessageReceivedByRecepient);
                break;
            case MessageHubEvent.PublicKeyReceived:
                await _eventCallbackExecutor.ExecuteAllAsync(parameter as Key, OnPublicKeyReceived);
                break;
            case MessageHubEvent.MyUsernameResolved:
                await _eventCallbackExecutor.ExecuteAllAsync(parameter as string, OnMyUsernameResolved);
                break;
            default:
                throw new ApplicationException($"Unsupported {nameof(UsersHubEvent)} '{eventType}' passed in.");

        }
    }

    public void RemoveHandlers(List<Guid> ids)
    {
        foreach (var id in ids)
        {
            if (OnAESAccept.GetValueOrDefault(id) != null)
            {
                _concurrentCollectionManager.TryRemoveSubscription(OnAESAccept, id);
            }
            if (OnMessageReceived.GetValueOrDefault(id) != null)
            {
                _concurrentCollectionManager.TryRemoveSubscription(OnMessageReceived, id);
            }
            if (OnOnlineUsersReceived.GetValueOrDefault(id) != null)
            {
                _concurrentCollectionManager.TryRemoveSubscription(OnOnlineUsersReceived, id);
            }
            if (OnMessageReceivedByRecepient.GetValueOrDefault(id) != null)
            {
                _concurrentCollectionManager.TryRemoveSubscription(OnMessageReceivedByRecepient, id);
            }
            if (OnPublicKeyReceived.GetValueOrDefault(id) != null)
            {
                _concurrentCollectionManager.TryRemoveSubscription(OnPublicKeyReceived, id);
            }
            if (OnMyUsernameResolved.GetValueOrDefault(id) != null)
            {
                _concurrentCollectionManager.TryRemoveSubscription(OnMyUsernameResolved, id);
            }
        }
    }

    public void UnsubscriveAll()
    {
        OnAESAccept.Clear();
        OnMessageReceived.Clear();
        OnOnlineUsersReceived.Clear();
        OnMessageReceivedByRecepient.Clear();
        OnPublicKeyReceived.Clear();
        OnMyUsernameResolved.Clear();
    }
}
