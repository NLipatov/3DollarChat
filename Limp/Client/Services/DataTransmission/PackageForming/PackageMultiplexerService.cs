using Limp.Client.ClientOnlyModels;

namespace Limp.Client.Services.DataTransmission.PackageForming;

class PackageMultiplexerService : IPackageMultiplexerService
{
    public string Combine(List<ClientPackage> packages, string partnerUsername)
    {
        byte[] combinedBytes = packages
            .OrderBy(p => p.Index)
            .SelectMany(p => Convert.FromBase64String(p.PlainB64Data))
            .ToArray();

        return Convert.ToBase64String(combinedBytes);
    }
}