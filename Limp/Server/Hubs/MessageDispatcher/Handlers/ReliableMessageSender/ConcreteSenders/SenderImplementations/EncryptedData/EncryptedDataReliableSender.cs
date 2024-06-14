using System.Collections.Concurrent;
using Ethachat.Server.Hubs.MessageDispatcher.Handlers.MessageTransmitionGateway;
using Ethachat.Server.Hubs.MessageDispatcher.Handlers.ReliableMessageSender.ConcreteSenders.LongTermMessageStorage;
using
    Ethachat.Server.Hubs.MessageDispatcher.Handlers.ReliableMessageSender.ConcreteSenders.SenderImplementations.Models;
using Ethachat.Server.Hubs.UsersConnectedManaging.ConnectedUserStorage;
using EthachatShared.Models.Message;

namespace Ethachat.Server.Hubs.MessageDispatcher.Handlers.ReliableMessageSender.ConcreteSenders.SenderImplementations.
    EncryptedData
{
    public class EncryptedDataReliableSender : IReliableMessageSender<EncryptedDataTransfer>
    {
        private readonly ILongTermStorageService<EncryptedDataTransfer> _longTermStorageService;
        private readonly IMessageGateway<EncryptedDataTransfer> _gateway;
        private readonly ConcurrentQueue<UnsentItem<EncryptedDataTransfer>> _messageQueue = new();
        private readonly ConcurrentDictionary<Guid, bool> _acked = new();
        private readonly ConcurrentDictionary<Guid, EncryptedDataTransfer> _unsentItems = new();
        private TaskCompletionSource<bool> _queueSignal = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public EncryptedDataReliableSender(IMessageGateway<EncryptedDataTransfer> gateway,
            ILongTermStorageService<EncryptedDataTransfer> longTermStorageService)
        {
            _longTermStorageService = longTermStorageService;
            _gateway = gateway;
            Task.Run(async () => await ProcessQueueAsync());
        }

        public Task EnqueueAsync(EncryptedDataTransfer data)
        {
            if (_unsentItems.TryAdd(data.Id, data))
            {
                _messageQueue.Enqueue(new UnsentItem<EncryptedDataTransfer>
                {
                    Item = data,
                    Backoff = TimeSpan.FromSeconds(3)
                });
                _queueSignal.TrySetResult(true); // Signal that a new item is available
            }

            return Task.CompletedTask;
        }

        private async Task ProcessQueueAsync()
        {
            await _queueSignal.Task; // Wait for signal
            _queueSignal = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            var pendingItems = new List<UnsentItem<EncryptedDataTransfer>>();
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

        private async Task SendAsync(UnsentItem<EncryptedDataTransfer> unsentItem)
        {
            if (!HasActiveConnections(unsentItem.Item.Target))
            {
                await PassToLongTermSender(unsentItem.Item.Id);
                Remove(unsentItem.Item.Id);
                return;
            }

            if (_acked.TryGetValue(unsentItem.Item.Id, out var isAcked) && isAcked)
            {
                Remove(unsentItem.Item.Id);
                return;
            }

            await _gateway.TransferAsync(unsentItem.Item);

            unsentItem.Backoff = IncreaseBackoff(unsentItem.Backoff);
            unsentItem.SendAfter = DateTime.UtcNow.Add(unsentItem.Backoff);
            _messageQueue.Enqueue(unsentItem);
        }

        private async Task PassToLongTermSender(Guid messageId)
        {
            if (_unsentItems.TryRemove(messageId, out var unsentItem))
            {
                await _longTermStorageService.SaveAsync(unsentItem);
            }
        }

        private bool HasActiveConnections(string username)
            => InMemoryHubConnectionStorage.MessageDispatcherHubConnections
                .Where(x => x.Key == username)
                .SelectMany(x => x.Value).Any();

        public void OnAck(EncryptedDataTransfer data)
        {
            _acked[data.Id] = true;
            Remove(data.Id);
        }

        private TimeSpan IncreaseBackoff(TimeSpan backoff)
        {
            var newBackoff = TimeSpan.FromTicks((long)(backoff.Ticks * 1.5));
            return newBackoff < TimeSpan.FromSeconds(5) ? newBackoff : TimeSpan.FromSeconds(5);
        }

        private void Remove(Guid messageId)
        {
            _unsentItems.TryRemove(messageId, out _);
            _acked.TryRemove(messageId, out _);
        }
    }
}