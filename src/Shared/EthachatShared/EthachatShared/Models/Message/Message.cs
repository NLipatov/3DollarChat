using System.Text.Json;
using EthachatShared.Contracts;
using EthachatShared.Models.Message.DataTransfer;
using EthachatShared.Models.Message.Interfaces;
using EthachatShared.Models.Message.TransferStatus;

namespace EthachatShared.Models.Message;

public class Message : ICloneable<Message>, IDestinationResolvable, ISourceResolvable, IDescribeable, IIdentifiable
{
    public HlsPlaylist? HlsPlaylist { get; set; }
    public SyncItem? SyncItem { get; set; }
    public Metadata? Metadata { get; set; }
    public Package? Package { get; set; }
    public string BlobLink { get; set; }
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Target { get; set; }
    public string? SenderConnectionId { get; set; }
    public string? CompanionConnectionId { get; set; }
    public string Sender { get; set; }
    public Cryptogram? Cryptogramm { get; set; }
    public bool IsDelivered { get; set; } = false;
    public DateTime DateReceived { get; set; }
    public bool IsSeen { get; set; } = false;
    public DateTime DateRead { get; set; }
    public DateTime DateSent { get; set; } = DateTime.UtcNow;
    public MessageType Type { get; set; }
    public bool IsRegisteredByHub { get; set; }

    public Message Clone()
    {
        return JsonSerializer
            .Deserialize<Message>
            (JsonSerializer.Serialize(this))!;
    }

    public string ItemDescription()
    {
        return Type switch
        {
            MessageType.Metadata => "file",
            _ => "message"
        };
    }
}
