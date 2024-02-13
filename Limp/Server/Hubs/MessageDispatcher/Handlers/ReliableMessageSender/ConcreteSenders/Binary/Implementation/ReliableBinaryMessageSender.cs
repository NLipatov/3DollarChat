using System.Collections.Concurrent;
using Ethachat.Server.Hubs.MessageDispatcher.Handlers.MessageTransmitionGateway;
using Ethachat.Server.Hubs.MessageDispatcher.Handlers.ReliableMessageSender.Models;
using Ethachat.Server.Hubs.MessageDispatcher.Handlers.ReliableMessageSender.Models.Extentions;
using Ethachat.Server.Utilities.Redis.UnsentMessageHandling;
using EthachatShared.Models.Message;

namespace Ethachat.Server.Hubs.MessageDispatcher.Handlers.ReliableMessageSender.ConcreteSenders.Binary.Implementation
{
    public class ReliableBinaryMessageSender : IReliableBinaryMessageSender
    {
        private readonly IUnsentMessagesRedisService _unsentMessagesRedisService;
        private readonly IMessageGateway _gateway;
        private ConcurrentDictionary<Guid, ConcurrentBag<UnsentItem>> FileIdToUnsentItems = new();
        private ConcurrentDictionary<Guid, Guid> messageIdToFileId = new();
        private volatile bool _isSending;

        public ReliableBinaryMessageSender(IMessageGateway gateway,
            IUnsentMessagesRedisService unsentMessagesRedisService)
        {
            _unsentMessagesRedisService = unsentMessagesRedisService;
            _gateway = gateway;
        }

        public void Enqueue(Message message)
        {
            var fileId = GetFileId(message);
            var unsentMessage = message.ToUnsentMessage();

            messageIdToFileId.TryAdd(message.Id, fileId);

            FileIdToUnsentItems.AddOrUpdate(fileId,
                _ => [unsentMessage],
                (_, existingData) =>
                {
                    existingData.Add(unsentMessage);
                    return existingData;
                });

            if (!_isSending)
            {
                if (!_isSending)
                {
                    _isSending = true;
                    Task.Run(() => StartSendingLoop());
                }
            }
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

        public void OnAckReceived(Guid messageId, string targetGroup)
        {
            messageIdToFileId.TryGetValue(messageId, out var fileId);
            FileIdToUnsentItems.TryGetValue(fileId, out var unsentItems);

            unsentItems!.First(x => x.Message.Id == messageId).Ack = true;
        }

        private async Task StartSendingLoop()
        {
            while (!FileIdToUnsentItems.IsEmpty)
            {
                foreach (var kvp in FileIdToUnsentItems)
                {
                    var fileId = kvp.Key;
                    var unsentItems = kvp.Value;

                    if (unsentItems.Any(x => x.Backoff > TimeSpan.FromMinutes(10)))
                    {
                        FileIdToUnsentItems.TryGetValue(fileId, out var items);
                        foreach (var item in items)
                        {
                            await _unsentMessagesRedisService.Save(item.Message);
                            messageIdToFileId.TryRemove(item.Message.Id, out _);
                        }

                        FileIdToUnsentItems.TryRemove(fileId, out var _);
                        continue;
                    }

                    if (unsentItems.All(x => x.Ack))
                    {
                        FileIdToUnsentItems.TryRemove(fileId, out var _);
                        foreach (var item in unsentItems)
                        {
                            messageIdToFileId.TryRemove(item.Message.Id, out var _);
                        }

                        continue;
                    }

                    foreach (var item in unsentItems.Where(x => !x.Ack))
                    {
                        if (item.ResendAfter > DateTime.UtcNow)
                        {
                            continue;
                        }

                        await _gateway.SendAsync(item.Message);
                        IncreaseBackoff(item);
                    }
                }
            }

            _isSending = false;
        }

        private void IncreaseBackoff(UnsentItem item)
        {
            item.Backoff = item.Backoff.Multiply(1.5);
            item.ResendAfter = DateTime.UtcNow.Add(item.Backoff);
        }
    }
}