namespace EthachatShared.Encryption;

public class Key
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public object? Value { get; set; }
    public KeyFormat? Format { get; set; }
    public KeyType? Type { get; set; }
    public string? Contact { get; set; }
    public string? Author { get; set; }
    public bool IsAccepted { get; set; } = false;
    public DateTime CreationDate { get; set; } = DateTime.UtcNow;
    public Guid? OfferMessageId { get; set; }
    public Guid? AcceptMessageId { get; set; }
}
