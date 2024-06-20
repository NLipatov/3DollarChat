using Ethachat.Server.Hubs.MessageDispatcher.Handlers.MessageTransmitionGateway;
using Ethachat.Server.Hubs.MessageDispatcher.Handlers.ReliableMessageSender.ConcreteSenders.LongTermMessageStorage;
using Ethachat.Server.Hubs.MessageDispatcher.Handlers.ReliableMessageSender.ConcreteSenders.SenderImplementations.Text;
using Ethachat.Server.Hubs.MessageDispatcher.Handlers.ReliableMessageSender.ConcreteSenders.SenderImplementations.Text.Implementation;
using EthachatShared.Models.Message;

namespace Ethachat.Server.Hubs.MessageDispatcher.Handlers.ReliableMessageSender.Implementation;

public class ReliableMessageSender : IReliableMessageSender<Message>
{
    private static IReliableTextMessageSender _reliableTextMessageSender;

    public ReliableMessageSender(IMessageGateway<Message> gateway, ILongTermStorageService<Message> longTermStorageService)
    {
        if (_reliableTextMessageSender is null)
        {
            _reliableTextMessageSender = new ReliableTextMessageSender(gateway, longTermStorageService);
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

    public void OnAck(Message data)
    {
        GetTargetSender(data.Type).OnAck(data);
    }

    private IReliableMessageSender<Message> GetTargetSender(MessageType type)
    {
        return type switch
            
        {
            _ => _reliableTextMessageSender
        };
    }
}