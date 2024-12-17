using System.Collections.Concurrent;
using EthachatShared.Models.Message;

namespace Ethachat.Server.Hubs.MessageDispatcher.Handlers.ReliableMessageSender.ConcreteSenders.LongTermMessageStorage.InMemoryStorage;

public class InMemoryLongTermTransferStorage : ILongTermStorageService<ClientToClientData>
{
    private ConcurrentDictionary<string, ConcurrentQueue<ClientToClientData>> _storedMessages = new();
    public async Task SaveAsync(ClientToClientData data)
    {
        var receiver = data.Target;

        var targetStack = _storedMessages.GetOrAdd(receiver, new ConcurrentQueue<ClientToClientData>());
        targetStack.Enqueue(data);
    }

    public async Task<ClientToClientData[]> GetSaved(string username)
    {
        _storedMessages.TryGetValue(username, out var stack);
        _storedMessages.TryRemove(username, out _);
        var datas = (stack ?? new()).ToArray();

        return datas;
    }
}