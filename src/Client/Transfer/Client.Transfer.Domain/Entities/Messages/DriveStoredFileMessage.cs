using EthachatShared.Models.Message.Interfaces;
using MessagePack;

namespace Client.Transfer.Domain.Entities.Messages;

[MessagePackObject]
public record DriveStoredFileMessage : IIdentifiable, ISourceResolvable, IDestinationResolvable
{
    [Key(0)] public required Guid Id { get; set; }
    [Key(1)] public required string Sender { get; set; }
    [Key(2)] public required string Target { get; set; }
}