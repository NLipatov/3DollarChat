using EthachatShared.Models.Message.DataTransfer;

namespace Ethachat.Client.ClientOnlyModels.ClientOnlyExtentions;

public static class ClientPackageExtensions
{
    public static Package ToPackage(this ClientPackage package)
    {
        return new Package
        {
            Index = package.Index,
            B64Data = package.B64Data,
            FileDataid = package.FileDataid,
            IV = package.IV
        };
    }
}