using Limp.Client.Services.HubServices.CommonServices.SubscriptionService.Types;

namespace Limp.Client.Services.HubServices.CommonServices.SubscriptionService
{
    public interface IHubServiceSubscriptionManager
    {
        List<Subscription> GetComponentSubscriptions(string SubscriptionName);
        Guid AddCallback<T>(Action<T> action, string subscriptionName, Guid? componentId = null);
        Guid AddCallback<T>(Func<T, Task> func, string subscriptionName, Guid? componentId = null);
        void RemoveComponentCallbacks(Guid componentId);
    }
}
