using System.Collections.Concurrent;
using Ethachat.Server.Hubs.MessageDispatcher.Handlers.ReliableMessageSender.ConcreteSenders.Models;
using Ethachat.Server.Hubs.MessageDispatcher.Handlers.ReliableMessageSender.ConcreteSenders.Models.Extentions;
using EthachatShared.Models.Message;

namespace Ethachat.Server.Hubs.MessageDispatcher.Handlers.ReliableMessageSender.ConcreteSenders.LongTermMessageStorage.InMemoryStorage;

public class InMemoryLongTermStorage : ILongTermMessageStorageService
{
    private ConcurrentDictionary<string, ConcurrentQueue<UnsentItem>> _storedMessages = new();
    public async Task SaveAsync(Message message)
    {
        var receiver = message.TargetGroup!;
        var unsentMessage = message.ToUnsentMessage();

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