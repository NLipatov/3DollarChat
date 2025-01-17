using Ethachat.Server.Services.Notifications.WebPush.PushDescriptionGeneration.Strategies;
using Ethachat.Server.Services.Notifications.WebPush.PushDescriptionGeneration.Strategies.Implementations;

namespace Ethachat.Server.Services.Notifications.WebPush.PushDescriptionGeneration;

public class PushMessageFactory : IPushMessageFactory
{
    private Dictionary<Type, IPushItemMessageStrategy> _messageStrategies = [];

    public void RegisterStrategy<T>(IPushItemMessageStrategy strategy)
    {
        _messageStrategies.TryAdd(typeof(T), strategy);
    }

    public IPushItemMessageStrategy GetItemStrategy(Type type)
    {
        _messageStrategies.TryGetValue(type, out var strategy);

        return strategy ?? new IgnoreItemStrategy();
    }
}