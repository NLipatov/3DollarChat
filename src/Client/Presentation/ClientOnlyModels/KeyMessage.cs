using EthachatShared.Encryption;
using EthachatShared.Models.Message.Interfaces;
using MessagePack;

namespace Ethachat.Client.ClientOnlyModels;

[MessagePackObject]
public class KeyMessage : IDestinationResolvable, ISourceResolvable, IIdentifiable
{
    [Key(0)] public Guid Id { get; set; } = Guid.NewGuid();
    [Key(1)] public required Key Key { get; set; }
    [Key(2)] public string Target { get; set; }
    [Key(3)] public string Sender { get; set; }
}