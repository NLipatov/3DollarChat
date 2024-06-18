namespace EthachatShared.Models.Message;

public record HlsPlaylist
{
    public Guid VideoId { get; set; }
    public required string Name { get; set; }
    public required string M3U8Content { get; set; }
    public string HexKey { get; set; } = string.Empty;
    public string HexIv { get; set; } = string.Empty;
}