using System.Collections.Concurrent;
using Ethachat.Server.Hubs.MessageDispatcher.Handlers.MessageTransmitionGateway;
using Ethachat.Server.Hubs.MessageDispatcher.Handlers.ReliableMessageSender.ConcreteSenders.LongTermMessageStorage;
using Ethachat.Server.Hubs.UsersConnectedManaging.ConnectedUserStorage;
using EthachatShared.Contracts;
using EthachatShared.Models.Message;

namespace Ethachat.Server.Hubs.MessageDispatcher.Handlers.ReliableMessageSender.ConcreteSenders.SenderImplementations.
    EncryptedData
{
    public class EncryptedDataReliableSender : IReliableSender<ClientToClientData>
    {
        private readonly ILongTermStorageService<ClientToClientData> _longTermStorageService;
        private readonly IMessageGateway<ClientToClientData> _gateway;
        private readonly ConcurrentQueue<UnsentItem<ClientToClientData>> _messageQueue = new();
        private readonly ConcurrentDictionary<Guid, bool> _acked = new();
        private readonly ConcurrentDictionary<Guid, ClientToClientData> _unsentItems = new();
        private TaskCompletionSource<bool> _queueSignal = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public EncryptedDataReliableSender(IMessageGateway<ClientToClientData> gateway,
            ILongTermStorageService<ClientToClientData> longTermStorageService)
        {
            _longTermStorageService = longTermStorageService;
            _gateway = gateway;
            Task.Run(async () => await ProcessQueueAsync());
        }

        public Task EnqueueAsync(ClientToClientData data)
        {
            if (_unsentItems.TryAdd(data.Id, data))
            {
                _messageQueue.Enqueue(new UnsentItem<ClientToClientData>
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

            var pendingItems = new List<UnsentItem<ClientToClientData>>();
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

        private async Task SendAsync(UnsentItem<ClientToClientData> unsentItem)
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

        public void OnAck(ClientToClientData data)
        {
            _acked[data.Id] = true;
            Remove(data.Id);
        }

        public void OnAck(Guid id)
        {
            _acked[id] = true;
            Remove(id);
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