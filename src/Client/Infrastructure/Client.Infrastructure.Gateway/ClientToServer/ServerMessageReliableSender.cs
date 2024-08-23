using System.Collections.Concurrent;
using EthachatShared.Contracts;
using EthachatShared.Models.Message;

namespace Client.Infrastructure.Gateway.ClientToServer;

internal class ServerMessageReliableSender : IReliableSender<ClientToServerData>
{
    private readonly SignalRGateway _gateway;
    private readonly ConcurrentQueue<UnsentItem<ClientToServerData>> _messageQueue = new();
    private readonly ConcurrentDictionary<Guid, bool> _acked = new();
    private readonly ConcurrentDictionary<Guid, ClientToServerData> _unsentItems = new();
    private TaskCompletionSource<bool> _queueSignal = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public ServerMessageReliableSender(SignalRGateway gateway)
    {
        _gateway = gateway;
        Task.Run(async () => await ProcessQueueAsync());
    }

    public Task EnqueueAsync(ClientToServerData data)
    {
        if (_unsentItems.TryAdd(data.Id, data))
        {
            _messageQueue.Enqueue(new UnsentItem<ClientToServerData>
            {
                Item = data,
                Backoff = TimeSpan.FromSeconds(1)
            });
            _queueSignal.TrySetResult(true); // Signal that a new item is available
        }

        return Task.CompletedTask;
    }

    private async Task ProcessQueueAsync()
    {
        await _queueSignal.Task; // Wait for signal
        _queueSignal = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        var pendingItems = new List<UnsentItem<ClientToServerData>>();
        while (_messageQueue.TryDequeue(out var unsentItem))
        {
            if (unsentItem.SendAfter <= DateTime.UtcNow)
            {
                pendingItems.Add(unsentItem);
            }
            else
            {
                _messageQueue.Enqueue(unsentItem);
            }
        }

        foreach (var item in pendingItems)
        {
            await SendAsync(item);
        }
    }

    private async Task SendAsync(UnsentItem<ClientToServerData> unsentItem)
    {
        if (_acked.TryGetValue(unsentItem.Item.Id, out var isAcked) && isAcked)
        {
            Remove(unsentItem.Item.Id);
            return;
        }

        await _gateway.SendAsync(unsentItem.Item.EventName, unsentItem.Item);

        unsentItem.Backoff = IncreaseBackoff(unsentItem.Backoff);
        unsentItem.SendAfter = DateTime.UtcNow.Add(unsentItem.Backoff);
        _messageQueue.Enqueue(unsentItem);
    }

    private void Remove(Guid messageId)
    {
        _unsentItems.TryRemove(messageId, out _);
        _acked.TryRemove(messageId, out _);
    }

    private TimeSpan IncreaseBackoff(TimeSpan backoff)
    {
        var newBackoff = TimeSpan.FromTicks((long)(backoff.Ticks * 1.5));
        return newBackoff < TimeSpan.FromSeconds(5) ? newBackoff : TimeSpan.FromSeconds(5);
    }

    public void OnAck(ClientToServerData data)
    {
        _acked[data.Id] = true;
        Remove(data.Id);
    }

    public void OnAck(Guid id)
    {
        _acked[id] = true;
        Remove(id);
    }
}