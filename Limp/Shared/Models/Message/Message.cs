using System.Text.Json;
using ICloneable = ClientServerCommon.Models.Contracts.ICloneable<ClientServerCommon.Models.Message.Message>;

namespace ClientServerCommon.Models.Message;

public class Message : ICloneable
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string? TargetGroup { get; set; }
    public string? SenderConnectionId { get; set; }
    public string? CompanionConnectionId { get; set; }
    public string? Sender { get; set; }
    public string? PlainTextPayload { get; set; }
    public Cryptogramm? Cryptogramm { get; set; }
    public bool IsReceived { get; set; } = false;
    public DateTime DateReceived { get; set; }
    public bool IsRead { get; set; } = false;
    public DateTime DateRead { get; set; }
    public DateTime DateSent { get; set; }
    public MessageType Type { get; set; }

    public Message Clone()
    {
        return JsonSerializer
            .Deserialize<Message>
            (JsonSerializer.Serialize(this))!;
    }
}
