using EthachatShared.Models.Message.DataTransfer;

namespace Ethachat.Client.ClientOnlyModels;

public record ClientDataFile : DataFile
{
    public List<Package> ClientPackages { get; set; }
}