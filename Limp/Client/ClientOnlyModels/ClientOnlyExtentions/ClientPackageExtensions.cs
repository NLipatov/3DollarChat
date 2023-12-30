using EthachatShared.Models.Message.DataTransfer;

namespace Ethachat.Client.ClientOnlyModels.ClientOnlyExtentions;

public static class ClientPackageExtensions
{
    public static Package ToPackage(this ClientPackage package)
    {
        return new Package
        {
            Index = package.Index,
            Total = package.Total,
            B64Data = package.B64Data,
            ContentType = package.ContentType,
            FileDataid = package.FileDataid,
            FileName = package.FileName,
            IV = package.IV
        };
    }
}