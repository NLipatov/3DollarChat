using Limp.Client.Services.HubServices.CommonServices.SubscriptionService.Types;

namespace Limp.Client.Services.HubServices.CommonServices.SubscriptionService
{
    public interface IHubServiceSubscriptionManager
    {
        /// <summary>
        /// Returns a list of subscriptions subscribed to this event name
        /// </summary>
        /// <param name="SubscriptionName"></param>
        /// <returns></returns>
        List<Subscription> GetSubscriptionsByName(string SubscriptionName);
        /// <summary>
        /// Add event callback
        /// </summary>
        /// <param name="action">Method to trigger</param>
        /// <typeparam name="T">Method argument type</typeparam>
        /// <param name="subscriptionName">Event name</param>
        /// <param name="componentId">Id of component subscribed method of which must be triggered</param>
        /// <returns></returns>
        Guid AddCallback<T>(Action<T> action, string subscriptionName, Guid componentId);
        /// <summary>
        /// Add event callback
        /// </summary>
        /// <param name="func">Method to trigger</param>
        /// <typeparam name="T">Method argument type</typeparam>
        /// <param name="subscriptionName">Event name</param>
        /// <param name="componentId">Id of component subscribed method of which must be triggered</param>
        /// <returns></returns>
        Guid AddCallback<T>(Func<T, Task> func, string subscriptionName, Guid componentId);
        /// <summary>
        /// Removes all callbacks of givent component
        /// </summary>
        /// <param name="componentId"></param>
        void RemoveComponentCallbacks(Guid componentId);
    }
}
