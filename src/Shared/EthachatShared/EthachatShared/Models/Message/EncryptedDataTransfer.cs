#nullable disable
using EthachatShared.Models.Message.Interfaces;

namespace EthachatShared.Models.Message;

public record EncryptedDataTransfer : IDestinationResolvable, ISourceResolvable, IDescribeable, IIdentifiable
{
    public required Guid Id { get; set; } = Guid.NewGuid();
    public required Cryptogram Cryptogram { get; set; }
    public required Type DataType { get; set; }
    public required string Target { get; set; }
    public required string Sender { get; set; }
    public string ItemDescription()
    {
        return DataType switch
        {
            _ => "something"
        };
    }
}