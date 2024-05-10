using System.Collections.Concurrent;
using Ethachat.Server.Hubs.MessageDispatcher.Handlers.MessageTransmitionGateway;
using Ethachat.Server.Hubs.MessageDispatcher.Handlers.ReliableMessageSender.ConcreteSenders.LongTermMessageStorage;
using Ethachat.Server.Hubs.UsersConnectedManaging.ConnectedUserStorage;
using EthachatShared.Models.Message;

namespace Ethachat.Server.Hubs.MessageDispatcher.Handlers.ReliableMessageSender.ConcreteSenders.SenderImplementations.
    EncryptedData;

public class EncryptedDataReliableSender : IReliableMessageSender<EncryptedDataTransfer>
{
    private readonly ILongTermStorageService<EncryptedDataTransfer> _longTermStorageService;
    private readonly IMessageGateway<EncryptedDataTransfer> _gateway;
    private ConcurrentDictionary<Guid, EncryptedDataTransfer> _unsentItems = new();
    private ConcurrentDictionary<Guid, bool> _acked = new();
    private volatile bool _isSending;

    public EncryptedDataReliableSender(IMessageGateway<EncryptedDataTransfer> gateway,
        ILongTermStorageService<EncryptedDataTransfer> longTermStorageService)
    {
        _longTermStorageService = longTermStorageService;
        _gateway = gateway;
    }

    public async Task EnqueueAsync(EncryptedDataTransfer data)
    {
        _unsentItems.TryAdd(data.Id, data);

        await Deliver(data);
    }

    private async Task Deliver(EncryptedDataTransfer data, TimeSpan? backoff = null)
    {
        if (_unsentItems.ContainsKey(data.Id))
        {
            if (!HasActiveConnections(data.Target))
            {
                await PassToLongTermSender(data.Id);
                Remove(data.Id);
                return;
            }

            _acked.TryGetValue(data.Id, out var isAcked);
            if (isAcked)
            {
                Remove(data.Id);
                return;
            }

            await _gateway.TransferAsync(data);
            backoff = IncreaseBackoff(backoff);
            await Task.Delay(backoff.Value);
            await Deliver(data, backoff);
        }
    }


    private async Task PassToLongTermSender(Guid messageId)
    {
        _unsentItems.TryRemove(messageId, out var unsentItem);

        if (unsentItem is not null)
        {
            await _longTermStorageService.SaveAsync(unsentItem);
        }
    }

    private bool HasActiveConnections(string username)
        => InMemoryHubConnectionStorage.MessageDispatcherHubConnections
            .Where(x => x.Key == username)
            .SelectMany(x => x.Value).Any();

    public void OnAck(EncryptedDataTransfer syncMessage)
    {
        _acked.TryAdd(syncMessage.Id, true);
    }

    private TimeSpan IncreaseBackoff(TimeSpan? backoff = null)
    {
        if (backoff.HasValue)
        {
            if (backoff.Value < TimeSpan.FromSeconds(5))
                backoff.Value.Multiply(1.5);
        }

        return TimeSpan.FromSeconds(3);
    }

    private void Remove(Guid messageId)
    {
        _unsentItems.TryRemove(messageId, out var _);
        _acked.TryRemove(messageId, out var _);
    }
}