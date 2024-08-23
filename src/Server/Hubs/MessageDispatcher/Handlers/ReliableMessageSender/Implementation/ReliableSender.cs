using Ethachat.Server.Hubs.MessageDispatcher.Handlers.MessageTransmitionGateway;
using Ethachat.Server.Hubs.MessageDispatcher.Handlers.ReliableMessageSender.ConcreteSenders.LongTermMessageStorage;
using Ethachat.Server.Hubs.MessageDispatcher.Handlers.ReliableMessageSender.ConcreteSenders.SenderImplementations.Text;
using Ethachat.Server.Hubs.MessageDispatcher.Handlers.ReliableMessageSender.ConcreteSenders.SenderImplementations.Text.Implementation;
using EthachatShared.Contracts;
using EthachatShared.Models.Message;

namespace Ethachat.Server.Hubs.MessageDispatcher.Handlers.ReliableMessageSender.Implementation;

public class ReliableSender : IReliableSender<Message>
{
    private static IReliableTextSender _reliableTextSender;

    public ReliableSender(IMessageGateway<Message> gateway, ILongTermStorageService<Message> longTermStorageService)
    {
        if (_reliableTextSender is null)
        {
            _reliableTextSender = new ReliableTextSender(gateway, longTermStorageService);
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

    public void OnAck(Guid id)
    {
        _reliableTextSender.OnAck(id);
    }

    private IReliableSender<Message> GetTargetSender(MessageType type)
    {
        return type switch
            
        {
            _ => _reliableTextSender
        };
    }
}