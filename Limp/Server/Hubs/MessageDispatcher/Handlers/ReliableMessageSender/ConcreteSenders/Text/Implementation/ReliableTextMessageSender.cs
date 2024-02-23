using System.Collections.Concurrent;
using Ethachat.Server.Hubs.MessageDispatcher.Handlers.MessageTransmitionGateway;
using Ethachat.Server.Hubs.MessageDispatcher.Handlers.ReliableMessageSender.ConcreteSenders.Models;
using Ethachat.Server.Hubs.MessageDispatcher.Handlers.ReliableMessageSender.ConcreteSenders.Models.Extentions;
using Ethachat.Server.Hubs.UsersConnectedManaging.ConnectedUserStorage;
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

        public ReliableTextMessageSender(IMessageGateway gateway,
            IUnsentMessagesRedisService unsentMessagesRedisService)
        {
            _unsentMessagesRedisService = unsentMessagesRedisService;
            _gateway = gateway;
        }

        public async Task EnqueueAsync(Message message)
        {
            var unsentMessage = message.ToUnsentMessage();
            _unsentItems.TryAdd(message.Id, unsentMessage);

            await Deliver(message);
        }

        private async Task Deliver(Message message, TimeSpan? backoff = null)
        {
            if (_unsentItems.ContainsKey(message.Id))
            {
                if (!HasActiveConnections(message.TargetGroup!))
                {
                    await PassToLongTermSender(message.Id);
                    Remove(message.Id);
                    return;
                }
                _acked.TryGetValue(message.Id, out var isAcked);
                if (isAcked)
                {
                    Remove(message.Id);
                    return;
                }

                await _gateway.SendAsync(message);
                backoff = IncreaseBackoff(backoff);
                await Task.Delay(backoff.Value);
                await Deliver(message, backoff);
            }
        }


        private async Task PassToLongTermSender(Guid messageId)
        {
            _unsentItems.TryRemove(messageId, out var unsentItems);

            if (unsentItems is not null)
            {
                await _unsentMessagesRedisService.SaveAsync(unsentItems.Message);
            }
        }

        private bool HasActiveConnections(string username)
            => InMemoryHubConnectionStorage.MessageDispatcherHubConnections
                .Where(x => x.Key == username)
                .SelectMany(x => x.Value).Any();

        public void OnAck(Message syncMessage)
        {
            _acked.TryAdd(syncMessage.SyncItem!.MessageId, true);
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
}