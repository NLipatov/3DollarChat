using System.Collections.Concurrent;
using Ethachat.Server.Hubs.MessageDispatcher.Handlers.MessageTransmitionGateway;
using Ethachat.Server.Hubs.MessageDispatcher.Handlers.ReliableMessageSender.ConcreteSenders.Models;
using Ethachat.Server.Hubs.MessageDispatcher.Handlers.ReliableMessageSender.ConcreteSenders.Models.Extentions;
using Ethachat.Server.Hubs.UsersConnectedManaging.ConnectedUserStorage;
using Ethachat.Server.Utilities.Redis.UnsentMessageHandling;
using EthachatShared.Models.Message;

namespace Ethachat.Server.Hubs.MessageDispatcher.Handlers.ReliableMessageSender.ConcreteSenders.Binary.Implementation
{
    public class ReliableBinaryMessageSender : IReliableBinaryMessageSender
    {
        private readonly IUnsentMessagesRedisService _unsentMessagesRedisService;
        private readonly IMessageGateway _gateway;
        private readonly ConcurrentDictionary<Guid, ConcurrentBag<UnsentItem>> _fileIdToUnsentItems = new();
        private readonly ConcurrentDictionary<Guid, HashSet<int>> _ackedChunks = new();
        private const int MetadataFilesCount = 1;

        public ReliableBinaryMessageSender(IMessageGateway gateway,
            IUnsentMessagesRedisService unsentMessagesRedisService)
        {
            _unsentMessagesRedisService = unsentMessagesRedisService;
            _gateway = gateway;
        }

        public async Task EnqueueAsync(Message message)
        {
            var fileId = GetFileId(message);
            var unsentMessage = message.ToUnsentMessage();

            _fileIdToUnsentItems.AddOrUpdate(fileId,
                _ => new ConcurrentBag<UnsentItem> { unsentMessage },
                (_, existingData) =>
                {
                    existingData.Add(unsentMessage);
                    return existingData;
                });

            await Deliver(message);
        }

        private async Task Deliver(Message message, TimeSpan? backoff = null)
        {
            var fileId = GetFileId(message);

            if (_fileIdToUnsentItems.TryGetValue(fileId, out var unsentItems)
                && !IsFileAcked(fileId))
            {
                if (!HasActiveConnections(message.TargetGroup!))
                {
                    await PassToLongTermSender(fileId);
                    Remove(fileId);
                    return;
                }
                
                if (!IsMessageAcked(fileId, GetKey(message)))
                {
                    await _gateway.SendAsync(message);
                    backoff = IncreaseBackoff(GetKey(message) + 1, backoff);
                    await Task.Delay(backoff ?? TimeSpan.Zero);
                    await Deliver(message, backoff);
                }
            }
            
            if (IsFileAcked(fileId))
                Remove(fileId);
        }

        private bool IsMessageAcked(Guid fileId, int index)
        {
            _ackedChunks.TryGetValue(fileId, out var acked);
            return (acked ?? new()).Contains(index);
        }

        private bool IsFileAcked(Guid fileId)
        {
            _fileIdToUnsentItems.TryGetValue(fileId, out var unsentItems);
            
            _ackedChunks.TryGetValue(fileId, out var acked);

            unsentItems ??= new();

            if (unsentItems.Any() && acked?.Count == GetChunksCount(unsentItems.First().Message) + MetadataFilesCount)
            {
                return true;
            }

            return false;
        }

        private Guid GetFileId(Message message)
        {
            return message.Type switch
            {
                MessageType.DataPackage => message.Package!.FileDataid,
                MessageType.Metadata => message.Metadata!.DataFileId,
                _ => throw new ArgumentException($"Unhandled type passed in")
            };
        }

        public void OnAck(Message syncMessage)
        {
            var fileId = syncMessage.SyncItem?.FileId ?? Guid.Empty;
            
            if (fileId == Guid.Empty) return; //Invalid ack
            if (syncMessage.SyncItem is null) return; //Invalid ack

            _ackedChunks.AddOrUpdate(fileId,
                _ => new HashSet<int>{syncMessage.SyncItem.Index},
                (_, existingData) =>
                {
                    existingData.Add(syncMessage.SyncItem!.Index);

                    return existingData;
                });

            _ackedChunks.TryGetValue(fileId, out var acked);
        }

        private int GetChunksCount(Message message)
        {
            return message.Type switch
            {
                MessageType.DataPackage => message.Package!.Total,
                MessageType.Metadata => message.Metadata!.ChunksCount,
                _ => throw new ArgumentException($"Unhandled type passed in")
            };
        }

        private int GetKey(Message message)
        {
            return message.Type switch
            {
                MessageType.DataPackage => message.Package!.Index,
                MessageType.Metadata => -1,
                _ => throw new ArgumentException($"Unhandled type passed in")
            };
        }

        private TimeSpan IncreaseBackoff(int index = 1, TimeSpan? backoff = null)
        {
            if (backoff.HasValue)
            {
                if (backoff.Value < TimeSpan.FromSeconds(Math.Max(index, 1) * 5))
                    backoff.Value.Multiply(1.5);
            }

            return TimeSpan.FromSeconds(Math.Max(index, 1) * 1);
        }

        private async Task PassToLongTermSender(Guid fileId)
        {
            _fileIdToUnsentItems.TryRemove(fileId, out var unsentItems);
            foreach (var unsentItem in unsentItems ?? new())
            {
                await _unsentMessagesRedisService.SaveAsync(unsentItem.Message);
            }
        }

        private void Remove(Guid fileId)
        {
            _fileIdToUnsentItems.TryRemove(fileId, out _);
            _ackedChunks.Remove(fileId, out _);
        }

        private bool HasActiveConnections(string username)
            => InMemoryHubConnectionStorage.MessageDispatcherHubConnections
                .Where(x => x.Key == username)
                .SelectMany(x => x.Value).Any();
    }
}