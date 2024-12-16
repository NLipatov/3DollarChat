using System.Collections.Concurrent;
using Ethachat.Server.Hubs.MessageDispatcher.Handlers.MessageTransmitionGateway;
using Ethachat.Server.Hubs.MessageDispatcher.Handlers.ReliableMessageSender.ConcreteSenders.LongTermMessageStorage;
using Ethachat.Server.Hubs.UsersConnectedManaging.ConnectedUserStorage;
using EthachatShared.Contracts;
using EthachatShared.Models.Message;

namespace Ethachat.Server.Hubs.MessageDispatcher.Handlers.ReliableMessageSender.ConcreteSenders.SenderImplementations.
    EncryptedData;

public class ClientToClientDataReliableSender : IReliableSender<ClientToClientData>
{
    private readonly ILongTermStorageService<ClientToClientData> _longTermStorageService;
    private readonly IMessageGateway<ClientToClientData> _gateway;
    private readonly ConcurrentQueue<Guid> _messageQueue = new();
    private readonly ConcurrentDictionary<Guid, UnsentItem<ClientToClientData>> _unsentItems = new();
    private TaskCompletionSource<bool> _queueSignal = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public ClientToClientDataReliableSender(IMessageGateway<ClientToClientData> gateway,
        ILongTermStorageService<ClientToClientData> longTermStorageService)
    {
        _longTermStorageService = longTermStorageService;
        _gateway = gateway;
        Task.Run(async () => await ProcessQueueAsync());
    }

    public async Task EnqueueAsync(ClientToClientData data)
    {
        var unsentItem = new UnsentItem<ClientToClientData>
        {
            Item = data,
            Backoff = TimeSpan.FromSeconds(5)
        };
        
        //If data has unique id, add it to unsentItems collection
        if (!_unsentItems.TryAdd(data.Id, unsentItem))
            return;
        
        _ = Task.Run(() => _gateway.TransferAsync(unsentItem.Item));

        //Add unsent item to send queue
        _messageQueue.Enqueue(data.Id);
        //Start queue processing
        _queueSignal.TrySetResult(true);
    }

    private async Task ProcessQueueAsync()
    {
        while (true)
        {
            await _queueSignal.Task;
            //waits for a signal to come in
            _queueSignal = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            while (_messageQueue.TryDequeue(out var messageId))
            {
                //If id is in messageQueue, but not in unsentItems it's considered delivered
                if (!_unsentItems.TryGetValue(messageId, out var unsentItem))
                    continue;

                if (DateTime.UtcNow < unsentItem.SendAfter)
                {
                    _messageQueue.Enqueue(messageId);
                    continue;
                }

                await SendAsync(unsentItem);
            }
        }
    }

    private async Task SendAsync(UnsentItem<ClientToClientData> unsentItem)
    {
        if (!HasActiveConnections(unsentItem.Item.Target))
        {
            await PassToLongTermSender(unsentItem.Item.Id);
            _unsentItems.TryRemove(unsentItem.Item.Id, out _);
            return;
        }

        await _gateway.TransferAsync(unsentItem.Item);

        if (_unsentItems.ContainsKey(unsentItem.Item.Id))
        {
            unsentItem.Backoff = IncreaseBackoff(unsentItem.Backoff);
            unsentItem.SendAfter = DateTime.UtcNow.Add(unsentItem.Backoff);
            _messageQueue.Enqueue(unsentItem.Item.Id);
        }
    }

    private async Task PassToLongTermSender(Guid messageId)
    {
        if (_unsentItems.TryRemove(messageId, out var unsentItem))
            await _longTermStorageService.SaveAsync(unsentItem.Item);
    }

    private bool HasActiveConnections(string username)
        => InMemoryHubConnectionStorage.MessageDispatcherHubConnections
            .Where(x => x.Key == username)
            .SelectMany(x => x.Value).Any();

    private TimeSpan IncreaseBackoff(TimeSpan backoff)
    {
        var newBackoff = TimeSpan.FromTicks((long)(backoff.Ticks * 1.5));
        return newBackoff < TimeSpan.FromSeconds(10) ? newBackoff : TimeSpan.FromSeconds(10);
    }

    public void OnAck(ClientToClientData data)
    {
        _unsentItems.TryRemove(data.Id, out _);
        Console.WriteLine($"{nameof(_messageQueue)}: {_messageQueue.Count}");
        Console.WriteLine($"{nameof(_unsentItems)}: {_unsentItems.Count}");
    }

    public void OnAck(Guid id)
    {
        _unsentItems.TryRemove(id, out _);
        Console.WriteLine($"{nameof(_messageQueue)}: {_messageQueue.Count}");
        Console.WriteLine($"{nameof(_unsentItems)}: {_unsentItems.Count}");
    }
}