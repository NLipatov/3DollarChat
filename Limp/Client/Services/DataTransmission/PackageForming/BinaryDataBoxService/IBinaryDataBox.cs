using Ethachat.Client.ClientOnlyModels;
using EthachatShared.Models.Message.DataTransfer;

namespace Ethachat.Client.Services.DataTransmission.PackageForming.BinaryDataBoxService;

public interface IBinaryDataBox
{
    void StoreMetadata(Metadata metadata);
    bool StoreData(Guid fileId, ClientPackage clientPackage);
    ClientPackage[]? GetData(Guid fileId);
    Metadata GetMetadata(Guid fileId);
}