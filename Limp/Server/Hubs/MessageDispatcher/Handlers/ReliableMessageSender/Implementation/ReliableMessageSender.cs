using System.Collections.Concurrent;
using Ethachat.Server.Hubs.MessageDispatcher.Handlers.MessageTransmitionGateway;
using Ethachat.Server.Hubs.MessageDispatcher.Handlers.ReliableMessageSender.ConcreteSenders.Binary;
using Ethachat.Server.Hubs.MessageDispatcher.Handlers.ReliableMessageSender.ConcreteSenders.Binary.Implementation;
using Ethachat.Server.Hubs.MessageDispatcher.Handlers.ReliableMessageSender.ConcreteSenders.Text;
using Ethachat.Server.Hubs.MessageDispatcher.Handlers.ReliableMessageSender.ConcreteSenders.Text.Implementation;
using Ethachat.Server.Utilities.Redis.UnsentMessageHandling;
using EthachatShared.Models.Message;

namespace Ethachat.Server.Hubs.MessageDispatcher.Handlers.ReliableMessageSender.Implementation;

public class ReliableMessageSender : IReliableMessageSender
{
    private static IReliableTextMessageSender _reliableTextMessageSender;
    private static IReliableBinaryMessageSender _reliableBinaryMessageSender;
    private static ConcurrentDictionary<Guid, MessageType> _messageIdToType = new();

    public ReliableMessageSender(IMessageGateway gateway, IUnsentMessagesRedisService unsentMessagesRedisService)
    {
        if (_reliableTextMessageSender is null)
        {
            _reliableTextMessageSender = new ReliableTextMessageSender(gateway, unsentMessagesRedisService);
        }

        if (_reliableBinaryMessageSender is null)
        {
            _reliableBinaryMessageSender = new ReliableBinaryMessageSender(gateway, unsentMessagesRedisService);
        }
    }

    public async Task Enqueue(Message message)
    {
        try
        {
            var targetSender = GetTargetSender(message.Type);
            await targetSender.Enqueue(message);
            _messageIdToType.TryAdd(message.Id, message.Type);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    public void OnAckReceived(Message syncMessage)
    {
        GetTargetSender(syncMessage.Type).OnAckReceived(syncMessage);
    }

    private IReliableMessageSender GetTargetSender(MessageType type)
    {
        return type switch
        {
            MessageType.Metadata or MessageType.DataPackage => _reliableBinaryMessageSender,
            _ => _reliableTextMessageSender
        };
    }
}