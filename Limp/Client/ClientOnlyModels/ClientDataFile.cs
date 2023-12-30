using LimpShared.Models.Message.DataTransfer;

namespace Ethachat.Client.ClientOnlyModels;

public record ClientDataFile : DataFile
{
    public List<ClientPackage> ClientPackages { get; set; }
}