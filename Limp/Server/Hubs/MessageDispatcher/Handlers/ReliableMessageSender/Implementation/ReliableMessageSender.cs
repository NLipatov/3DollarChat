using Ethachat.Server.Hubs.MessageDispatcher.Handlers.MessageTransmitionGateway;
using Ethachat.Server.Hubs.MessageDispatcher.Handlers.ReliableMessageSender.ConcreteSenders.LongTermMessageStorage;
using Ethachat.Server.Hubs.MessageDispatcher.Handlers.ReliableMessageSender.ConcreteSenders.SenderImplementations.Binary;
using Ethachat.Server.Hubs.MessageDispatcher.Handlers.ReliableMessageSender.ConcreteSenders.SenderImplementations.Binary.Implementation;
using Ethachat.Server.Hubs.MessageDispatcher.Handlers.ReliableMessageSender.ConcreteSenders.SenderImplementations.Text;
using Ethachat.Server.Hubs.MessageDispatcher.Handlers.ReliableMessageSender.ConcreteSenders.SenderImplementations.Text.Implementation;
using EthachatShared.Models.Message;

namespace Ethachat.Server.Hubs.MessageDispatcher.Handlers.ReliableMessageSender.Implementation;

public class ReliableMessageSender : IReliableMessageSender
{
    private static IReliableTextMessageSender _reliableTextMessageSender;
    private static IReliableBinaryMessageSender _reliableBinaryMessageSender;

    public ReliableMessageSender(IMessageGateway gateway, ILongTermMessageStorageService longTermMessageStorageService)
    {
        if (_reliableTextMessageSender is null)
        {
            _reliableTextMessageSender = new ReliableTextMessageSender(gateway, longTermMessageStorageService);
        }

        if (_reliableBinaryMessageSender is null)
        {
            _reliableBinaryMessageSender = new ReliableBinaryMessageSender(gateway, longTermMessageStorageService);
        }
    }

    public async Task EnqueueAsync(Message message)
    {
        try
        {
            var targetSender = GetTargetSender(message.Type);
            await targetSender.EnqueueAsync(message);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    public void OnAck(Message syncMessage)
    {
        GetTargetSender(syncMessage.Type).OnAck(syncMessage);
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