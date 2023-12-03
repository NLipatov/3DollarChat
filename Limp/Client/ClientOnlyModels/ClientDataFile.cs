using LimpShared.Models.Message.DataTransfer;

namespace Limp.Client.ClientOnlyModels;

public record ClientDataFile : DataFile
{
    public List<ClientPackage> ClientPackages { get; set; }
}