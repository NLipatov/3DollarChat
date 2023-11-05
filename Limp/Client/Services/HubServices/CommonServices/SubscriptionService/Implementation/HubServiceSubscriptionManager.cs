using Limp.Client.Services.HubServices.CommonServices.SubscriptionService.Types;
using System.Collections.Concurrent;

namespace Limp.Client.Services.HubServices.CommonServices.SubscriptionService.Implementation
{
    public class HubServiceSubscriptionManager : IHubServiceSubscriptionManager
    {
        private ConcurrentDictionary<Guid, List<Subscription>> ComponentSubscriptionsKeyValueStorage { get; set; } = new();
        public List<Subscription> GetSubscriptionsByName(string subscriptionName)
        {
            List<Subscription> targetSubscriptions = new();

            foreach (var subscriptionList in ComponentSubscriptionsKeyValueStorage.Values)
            {
                foreach (var subscription in subscriptionList)
                {
                    if(subscription.SubscriptionName == subscriptionName)
                        targetSubscriptions.Add(subscription);
                }
            }

            return targetSubscriptions;
        }

        public Guid AddCallback(Action action, string subscriptionName, Guid componentId)
        {
            Subscription subscription = BuildSubscription(action, subscriptionName, componentId);
            AddSubscription(subscription, subscription.ComponentId);

            return subscription.ComponentId;
        }

        public Guid AddCallback(Func<Task> func, string subscriptionName, Guid componentId)
        {
            Subscription subscription = BuildSubscription(func, subscriptionName, componentId);
            AddSubscription(subscription, subscription.ComponentId);

            return subscription.ComponentId;
        }
        public Guid AddCallback<T>(Action<T> action, string subscriptionName, Guid componentId)
        {
            Subscription subscription = BuildSubscription(action, subscriptionName, componentId);
            AddSubscription(subscription, subscription.ComponentId);

            return subscription.ComponentId;
        }

        public Guid AddCallback<T>(Func<T, Task> func, string subscriptionName, Guid componentId)
        {
            Subscription subscription = BuildSubscription(func, subscriptionName, componentId);
            AddSubscription(subscription, subscription.ComponentId);

            return subscription.ComponentId;
        }

        private Subscription BuildSubscription(object callback, string subscriptionName, Guid componentId)
        {
            Subscription subscription = new Subscription()
            {
                SubscriptionName = subscriptionName,
                ComponentId = componentId,
                Callback = new()
                {
                    CallbackType = callback.GetType(),
                    Delegate = callback
                },
            };

            return subscription;
        }

        private void AddSubscription(Subscription subscription, Guid componentId)
        {
            List<Subscription>? existingSubscriptions;
            ComponentSubscriptionsKeyValueStorage.TryGetValue(componentId, out existingSubscriptions);
            if (existingSubscriptions == null)
            {
                existingSubscriptions = new List<Subscription>()
                {
                    subscription
                };
                ComponentSubscriptionsKeyValueStorage.TryAdd(componentId, existingSubscriptions);
            }
            else
            {
                var updatedSubscriptions = existingSubscriptions.ToList();
                updatedSubscriptions.Add(subscription);
                ComponentSubscriptionsKeyValueStorage.TryUpdate(componentId, updatedSubscriptions, existingSubscriptions);
            }
        }

        public void RemoveComponentCallbacks(Guid componentId)
        {
            ComponentSubscriptionsKeyValueStorage.Remove(componentId, out _);
        }
    }
}