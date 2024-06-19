using Ethachat.Server.Services.Notifications.WebPush.PushDescriptionGeneration.Strategies;

namespace Ethachat.Server.Services.Notifications.WebPush.PushDescriptionGeneration;

public interface IPushMessageFactory
{
    void RegisterStrategy<T>(IPushItemMessageStrategy strategy);
    IPushItemMessageStrategy GetItemStrategy(Type type);
}