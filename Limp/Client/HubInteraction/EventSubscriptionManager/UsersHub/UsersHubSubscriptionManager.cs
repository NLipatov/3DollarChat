using ClientServerCommon.Models;
using Limp.Client.HubInteraction.EventExecution;
using Limp.Client.HubInteraction.EventSubscriptionManager.UsersHub.Contract;
using Limp.Client.HubInteraction.EventSubscriptionManager.UsersHub.EventTypes;

namespace Limp.Client.HubInteraction.EventSubscriptionManager.UsersHub;

public class UsersHubSubscriptionManager : IUsersHubSubscriptionManager
{
    private readonly IEventCallbackExecutor _eventCallbackExecutor;

    public UsersHubSubscriptionManager(IEventCallbackExecutor eventCallbackExecutor)
    {
        _eventCallbackExecutor = eventCallbackExecutor;
    }
    private List<Func<string, Task>> OnConnectionIdReceived { get; set; } = new();
    private List<Func<List<UserConnections>, Task>> OnUsersConnectionsReceived { get; set; } = new();
    private List<Func<string, Task>> OnUsernameResolved { get; set; } = new();

    public void AddHandler<T>(UserHubEventType eventType, Func<T, Task> callback)
    {
        switch (eventType)
        {
            case UserHubEventType.ConnectionIdReceived:
                OnConnectionIdReceived.Add(callback as Func<string, Task>);
                break;
            case UserHubEventType.ConnectedUsersListReceived:
                OnUsersConnectionsReceived.Add(callback as Func<List<UserConnections>, Task>);
                break;
            case UserHubEventType.MyUsernameResolved:
                OnUsernameResolved.Add(callback as Func<string, Task>);
                break;
            default:
                ThrowUnsupportedEventTypePassed(eventType);
                break;
        }
    }

    public async Task CallHandler<T>(UserHubEventType eventType, T parameter)
    {
        switch (eventType)
        {
            case UserHubEventType.ConnectionIdReceived:
                await _eventCallbackExecutor.ExecuteAllAsync<string>(parameter as string, OnConnectionIdReceived);
                break;
            case UserHubEventType.ConnectedUsersListReceived:
                await _eventCallbackExecutor.ExecuteAllAsync<List<UserConnections>>(parameter as List<UserConnections>, OnUsersConnectionsReceived);
                break;
            case UserHubEventType.MyUsernameResolved:
                await _eventCallbackExecutor.ExecuteAllAsync<string>(parameter as string, OnUsernameResolved);
                break;
            default:
                ThrowUnsupportedEventTypePassed(eventType);
                break;
        }
    }

    private ApplicationException ThrowUnsupportedEventTypePassed(UserHubEventType eventType) => throw new ApplicationException($"Unsupported {nameof(UserHubEventType)} '{eventType}' passed in.");

    public void UnsubscriveAll()
    {
        OnConnectionIdReceived.Clear();
        OnUsersConnectionsReceived.Clear();
        OnUsernameResolved.Clear();
    }
}
