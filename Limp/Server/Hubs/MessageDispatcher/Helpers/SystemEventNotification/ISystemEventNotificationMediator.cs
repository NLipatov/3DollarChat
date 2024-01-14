using EthachatShared.Models.SystemEvents;
using Microsoft.AspNetCore.SignalR;

namespace Ethachat.Server.Hubs.MessageDispatcher.Helpers.SystemEventNotification;

public interface ISystemEventNotificationMediator
{
    Task Notify<T>(SystemEvent<T> systemEvent, IClientProxy clientProxy, string username);
}