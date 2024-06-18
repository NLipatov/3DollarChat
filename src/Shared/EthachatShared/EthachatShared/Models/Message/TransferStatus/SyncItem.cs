#nullable disable
namespace EthachatShared.Models.Message.TransferStatus;

public class SyncItem
{
    public Guid MessageId { get; set; }
    public Guid FileId { get; set; }
    public int Index { get; set; }
    public int TotalItems { get; set; }
}