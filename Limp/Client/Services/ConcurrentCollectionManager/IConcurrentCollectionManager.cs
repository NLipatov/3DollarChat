using System.Collections.Concurrent;

namespace Limp.Client.Services.ConcurrentCollectionManager
{
    public interface IConcurrentCollectionManager
    {
        Guid TryAddSubscription<T>(ConcurrentDictionary<Guid, T> dictionary, T callback);
        void TryRemoveSubscription<T>(ConcurrentDictionary<Guid, T> dictionary, Guid handlerId);
    }
}
