using EthachatShared.Models.Message.Interfaces;
using MessagePack;

namespace Client.Transfer.Domain.TransferedEntities.Messages;

[MessagePackObject]
public record DriveStoredFileMessage : IIdentifiable, ISourceResolvable, IDestinationResolvable
{
    [Key(0)] public required Guid Id { get; set; }
    [Key(1)] public required string Sender { get; set; }
    [Key(2)] public required string Target { get; set; }
    [Key(3)] public required string ContentType { get; set; }
    [Key(4)] public required string Filename { get; set; }
    [Key(5)] public required byte[] Iv { get; init; } = [];
    [Key(6)] public required Guid KeyId { get; init; }
}