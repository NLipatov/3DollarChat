using ClientServerCommon.Models;
using Limp.Client.HubInteraction.EventExecution;
using Limp.Client.HubInteraction.HubObservers.Implementations.UsersHubObserver.EventTypes;
using Limp.Client.Services.ConcurrentCollectionManager;
using System.Collections.Concurrent;

namespace Limp.Client.HubInteraction.HubObservers.Implementations.UsersHubObserver;

public class UsersHubObserver : IHubObserver<UsersHubEvent>
{
    public UsersHubObserver
    (IEventCallbackExecutor eventCallbackExecutor,
    IConcurrentCollectionManager concurrentCollectionManager)
    {
        _eventCallbackExecutor = eventCallbackExecutor;
        _concurrentCollectionManager = concurrentCollectionManager;
    }
    private readonly IEventCallbackExecutor _eventCallbackExecutor;
    private readonly IConcurrentCollectionManager _concurrentCollectionManager;

    private ConcurrentDictionary<Guid, Func<string, Task>> OnConnectionIdReceived { get; set; } = new();
    private ConcurrentDictionary<Guid, Func<List<UserConnection>, Task>> OnActiveUsersReceived { get; set; } = new();
    private ConcurrentDictionary<Guid, Func<string, Task>> OnUsernameResolved { get; set; } = new();

    public Guid AddHandler<T>(UsersHubEvent eventType, T callback)
    {
        switch (eventType)
        {
            case UsersHubEvent.ConnectionIdReceived:
                return _concurrentCollectionManager.TryAddSubscription(OnConnectionIdReceived, callback as Func<string, Task>);
            case UsersHubEvent.ConnectedUsersListReceived:
                return _concurrentCollectionManager.TryAddSubscription(OnActiveUsersReceived, callback as Func<List<UserConnection>, Task>);
            case UsersHubEvent.MyUsernameResolved:
                return _concurrentCollectionManager.TryAddSubscription(OnUsernameResolved, callback as Func<string, Task>);
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
                _concurrentCollectionManager.TryRemoveSubscription(OnConnectionIdReceived, id);
            }
            if (OnActiveUsersReceived.GetValueOrDefault(id) != null)
            {
                _concurrentCollectionManager.TryRemoveSubscription(OnActiveUsersReceived, id);
            }
            if (OnUsernameResolved.GetValueOrDefault(id) != null)
            {
                _concurrentCollectionManager.TryRemoveSubscription(OnUsernameResolved, id);
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
