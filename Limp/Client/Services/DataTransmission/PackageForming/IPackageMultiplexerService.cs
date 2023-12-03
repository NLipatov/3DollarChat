using Limp.Client.ClientOnlyModels;

namespace Limp.Client.Services.DataTransmission.PackageForming;

public interface IPackageMultiplexerService
{
    string Combine(List<ClientPackage> packages, string partnerUsername);
}