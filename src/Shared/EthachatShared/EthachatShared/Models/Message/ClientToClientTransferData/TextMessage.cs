using EthachatShared.Models.Message.Interfaces;

namespace EthachatShared.Models.Message.ClientToClientTransferData;

public record TextMessage : IIdentifiable
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Sender { get; set; }
    public required string Receiver { get; set; }
    public string Text { get; set; } = string.Empty;
    public int Index { get; set; } = 0;
    public int Total { get; set; } = 1;
}