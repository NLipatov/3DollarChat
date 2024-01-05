using System.Collections.Concurrent;

namespace Ethachat.Client.Services.ConcurrentCollectionManager.Implementations
{
    public class ConcurrentCollectionManager : IConcurrentCollectionManager
    {
        public Guid TryAddSubscription<T>(ConcurrentDictionary<Guid, T> dictionary, T callback)
        {
            Guid handlerId = Guid.NewGuid();
            T callbackFunc = callback;
            bool isAdded = dictionary.TryAdd(handlerId, callbackFunc);
            if (!isAdded)
                TryAddSubscription(dictionary, callback);

            return handlerId;
        }

        public void TryRemoveSubscription<T>(ConcurrentDictionary<Guid, T> dictionary, Guid handlerId)
        {
            T? target = dictionary.GetValueOrDefault(handlerId);
            if (target != null)
            {
                bool isRemoved = dictionary.TryRemove(handlerId, out target);
                if (!isRemoved)
                    TryRemoveSubscription(dictionary, handlerId);
            }
        }
    }
}
