using System.Collections.Concurrent;
using Ethachat.Server.Hubs.MessageDispatcher.Handlers.ReliableMessageSender.ConcreteSenders.Models;
using Ethachat.Server.Hubs.MessageDispatcher.Handlers.ReliableMessageSender.ConcreteSenders.Models.Extentions;
using EthachatShared.Models.Message;

namespace Ethachat.Server.Hubs.MessageDispatcher.Handlers.ReliableMessageSender.ConcreteSenders.LongTermMessageStorage.InMemoryStorage;

public class InMemoryLongTermStorage : ILongTermStorageService<Message>
{
    private ConcurrentDictionary<string, ConcurrentQueue<UnsentItem>> _storedMessages = new();
    public async Task SaveAsync(Message data)
    {
        var receiver = data.Target!;
        var unsentMessage = data.ToUnsentMessage();

        var targetStack = _storedMessages.GetOrAdd(receiver, new ConcurrentQueue<UnsentItem>());
        targetStack.Enqueue(unsentMessage);
    }

    public async Task<Message[]> GetSaved(string username)
    {
        _storedMessages.TryGetValue(username, out var stack);
        _storedMessages.TryRemove(username, out _);
        var messages = (stack ?? new()).ToArray();
        
        return messages
            .Select(x=>x.Message)
            .ToArray();
    }
}