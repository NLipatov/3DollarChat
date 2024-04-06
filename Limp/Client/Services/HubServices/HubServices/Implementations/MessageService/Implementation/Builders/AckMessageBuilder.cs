using EthachatShared.Models.Message;
using EthachatShared.Models.Message.TransferStatus;

namespace Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.Implementation.Builders;

public class AckMessageBuilder
{
    public Message CreateMessageAck(Message message)
    {
        return message.Type switch
        {
            MessageType.Metadata => CreateBinaryMessageAck(message),
            MessageType.DataPackage => CreateBinaryMessageAck(message),
            _ => CreateBaseMessageAck(message)
        };
    }

    private Message CreateBinaryMessageAck(Message message)
    {
        var index = message.Type is MessageType.Metadata ? -1 : message.Package!.Index;
        var fileId = message.Type is MessageType.Metadata ? message.Metadata!.DataFileId : message.Package!.FileDataid;

        return new Message
        {
            Id = message.Id,
            Sender = message.Sender,
            Type = message.Type,
            SyncItem = new SyncItem
            {
                Index = index,
                FileId = fileId
            }
        };
    }

    private Message CreateBaseMessageAck(Message message)
    {
        return new Message
        {
            SyncItem = new SyncItem()
            {
                MessageId = message.Id,
            },
            Type = MessageType.SyncItem,
            Sender = message.Sender,
            TargetGroup = message.TargetGroup
        };
    }
}