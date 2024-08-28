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
            while (true)
            {
                await _queueSignal.Task; // Wait for signal
                _queueSignal = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

                while (_messageQueue.TryDequeue(out var unsentItem))
                {
                    if (_acked.TryGetValue(unsentItem.Item.Id, out var acked) && acked)
                    {
                        // Item acked - remove it
                        _acked.TryRemove(unsentItem.Item.Id, out _);
                        foreach (var key in _acked.Keys)
                        {
                            if (!_unsentItems.ContainsKey(key))
                            {
                                _acked.TryRemove(key, out _);
                            }
                        }
                        Console.WriteLine($"{nameof(_messageQueue)}: {_messageQueue.Count}");
                        Console.WriteLine($"{nameof(unsentItem)}: {_unsentItems.Count}");
                        Console.WriteLine($"{nameof(_acked)}: {_acked.Count}");
                        continue;
                    }

                    if (unsentItem.SendAfter <= DateTime.UtcNow)
                    {
                        await SendAsync(unsentItem);
                    }
                    else
                    {
                        // If message is not ready to be sent, move it back to queue
                        _messageQueue.Enqueue(unsentItem);
                        break; // waiting for next signal
                    }
                }

                // Rerunning queue processing if something still in queue
                if (!_messageQueue.IsEmpty)
                {
                    _queueSignal.TrySetResult(true);
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

            if (_acked.TryGetValue(unsentItem.Item.Id, out var isAcked) && isAcked)
            {
                _unsentItems.TryRemove(unsentItem.Item.Id, out _);
                return;
            }

            await _gateway.TransferAsync(unsentItem.Item);

            if (!_acked.TryGetValue(unsentItem.Item.Id, out isAcked) || !isAcked)
            {
                // Item is not acked - increase backoff and move back to queue
                unsentItem.Backoff = IncreaseBackoff(unsentItem.Backoff);
                unsentItem.SendAfter = DateTime.UtcNow.Add(unsentItem.Backoff);
                _messageQueue.Enqueue(unsentItem);
            }
            else
            {
                // Item acked - remove it
                _unsentItems.TryRemove(unsentItem.Item.Id, out _);
            }
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

        private TimeSpan IncreaseBackoff(TimeSpan backoff)
        {
            var newBackoff = TimeSpan.FromTicks((long)(backoff.Ticks * 1.5));
            return newBackoff < TimeSpan.FromSeconds(5) ? newBackoff : TimeSpan.FromSeconds(5);
        }

        public void OnAck(ClientToClientData data)
        {
            _acked[data.Id] = true;   
            _unsentItems.TryRemove(data.Id, out _);
        }

        public void OnAck(Guid id)
        {
            _acked[id] = true;
            _unsentItems.TryRemove(id, out _);
        }
    }
}