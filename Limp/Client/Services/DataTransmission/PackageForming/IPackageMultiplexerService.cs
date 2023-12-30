using Ethachat.Client.ClientOnlyModels;

namespace Ethachat.Client.Services.DataTransmission.PackageForming;

public interface IPackageMultiplexerService
{
    string Combine(List<ClientPackage> packages, string partnerUsername);
}