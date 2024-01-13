using EthachatShared.Models.SystemEvents;

namespace Ethachat.Server.Utilities.Redis.UnsentSystemEventsHandling;

public interface IUnsentSystemEventsService
{
    Task Save<T>(SystemEvent<T> systemEvent, string username);
    Task<SystemEvent<T>[]> GetSaved<T>(string username);
}