using System.Collections.Concurrent;
using Ethachat.Server.Hubs.MessageDispatcher.Handlers.MessageTransmitionGateway;
using Ethachat.Server.Hubs.MessageDispatcher.Handlers.ReliableMessageSender.ConcreteSenders.Models;
using Ethachat.Server.Hubs.MessageDispatcher.Handlers.ReliableMessageSender.ConcreteSenders.Models.Extentions;
using Ethachat.Server.Utilities.Redis.UnsentMessageHandling;
using EthachatShared.Models.Message;

namespace Ethachat.Server.Hubs.MessageDispatcher.Handlers.ReliableMessageSender.ConcreteSenders.Text.Implementation
{
    public class ReliableTextMessageSender : IReliableTextMessageSender
    {
        private readonly IUnsentMessagesRedisService _unsentMessagesRedisService;
        private readonly IMessageGateway _gateway;
        private ConcurrentDictionary<Guid, UnsentItem> _unsentItems = new();
        private ConcurrentDictionary<Guid, bool> _acked = new();
        private volatile bool _isSending;

        public ReliableTextMessageSender(IMessageGateway gateway, IUnsentMessagesRedisService unsentMessagesRedisService)
        {
            _unsentMessagesRedisService = unsentMessagesRedisService;
            _gateway = gateway;
        }

        public async Task Enqueue(Message message)
        {
            var unsentMessage = message.ToUnsentMessage();
            _unsentItems.TryAdd(message.Id, unsentMessage);

            if (!_isSending)
            {
                if (!_isSending)
                {
                    _isSending = true;
                    Task.Run(() => StartSendingLoop());
                }
            }
        }

        public void OnAckReceived(Guid messageId, string targetGroup)
        {
            _acked.TryAdd(messageId, true);
        }

        public void OnAckReceived(Message syncMessage)
        {
            _acked.TryAdd(syncMessage.SyncItem!.MessageId, true);
        }

        private async Task StartSendingLoop()
        {
            while (!_unsentItems.IsEmpty)
            {
                foreach (var kvp in _unsentItems)
                {
                    var messageId = kvp.Key;
                    var unsentItem = kvp.Value;

                    if (_acked.TryGetValue(messageId, out var isAcked) && isAcked)
                    {
                        _unsentItems.TryRemove(messageId, out _);
                        continue;
                    }

                    if (unsentItem.Backoff > TimeSpan.FromMinutes(10))
                    {
                        await _unsentMessagesRedisService.Save(unsentItem.Message);
                        _unsentItems.TryRemove(messageId, out _);
                        _acked.TryRemove(messageId, out _);
                        continue;
                    }

                    if (unsentItem.ResendAfter > DateTime.UtcNow)
                    {
                        continue;
                    }

                    await _gateway.SendAsync(unsentItem.Message);
                    IncreaseBackoff(unsentItem);
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