using System.Collections.Concurrent;
using Ethachat.Server.Hubs.MessageDispatcher.Helpers.SystemEventNotification.Exceptions;
using Ethachat.Server.Hubs.UsersConnectedManaging.ConnectedUserStorage;
using EthachatShared.Models.SystemEvents;
using Microsoft.AspNetCore.SignalR;

namespace Ethachat.Server.Hubs.MessageDispatcher.Helpers.SystemEventNotification.Implementation;

public class SystemEventNotificationMediator : ISystemEventNotificationMediator
{
    private ConcurrentDictionary<string, List<Func<Task>>> _unnotifiedSystemEvents = new();
    public async Task Notify<T>(SystemEvent<T> systemEvent, IClientProxy clientProxy, string username)
    {
        var func = async () => await clientProxy.SendAsync(systemEvent.Type.ToString(), systemEvent.EventValue);

        if (IsClientConnectedToHub(username))
            await func.Invoke();
        else
        {
            if (_unnotifiedSystemEvents.ContainsKey(username))
            {
                if (_unnotifiedSystemEvents.TryGetValue(username, out var currentFuncCollection))
                {
                    var updatedFuncCollection = new List<Func<Task>>();
                    updatedFuncCollection.AddRange(currentFuncCollection);
                    updatedFuncCollection.Add(func);

                    _unnotifiedSystemEvents
                        .TryUpdate(username, currentFuncCollection, updatedFuncCollection);
                }
                else
                {
                    throw new SystemEventNotificationMediatorException(
                        $"Could not update {nameof(_unnotifiedSystemEvents)} collection.");
                }
            }
            else
            {
                _unnotifiedSystemEvents.TryAdd(username, [func]);
            }
        }
    }
    
    private static bool IsClientConnectedToHub(string username)
    {
        lock (InMemoryHubConnectionStorage.MessageDispatcherHubConnections)
        {
            return InMemoryHubConnectionStorage.MessageDispatcherHubConnections
                .Any(x => x.Key == username);
        }
    }
}