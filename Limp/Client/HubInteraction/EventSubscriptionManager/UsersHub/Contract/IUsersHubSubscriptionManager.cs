using Limp.Client.HubInteraction.EventSubscriptionManager.UsersHub.EventTypes;

namespace Limp.Client.HubInteraction.EventSubscriptionManager.UsersHub.Contract
{
    public interface IUsersHubSubscriptionManager
    {
        void AddHandler<T>(UserHubEventType eventType, Func<T, Task> callback);
        Task CallHandler<T>(UserHubEventType eventType, T parameter);
        void UnsubscriveAll();
    }
}