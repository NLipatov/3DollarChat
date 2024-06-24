using EthachatShared.Models.Message.Interfaces;
using MessagePack;

namespace Ethachat.Client.ClientOnlyModels;

[MessagePackObject]
public class BinaryMessage : IDestinationResolvable, ISourceResolvable, IIdentifiable
{
    [Key(0)] public Guid Id { get; set; }
    [Key(1)] public required string Target { get; set; }
    [Key(2)] public required string Sender { get; set; }
    [Key(3)] public int Total { get; set; }
    [Key(4)] public int Index { get; set; }
    [Key(5)] public byte[] Data { get; set; } = [];
    [Key(6)] public string Iv { get; set; } = string.Empty;
}