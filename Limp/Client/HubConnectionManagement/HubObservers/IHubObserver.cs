namespace Limp.Client.HubInteraction.HubObservers
{
    public interface IHubObserver<EventEnum> where EventEnum : Enum
    {
        /// <summary>
        /// Adds handler for event type
        /// </summary>
        Guid AddHandler<T>(EventEnum eventType, Func<T, Task> callback);
        /// <summary>
        /// Removes handler by handler id
        /// </summary>
        void RemoveHandlers(List<Guid> handlerIds);
        /// <summary>
        /// Call event type handler method with given parameters
        /// </summary>
        Task CallHandler<T>(EventEnum eventType, T parameter);
        /// <summary>
        /// Remove all registered in observer handlers
        /// </summary>
        void UnsubscriveAll();
    }
}