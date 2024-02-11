using System.Collections.Concurrent;
using Ethachat.Server.Hubs.MessageDispatcher.Handlers.MessageTransmitionGateway;
using Ethachat.Server.Hubs.MessageDispatcher.Handlers.ReliableMessageSender.Models;
using Ethachat.Server.Hubs.MessageDispatcher.Handlers.ReliableMessageSender.Models.Extentions;
using Ethachat.Server.Utilities.Redis.UnsentMessageHandling;
using EthachatShared.Models.Message;

namespace Ethachat.Server.Hubs.MessageDispatcher.Handlers.ReliableMessageSender
{
    public class ReliableMessageSender : IReliableMessageSender
    {
        private readonly IUnsentMessagesRedisService _unsentMessagesRedisService;
        public readonly IMessageGateway _gateway;
        private readonly ConcurrentDictionary<Guid, UnsentItem> unsentItems = new();
        private readonly ConcurrentDictionary<Guid, bool> _acked = new();
        private readonly object _lock = new();
        private bool _isSending;

        public ReliableMessageSender(IMessageGateway gateway, IUnsentMessagesRedisService unsentMessagesRedisService)
        {
            _unsentMessagesRedisService = unsentMessagesRedisService;
            _gateway = gateway;
        }

        public void Enqueue(Message message)
        {
            var unsentMessage = message.ToUnsentMessage();

            unsentItems.TryAdd(message.Id, unsentMessage);

            _acked.TryAdd(message.Id, false);

            lock (_lock)
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
            _acked.TryUpdate(messageId, true, false);
        }

        private async Task StartSendingLoop()
        {
            while (!unsentItems.IsEmpty)
            {
                foreach (var key in unsentItems.Keys)
                {
                    unsentItems.TryGetValue(key, out var unsentItem);

                    if (unsentItem is null)
                    {
                        unsentItems.Remove(key, out _);
                        continue;
                    }

                    if (_acked.TryGetValue(unsentItem.Message.Id, out var isAcked) && isAcked)
                    {
                        unsentItems.Remove(key, out _);
                        _acked.TryRemove(unsentItem.Message.Id, out _);
                        continue;
                    }

                    if (unsentItem.Backoff > TimeSpan.FromMinutes(10))
                    {
                        await _unsentMessagesRedisService.Save(unsentItem.Message);
                        unsentItems.Remove(key, out _);
                        _acked.TryRemove(unsentItem.Message.Id, out _);
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

            lock (_lock)
            {
                _isSending = false;
            }
        }

        private void IncreaseBackoff(UnsentItem item)
        {
            item.Backoff = item.Backoff.Multiply(1.5);
            item.ResendAfter = DateTime.UtcNow.Add(item.Backoff);
        }
    }
}