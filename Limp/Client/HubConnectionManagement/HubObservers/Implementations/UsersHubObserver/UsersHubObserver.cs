using ClientServerCommon.Models;
using Limp.Client.HubInteraction.EventExecution;
using Limp.Client.HubInteraction.HubObservers.Implementations.UsersHubObserver.EventTypes;
using System.Collections.Concurrent;

namespace Limp.Client.HubInteraction.HubObservers.Implementations.UsersHubObserver;

public class UsersHubObserver : IHubObserver<UsersHubEvent>
{
    public UsersHubObserver(IEventCallbackExecutor eventCallbackExecutor)
    {
        _eventCallbackExecutor = eventCallbackExecutor;
    }
    private readonly IEventCallbackExecutor _eventCallbackExecutor;
    private ConcurrentDictionary<Guid, Func<string, Task>> OnConnectionIdReceived { get; set; } = new();
    private ConcurrentDictionary<Guid, Func<List<UserConnection>, Task>> OnActiveUsersReceived { get; set; } = new();
    private ConcurrentDictionary<Guid, Func<string, Task>> OnUsernameResolved { get; set; } = new();
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
        if(target != null)
        {
            bool isRemoved = dictionary.TryRemove(handlerId, out target);
            if (!isRemoved)
                TryRemoveSubscription(dictionary, handlerId);
        }
    }

    public Guid AddHandler<T>(UsersHubEvent eventType, Func<T, Task> callback)
    {
        switch (eventType)
        {
            case UsersHubEvent.ConnectionIdReceived:
                return TryAddSubscription(OnConnectionIdReceived, callback as Func<string, Task>);
            case UsersHubEvent.ConnectedUsersListReceived:
                return TryAddSubscription(OnActiveUsersReceived, callback as Func<List<UserConnection>, Task>);
            case UsersHubEvent.MyUsernameResolved:
                return TryAddSubscription(OnUsernameResolved, callback as Func<string, Task>);
            default:
                throw new ApplicationException($"Unsupported {nameof(UsersHubEvent)} '{eventType}' passed in.");
        }
    }
    public void RemoveHandlers(List<Guid> ids)
    {
        foreach (var id in ids)
        {
            if (OnConnectionIdReceived.GetValueOrDefault(id) != null)
            {
                TryRemoveSubscription(OnConnectionIdReceived, id);
            }
            if (OnActiveUsersReceived.GetValueOrDefault(id) != null)
            {
                TryRemoveSubscription(OnActiveUsersReceived, id);
            }
            if (OnUsernameResolved.GetValueOrDefault(id) != null)
            {
                TryRemoveSubscription(OnUsernameResolved, id);
            }
        }
    }
    public async Task CallHandler<T>(UsersHubEvent eventType, T parameter)
    {
        switch (eventType)
        {
            case UsersHubEvent.ConnectionIdReceived:
                await _eventCallbackExecutor.ExecuteAllAsync<string>(parameter as string, OnConnectionIdReceived);
                break;
            case UsersHubEvent.ConnectedUsersListReceived:
                await _eventCallbackExecutor.ExecuteAllAsync<List<UserConnection>>(parameter as List<UserConnection>, OnActiveUsersReceived);
                break;
            case UsersHubEvent.MyUsernameResolved:
                await _eventCallbackExecutor.ExecuteAllAsync<string>(parameter as string, OnUsernameResolved);
                break;
            default:
                throw new ApplicationException($"Unsupported {nameof(UsersHubEvent)} '{eventType}' passed in.");
        }
    }
    public void UnsubscriveAll()
    {
        OnConnectionIdReceived.Clear();
        OnActiveUsersReceived.Clear();
        OnUsernameResolved.Clear();
    }
}
