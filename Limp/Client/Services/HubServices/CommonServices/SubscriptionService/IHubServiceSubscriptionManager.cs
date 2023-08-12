using Limp.Client.Services.HubServices.CommonServices.SubscriptionService.Types;

namespace Limp.Client.Services.HubServices.CommonServices.SubscriptionService
{
    public interface IHubServiceSubscriptionManager
    {
        List<Subscription> GetSubscriptionsByName(string SubscriptionName);
        Guid AddCallback(Action action, string subscriptionName, Guid componentId);
        Guid AddCallback(Func<Task> func, string subscriptionName, Guid componentId);
        Guid AddCallback<T>(Action<T> action, string subscriptionName, Guid componentId);
        Guid AddCallback<T>(Func<T, Task> func, string subscriptionName, Guid componentId);
        void RemoveComponentCallbacks(Guid componentId);
    }
}
