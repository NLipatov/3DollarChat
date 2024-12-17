using EthachatShared.Models.Message.Interfaces;
using MessagePack;

namespace Client.Transfer.Domain.TransferedEntities.Messages;

[MessagePackObject]
public class HlsPlaylistMessage : IDestinationResolvable, ISourceResolvable, IIdentifiable
{
    [Key(0)] public Guid Id { get; set; } = Guid.NewGuid();
    [Key(1)] public required string Target { get; set; }
    [Key(2)] public required string Sender { get; set; }
    [Key(3)] public required string Playlist { get; set; }
}