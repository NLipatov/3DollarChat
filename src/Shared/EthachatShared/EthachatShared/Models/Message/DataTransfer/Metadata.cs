namespace EthachatShared.Models.Message.DataTransfer;

public record Metadata
{
    public required Guid DataFileId { get; set; }
    public required string ContentType { get; set; }
    public required string Filename { get; set; }
    public required int ChunksCount { get; set; }
}